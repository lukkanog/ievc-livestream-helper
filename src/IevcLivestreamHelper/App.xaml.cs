using System.Windows;
using System.Windows.Threading;
using IevcLivestreamHelper.Alerts;
using IevcLivestreamHelper.Diagnostics;
using IevcLivestreamHelper.Settings;
using IevcLivestreamHelper.Startup;
using IevcLivestreamHelper.Views;

namespace IevcLivestreamHelper;

/// <summary>
/// Ponto de partida do app: carrega a configuração e abre as janelas.
///
///   ControlWindow  → janela do operador (no monitor do OBS)
///   AlertService   → guarda o aviso atual e o tempo restante
///   OverlayWindow  → o aviso em si, desenhado por cima da TV
/// </summary>
public partial class App : Application
{
    public const string AppTitle = "IEVC Livestream Helper";

    /// <summary>Impede abrir o app duas vezes (veja <see cref="SingleInstance"/>).</summary>
    private SingleInstance? _singleInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnexpectedError;
        AppDomain.CurrentDomain.UnhandledException += OnFatalError;

        Log.DeleteOldFiles();
        Log.Info($"Abrindo o {AppTitle} {typeof(App).Assembly.GetName().Version} " +
                 $"(.NET {Environment.Version}, {Environment.OSVersion})");

        _singleInstance = SingleInstance.TryAcquire() ?? HandleAlreadyRunning();
        if (_singleInstance is null)
        {
            Shutdown();
            return;
        }

        try
        {
            OpenWindows();
        }
        catch (Exception ex)
        {
            // Se der erro antes da janela abrir, o app ficaria rodando escondido.
            // Melhor mostrar o erro e fechar.
            Log.Error("Erro ao abrir o app", ex);
            ShowErrorMessage("Não consegui abrir o app.", ex);
            Shutdown();
        }
    }

    /// <summary>
    /// Já existe outro aberto. Se ele tem janela, só trazemos ela para a frente.
    /// Se não tem (travou), oferecemos fechar o travado e abrir um novo.
    /// Retorna a vaga de instância única, ou null se este não deve abrir.
    /// </summary>
    private static SingleInstance? HandleAlreadyRunning()
    {
        if (SingleInstance.BringOtherInstanceToFront())
        {
            Log.Info("Já estava aberto; trouxe a janela existente para a frente.");
            return null;
        }

        Log.Warning("Já existe uma instância aberta, mas sem janela (travada).");
        var answer = MessageBox.Show(
            "O IEVC Livestream Helper parece estar aberto, mas travado e sem janela.\n\n" +
            "Fechar o que travou e abrir de novo?",
            AppTitle, MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
            return null;

        SingleInstance.CloseOtherInstances();
        var instance = SingleInstance.WaitAndAcquire(TimeSpan.FromSeconds(5));
        if (instance is null)
        {
            MessageBox.Show(
                "Não consegui fechar o que travou. Reinicie o computador e tente de novo.",
                AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        return instance;
    }

    private void OpenWindows()
    {
        var settingsStore = new SettingsStore();
        Log.Info($"Configurações em: {settingsStore.FilePath}");

        var settings = settingsStore.Load(out var settingsError);
        if (settingsError is not null)
        {
            Log.Warning(settingsError);
            MessageBox.Show($"{settingsError}\n\nPor enquanto vou usar as configurações padrão.",
                AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        var overlay = new OverlayWindow();
        var alerts = new AlertService(overlay);
        var controlWindow = new ControlWindow(settingsStore, settings, alerts);

        // Fechar a janela de controle fecha o app inteiro (inclusive o aviso na TV).
        MainWindow = controlWindow;
        controlWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Info("App fechado.");
        _singleInstance?.Dispose();
        base.OnExit(e);
    }

    /// <summary>
    /// Durante o culto o app não pode simplesmente fechar.
    /// Mostra o erro e continua rodando.
    /// </summary>
    private static void OnUnexpectedError(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error("Erro inesperado", e.Exception);
        ShowErrorMessage("Aconteceu um erro inesperado, mas o app continua aberto.", e.Exception);
        e.Handled = true;
    }

    /// <summary>Erro fora da tela (ex.: em segundo plano). Aqui o app vai fechar; só dá para registrar.</summary>
    private static void OnFatalError(object sender, UnhandledExceptionEventArgs e) =>
        Log.Error("Erro grave, o app vai fechar", e.ExceptionObject as Exception);

    /// <summary>Mostra o erro e oferece abrir a pasta do log, com os detalhes técnicos.</summary>
    private static void ShowErrorMessage(string title, Exception exception)
    {
        var answer = MessageBox.Show(
            $"{title}\n\n{exception.Message}\n\n" +
            $"Os detalhes foram salvos no arquivo de registro (log):\n{Log.CurrentFilePath}\n\n" +
            "Quer abrir a pasta dos registros agora?",
            AppTitle, MessageBoxButton.YesNo, MessageBoxImage.Error);

        if (answer == MessageBoxResult.Yes)
            Log.OpenFolder();
    }
}

using System.Windows;
using System.Windows.Threading;
using IevcLivestreamHelper.Alerts;
using IevcLivestreamHelper.Diagnostics;
using IevcLivestreamHelper.Settings;
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

    /// <summary>Impede abrir o app duas vezes (duas janelinhas mandando avisos para a mesma TV).</summary>
    private Mutex? _singleInstanceLock;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnexpectedError;
        AppDomain.CurrentDomain.UnhandledException += OnFatalError;

        Log.DeleteOldFiles();
        Log.Info($"Abrindo o {AppTitle} {typeof(App).Assembly.GetName().Version} " +
                 $"(.NET {Environment.Version}, {Environment.OSVersion})");

        _singleInstanceLock = new Mutex(initiallyOwned: true, "IevcLivestreamHelper.SingleInstance", out var isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show("O IEVC Livestream Helper já está aberto. Procure o ícone dele na barra de tarefas.",
                AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
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
        _singleInstanceLock?.Dispose();
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

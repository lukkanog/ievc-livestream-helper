using System.Windows;
using System.Windows.Threading;
using IevcLivestreamHelper.Alerts;
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
    public const string AppTitle = "Sininho da Live";

    /// <summary>Impede abrir o app duas vezes (os atalhos iriam brigar entre si).</summary>
    private Mutex? _singleInstanceLock;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnexpectedError;

        _singleInstanceLock = new Mutex(initiallyOwned: true, "IevcLivestreamHelper.Sininho", out var isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show("O Sininho já está aberto. Procure o ícone dele na barra de tarefas.",
                AppTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        var settingsStore = new SettingsStore();
        var settings = settingsStore.Load(out var settingsError);
        if (settingsError is not null)
        {
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
        _singleInstanceLock?.Dispose();
        base.OnExit(e);
    }

    /// <summary>
    /// Durante o culto o app não pode simplesmente fechar.
    /// Mostra o erro e continua rodando.
    /// </summary>
    private static void OnUnexpectedError(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"Aconteceu um erro inesperado:\n\n{e.Exception.Message}",
            AppTitle, MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}

using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using IevcLivestreamHelper.Alerts;
using IevcLivestreamHelper.Diagnostics;
using IevcLivestreamHelper.Monitors;
using IevcLivestreamHelper.Settings;

namespace IevcLivestreamHelper.Views;

/// <summary>
/// Janelinha flutuante do operador: botões de aviso, mensagem personalizada,
/// limpar e configurações. Fica sempre por cima, pode ser arrastada e
/// recolhida numa bolinha.
/// </summary>
public partial class ControlWindow : Window
{
    private static readonly Brush IdleStatusBackground = new SolidColorBrush(Color.FromRgb(0xEE, 0xF7, 0xEE));
    private static readonly Brush ActiveStatusBackground = new SolidColorBrush(Color.FromRgb(0xFF, 0xEF, 0xC2));
    private static readonly Brush IdleBubbleBorder = new SolidColorBrush(Color.FromRgb(0xF1, 0xDF, 0xCF));
    private static readonly Brush ActiveBubbleBorder = new SolidColorBrush(Color.FromRgb(0xFF, 0x7A, 0x59));

    /// <summary>Quanto o mouse pode mexer num clique sem que ele vire "arrastar".</summary>
    private const double ClickTolerance = 4;

    private readonly SettingsStore _settingsStore;
    private readonly AlertService _alerts;
    private AppSettings _settings;

    /// <summary>
    /// Verdadeiro enquanto preenchemos os campos a partir da configuração,
    /// para não salvar o arquivo à toa nesse momento.
    /// Começa verdadeiro porque, durante o InitializeComponent, o WPF já dispara
    /// eventos como o ValueChanged da barra de duração.
    /// </summary>
    private bool _isFillingControls = true;

    public ControlWindow(SettingsStore settingsStore, AppSettings settings, AlertService alerts)
    {
        // Os campos são preenchidos ANTES do InitializeComponent, porque os eventos
        // dos controles podem rodar no meio dele e já precisam deles.
        _settingsStore = settingsStore;
        _settings = settings;
        _alerts = alerts;

        InitializeComponent();

        _alerts.Changed += (_, _) => UpdateStatus();
        Loaded += (_, _) => PlaceWindowOnStart();
        Closed += (_, _) => _alerts.Clear();

        ApplySettings();
    }

    // ===== Enviar e limpar avisos =====

    /// <summary>Manda uma mensagem para a TV usando a tela e a duração configuradas.</summary>
    private void SendAlert(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var tvMonitor = MonitorList.FindTvMonitor(_settings.TvMonitorDeviceName);
        if (tvMonitor is null)
        {
            ShowProblem("Nenhum monitor encontrado para mostrar o aviso.");
            return;
        }

        Log.Info($"Aviso enviado para {tvMonitor.DisplayName}: \"{message.Trim()}\"");
        _alerts.Show(message, TimeSpan.FromSeconds(_settings.AlertDurationSeconds), tvMonitor);
    }

    private void OnQuickMessageClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: QuickMessage message })
            SendAlert(message.Text);
    }

    private void OnSendCustomMessageClick(object sender, RoutedEventArgs e) => SendCustomMessage();

    private void OnCustomMessageKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SendCustomMessage();
            e.Handled = true;
        }
    }

    private void SendCustomMessage()
    {
        SendAlert(CustomMessageBox.Text);
        CustomMessageBox.Clear();
    }

    private void OnClearClick(object sender, RoutedEventArgs e) => _alerts.Clear();

    private void OnTestClick(object sender, RoutedEventArgs e) =>
        SendAlert("Teste: se você está lendo isto, esta é a tela certa!");

    /// <summary>Atualiza a faixa "o que está na TV agora" e o contador da bolinha.</summary>
    private void UpdateStatus()
    {
        if (_alerts.IsShowing)
        {
            var secondsLeft = (int)Math.Ceiling(_alerts.TimeLeft.TotalSeconds);
            StatusText.Text = $"Na TV agora: “{_alerts.CurrentMessage}” — some em {secondsLeft} s";
            StatusPanel.Background = ActiveStatusBackground;

            BubbleBadgeText.Text = secondsLeft.ToString();
            BubbleBadge.Visibility = Visibility.Visible;
            Bubble.BorderBrush = ActiveBubbleBorder;
        }
        else
        {
            StatusText.Text = "Nenhum aviso na TV.";
            StatusPanel.Background = IdleStatusBackground;

            BubbleBadge.Visibility = Visibility.Collapsed;
            Bubble.BorderBrush = IdleBubbleBorder;
        }
    }

    // ===== Janelinha: arrastar, recolher, fechar =====

    /// <summary>Segurar o cabeçalho e arrastar muda a janelinha de lugar.</summary>
    private void OnDragAreaMouseDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
        SavePosition();
    }

    /// <summary>
    /// Na bolinha, o mesmo gesto serve para duas coisas:
    /// se o mouse quase não andou, foi um clique (abre); se andou, foi um arraste.
    /// </summary>
    private void OnBubbleMouseDown(object sender, MouseButtonEventArgs e)
    {
        var before = new Point(Left, Top);
        DragMove(); // só volta quando o botão do mouse é solto

        var moved = Math.Abs(Left - before.X) > ClickTolerance || Math.Abs(Top - before.Y) > ClickTolerance;
        if (moved)
            SavePosition();
        else
            SetCollapsed(false);
    }

    private void OnCollapseClick(object sender, RoutedEventArgs e) => SetCollapsed(true);

    private void OnToggleSettingsClick(object sender, RoutedEventArgs e)
    {
        var right = Left + ActualWidth;
        SettingsPanel.Visibility = SettingsPanel.IsVisible ? Visibility.Collapsed : Visibility.Visible;
        KeepRightEdge(right);
    }

    /// <summary>Fechar encerra o app e tira o aviso da TV, então confirmamos antes.</summary>
    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show(this,
            "Fechar o IEVC Livestream Helper?\n\nSe houver um aviso na TV, ele some.",
            App.AppTitle, MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (answer == MessageBoxResult.Yes)
            Close();
    }

    /// <summary>Troca entre a janelinha aberta e a bolinha, sem "pular" de lugar na tela.</summary>
    private void SetCollapsed(bool collapsed)
    {
        var right = Left + ActualWidth;

        ExpandedView.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;
        CollapsedView.Visibility = collapsed ? Visibility.Visible : Visibility.Collapsed;

        KeepRightEdge(right);
    }

    /// <summary>
    /// Depois de mudar de tamanho, mantém a borda direita no mesmo lugar
    /// (a janela "cresce para a esquerda"), e garante que ela continua visível.
    /// </summary>
    private void KeepRightEdge(double right)
    {
        UpdateLayout();
        Left = right - ActualWidth;
        KeepInsideScreen();
        SavePosition();
    }

    /// <summary>Abre onde estava da última vez, ou no canto inferior direito da tela principal.</summary>
    private void PlaceWindowOnStart()
    {
        if (_settings.ControlWindowPosition is { } saved)
        {
            Left = saved.Left;
            Top = saved.Top;
        }
        else
        {
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - ActualWidth;
            Top = workArea.Bottom - ActualHeight;
        }

        KeepInsideScreen();
    }

    /// <summary>Se a janela ficou fora da tela (ex.: um monitor foi desligado), traz de volta.</summary>
    private void KeepInsideScreen()
    {
        var screenLeft = SystemParameters.VirtualScreenLeft;
        var screenTop = SystemParameters.VirtualScreenTop;
        var screenRight = screenLeft + SystemParameters.VirtualScreenWidth;
        var screenBottom = screenTop + SystemParameters.VirtualScreenHeight;

        Left = Math.Max(screenLeft, Math.Min(Left, screenRight - ActualWidth));
        Top = Math.Max(screenTop, Math.Min(Top, screenBottom - ActualHeight));
    }

    private void SavePosition()
    {
        _settings.ControlWindowPosition = new WindowPosition { Left = Left, Top = Top };
        SaveSettings();
    }

    // ===== Configurações =====

    /// <summary>Preenche a janela de acordo com a configuração atual.</summary>
    private void ApplySettings()
    {
        _isFillingControls = true;

        QuickMessagesList.ItemsSource = _settings.QuickMessages;

        DurationSlider.Maximum = Math.Max(120, _settings.AlertDurationSeconds);
        DurationSlider.Value = _settings.AlertDurationSeconds;
        DurationLabel.Text = $"{_settings.AlertDurationSeconds} s";

        FillMonitorPicker();

        _isFillingControls = false;

        ShowProblem(null);
        UpdateStatus();
    }

    /// <summary>Lista os monitores e seleciona o da TV.</summary>
    private void FillMonitorPicker()
    {
        var wasFilling = _isFillingControls;
        _isFillingControls = true;

        var monitors = MonitorList.GetAll();
        var tvMonitor = MonitorList.FindTvMonitor(_settings.TvMonitorDeviceName);
        Log.Info($"Monitores: {string.Join("; ", monitors.Select(m => $"{m.DisplayName} [{m.DeviceName}]"))}. " +
                 $"TV: {tvMonitor?.DisplayName ?? "nenhum"}");

        MonitorPicker.ItemsSource = monitors;
        MonitorPicker.SelectedItem = monitors.FirstOrDefault(m => m.DeviceName == tvMonitor?.DeviceName);

        _isFillingControls = wasFilling;
    }

    /// <summary>Mostra um problema na parte de baixo da janela (ou esconde, se for null).</summary>
    private void ShowProblem(string? problem)
    {
        if (problem is not null)
            Log.Warning(problem);

        ProblemsText.Text = problem is null ? "" : "⚠ " + problem;
        ProblemsText.Visibility = problem is null ? Visibility.Collapsed : Visibility.Visible;
    }

    private void SaveSettings()
    {
        if (!_settingsStore.TrySave(_settings))
            ShowProblem($"Não consegui salvar em \"{_settingsStore.FilePath}\". A pasta pode estar protegida.");
    }

    private void OnMonitorPickerChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isFillingControls || MonitorPicker.SelectedItem is not MonitorInfo monitor)
            return;

        _settings.TvMonitorDeviceName = monitor.DeviceName;
        SaveSettings();
    }

    private void OnRefreshMonitorsClick(object sender, RoutedEventArgs e) => FillMonitorPicker();

    private void OnDurationChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // Este evento já dispara enquanto a janela está sendo montada.
        if (DurationLabel is null)
            return;

        var seconds = (int)DurationSlider.Value;
        DurationLabel.Text = $"{seconds} s";

        if (_isFillingControls)
            return;

        _settings.AlertDurationSeconds = seconds;
        SaveSettings();
    }

    private void OnEditSettingsFileClick(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(_settingsStore.FilePath))
            SaveSettings();

        Process.Start(new ProcessStartInfo("notepad.exe", $"\"{_settingsStore.FilePath}\"")
        {
            UseShellExecute = true,
        });
    }

    private void OnReloadSettingsClick(object sender, RoutedEventArgs e)
    {
        _settings = _settingsStore.Load(out var error);
        ApplySettings();

        if (error is not null)
        {
            MessageBox.Show(this, $"{error}\n\nPor enquanto vou usar as configurações padrão.",
                App.AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnOpenLogsClick(object sender, RoutedEventArgs e) => Log.OpenFolder();
}

using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using IevcLivestreamHelper.Alerts;
using IevcLivestreamHelper.Diagnostics;
using IevcLivestreamHelper.Hotkeys;
using IevcLivestreamHelper.Monitors;
using IevcLivestreamHelper.Settings;

namespace IevcLivestreamHelper.Views;

/// <summary>
/// Janela do operador: botões de mensagem, mensagem personalizada,
/// botão de limpar e configurações.
/// </summary>
public partial class ControlWindow : Window
{
    private static readonly Brush IdleStatusBackground = new SolidColorBrush(Color.FromRgb(0xEE, 0xF7, 0xEE));
    private static readonly Brush ActiveStatusBackground = new SolidColorBrush(Color.FromRgb(0xFF, 0xEF, 0xC2));

    private readonly SettingsStore _settingsStore;
    private readonly AlertService _alerts;
    private AppSettings _settings;
    private GlobalHotkeyManager? _hotkeys;

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

        // Os atalhos globais precisam da janela já criada no Windows.
        SourceInitialized += (_, _) =>
        {
            _hotkeys = new GlobalHotkeyManager(this);
            ApplySettings();
        };

        Closed += (_, _) =>
        {
            _hotkeys?.Dispose();
            _alerts.Clear();
        };
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
            ShowProblems(["Nenhum monitor encontrado para mostrar o aviso."]);
            return;
        }

        Log.Info($"Aviso enviado para {tvMonitor.DisplayName}: \"{message.Trim()}\"");
        _alerts.Show(message, TimeSpan.FromSeconds(_settings.AlertDurationSeconds), tvMonitor);
    }

    private void OnQuickMessageClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: QuickMessageItem item })
            SendAlert(item.Text);
    }

    private void OnSendCustomMessageClick(object sender, RoutedEventArgs e)
    {
        SendAlert(CustomMessageBox.Text);
        CustomMessageBox.Clear();
        CustomMessageBox.Focus();
    }

    private void OnClearClick(object sender, RoutedEventArgs e) => _alerts.Clear();

    private void OnOpenLogsClick(object sender, RoutedEventArgs e) => Log.OpenFolder();

    private void OnTestClick(object sender, RoutedEventArgs e) =>
        SendAlert("Teste: se você está lendo isto, esta é a tela certa!");

    /// <summary>Atualiza a faixa "o que está na TV agora".</summary>
    private void UpdateStatus()
    {
        if (_alerts.IsShowing)
        {
            var secondsLeft = (int)Math.Ceiling(_alerts.TimeLeft.TotalSeconds);
            StatusText.Text = $"Na TV agora: “{_alerts.CurrentMessage}” — some em {secondsLeft} s";
            StatusPanel.Background = ActiveStatusBackground;
        }
        else
        {
            StatusText.Text = "Nenhum aviso na TV.";
            StatusPanel.Background = IdleStatusBackground;
        }
    }

    // ===== Configurações =====

    /// <summary>Preenche a janela e registra os atalhos de acordo com a configuração atual.</summary>
    private void ApplySettings()
    {
        _isFillingControls = true;

        QuickMessagesList.ItemsSource = _settings.QuickMessages
            .Select(message => new QuickMessageItem(message.Text, message.Hotkey ?? ""))
            .ToList();

        ClearButton.Content = string.IsNullOrWhiteSpace(_settings.ClearHotkey)
            ? "Limpar aviso da TV"
            : $"Limpar aviso da TV   ({_settings.ClearHotkey})";

        DurationSlider.Maximum = Math.Max(120, _settings.AlertDurationSeconds);
        DurationSlider.Value = _settings.AlertDurationSeconds;
        DurationLabel.Text = $"{_settings.AlertDurationSeconds} s";

        FillMonitorPicker();

        _isFillingControls = false;

        RegisterHotkeys();
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

    private void RegisterHotkeys()
    {
        if (_hotkeys is null)
            return;

        _hotkeys.UnregisterAll();
        var problems = new List<string>();

        TryRegisterHotkey(_settings.ClearHotkey, _alerts.Clear, problems);

        foreach (var message in _settings.QuickMessages)
            TryRegisterHotkey(message.Hotkey, () => SendAlert(message.Text), problems);

        ShowProblems(problems);
    }

    private void TryRegisterHotkey(string? hotkeyText, Action action, List<string> problems)
    {
        if (string.IsNullOrWhiteSpace(hotkeyText))
            return; // atalho opcional

        if (!Hotkey.TryParse(hotkeyText, out var hotkey) || hotkey is null)
            problems.Add($"Não entendi o atalho \"{hotkeyText}\". Use algo como \"Ctrl+Alt+1\" ou \"F9\".");
        else if (!_hotkeys!.Register(hotkey, action))
            problems.Add($"O atalho {hotkeyText} já está sendo usado por outro programa.");
    }

    private void ShowProblems(IReadOnlyCollection<string> problems)
    {
        foreach (var problem in problems)
            Log.Warning(problem);

        ProblemsText.Text = string.Join(Environment.NewLine, problems.Select(p => "⚠ " + p));
        ProblemsText.Visibility = problems.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SaveSettings()
    {
        if (!_settingsStore.TrySave(_settings))
            ShowProblems([$"Não consegui salvar em \"{_settingsStore.FilePath}\". A pasta pode estar protegida."]);
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
            MessageBox.Show($"{error}\n\nPor enquanto vou usar as configurações padrão.",
                App.AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>O que cada botão de mensagem rápida mostra.</summary>
    public sealed record QuickMessageItem(string Text, string HotkeyLabel);
}

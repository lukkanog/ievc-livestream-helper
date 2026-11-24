using System.Windows.Threading;
using IevcLivestreamHelper.Monitors;
using IevcLivestreamHelper.Views;

namespace IevcLivestreamHelper.Alerts;

/// <summary>
/// Controla o aviso atual: qual mensagem está na TV e quanto tempo falta.
/// Quando o tempo acaba, o aviso some sozinho.
/// </summary>
public sealed class AlertService
{
    private readonly OverlayWindow _overlay;
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private DateTime _endsAtUtc;

    public AlertService(OverlayWindow overlay)
    {
        _overlay = overlay;
        _clock.Tick += (_, _) => OnClockTick();
    }

    /// <summary>Avisa a tela de controle que algo mudou (aviso novo, tempo passando, aviso limpo).</summary>
    public event EventHandler? Changed;

    /// <summary>Mensagem que está na TV agora, ou null se não há nenhuma.</summary>
    public string? CurrentMessage { get; private set; }

    public bool IsShowing => CurrentMessage is not null;

    public TimeSpan TimeLeft =>
        IsShowing && _endsAtUtc > DateTime.UtcNow ? _endsAtUtc - DateTime.UtcNow : TimeSpan.Zero;

    /// <summary>Mostra uma mensagem na TV. Se já houver uma, ela é trocada e o tempo recomeça.</summary>
    public void Show(string message, TimeSpan duration, MonitorInfo tvMonitor)
    {
        CurrentMessage = message.Trim();
        _endsAtUtc = DateTime.UtcNow + duration;

        _overlay.ShowAlert(CurrentMessage, duration, tvMonitor);
        _clock.Start();

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Tira o aviso da TV agora.</summary>
    public void Clear()
    {
        _clock.Stop();

        if (!IsShowing)
            return;

        CurrentMessage = null;
        _overlay.HideAlert();

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnClockTick()
    {
        if (TimeLeft <= TimeSpan.Zero)
            Clear();
        else
            Changed?.Invoke(this, EventArgs.Empty);
    }
}

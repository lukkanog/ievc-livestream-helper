using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using IevcLivestreamHelper.Monitors;
using IevcLivestreamHelper.Native;

namespace IevcLivestreamHelper.Views;

/// <summary>
/// Mostra e esconde o aviso na TV, com as animações.
/// Quem decide QUANDO mostrar/esconder é o <see cref="Alerts.AlertService"/>.
/// </summary>
public partial class OverlayWindow : Window
{
    /// <summary>
    /// Distância (abaixo da tela) de onde o cartão entra e para onde ele sai.
    /// O cartão fica na parte de baixo da TV.
    /// </summary>
    private const double OffscreenY = 700;

    private readonly nint _handle;

    /// <summary>
    /// O projetor do OBS também pode ficar "sempre no topo". Enquanto há aviso,
    /// reafirmamos a cada segundo que o aviso fica por cima de tudo.
    /// </summary>
    private readonly DispatcherTimer _keepOnTopTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    /// <summary>
    /// Aumenta a cada aviso novo. Serve para a animação de saída de um aviso
    /// antigo não esconder um aviso novo que chegou no meio dela.
    /// </summary>
    private int _alertNumber;

    public OverlayWindow()
    {
        InitializeComponent();

        _handle = new WindowInteropHelper(this).EnsureHandle();
        MakeClickThroughAndNeverFocused();
        HideFromScreenCapture();

        _keepOnTopTimer.Tick += (_, _) => BringToFront();
    }

    /// <summary>Mostra (ou troca) a mensagem na TV.</summary>
    public void ShowAlert(string message, TimeSpan duration, MonitorInfo monitor)
    {
        _alertNumber++;
        MessageText.Text = message;

        if (!IsVisible)
        {
            Card.Opacity = 0; // evita "piscar" o cartão antes da animação começar
            Show();
        }

        MoveToMonitor(monitor);
        BringToFront();
        _keepOnTopTimer.Start();

        PlayEntrance();
        StartBellSwing();
        StartPulse();
        StartTimeLeftBar(duration);
    }

    /// <summary>Tira o aviso da TV (com animação de saída).</summary>
    public void HideAlert()
    {
        if (!IsVisible)
            return;

        var alertBeingHidden = _alertNumber;

        var slideOut = new DoubleAnimation(OffscreenY, TimeSpan.FromMilliseconds(450))
        {
            EasingFunction = new BackEase { EasingMode = EasingMode.EaseIn, Amplitude = 0.3 },
        };
        slideOut.Completed += (_, _) =>
        {
            // Se um aviso novo chegou durante a saída, não esconda.
            if (alertBeingHidden == _alertNumber)
                HideWindowAndStopAnimations();
        };

        CardSlide.BeginAnimation(TranslateTransform.YProperty, slideOut);
        Card.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(450)));
    }

    // ----- Animações -----

    /// <summary>O cartão sobe de baixo e dá uma leve "quicada".</summary>
    private void PlayEntrance()
    {
        CardSlide.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(OffscreenY, 0, TimeSpan.FromMilliseconds(750))
        {
            EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.45 },
        });
        Card.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(250)));
    }

    /// <summary>O sino balança para lá e para cá, descansa um pouquinho, e repete.</summary>
    private void StartBellSwing()
    {
        var swing = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(1600),
            RepeatBehavior = RepeatBehavior.Forever,
        };

        double[] angles = [0, -22, 18, -14, 10, -5, 0];
        for (var i = 0; i < angles.Length; i++)
        {
            swing.KeyFrames.Add(new EasingDoubleKeyFrame(
                angles[i],
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(i * 140)),
                new SineEase { EasingMode = EasingMode.EaseInOut }));
        }

        BellSwing.BeginAnimation(RotateTransform.AngleProperty, swing);
    }

    /// <summary>O cartão "respira" (cresce e diminui bem de leve) para chamar atenção.</summary>
    private void StartPulse()
    {
        var pulse = new DoubleAnimation(1.0, 1.03, TimeSpan.FromMilliseconds(900))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        };

        CardPulse.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
        CardPulse.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
    }

    /// <summary>A barrinha de baixo esvazia no mesmo tempo que o aviso dura.</summary>
    private void StartTimeLeftBar(TimeSpan duration)
    {
        TimeLeftBar.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1, 0, duration));
    }

    private void HideWindowAndStopAnimations()
    {
        _keepOnTopTimer.Stop();
        Hide();

        // Passar "null" para BeginAnimation para a animação.
        BellSwing.BeginAnimation(RotateTransform.AngleProperty, null);
        CardPulse.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        CardPulse.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        TimeLeftBar.BeginAnimation(ScaleTransform.ScaleXProperty, null);
    }

    // ----- Janela (Windows) -----

    /// <summary>Faz a janela cobrir exatamente o monitor escolhido.</summary>
    private void MoveToMonitor(MonitorInfo monitor)
    {
        // Chamamos duas vezes de propósito: se a TV tiver uma escala (DPI) diferente
        // do monitor anterior, o Windows reajusta o tamanho na primeira chamada.
        // A segunda garante o tamanho exato da TV.
        for (var attempt = 0; attempt < 2; attempt++)
        {
            NativeMethods.SetWindowPos(
                _handle,
                NativeMethods.HWND_TOPMOST,
                monitor.Left, monitor.Top, monitor.Width, monitor.Height,
                NativeMethods.SWP_NOACTIVATE);
        }
    }

    private void BringToFront()
    {
        NativeMethods.SetWindowPos(
            _handle,
            NativeMethods.HWND_TOPMOST,
            0, 0, 0, 0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
    }

    /// <summary>Cliques passam direto, não aparece no Alt+Tab e nunca tira o foco do OBS.</summary>
    private void MakeClickThroughAndNeverFocused()
    {
        var style = NativeMethods.GetWindowLongPtr(_handle, NativeMethods.GWL_EXSTYLE);
        style |= NativeMethods.WS_EX_LAYERED
               | NativeMethods.WS_EX_TRANSPARENT
               | NativeMethods.WS_EX_TOOLWINDOW
               | NativeMethods.WS_EX_NOACTIVATE;
        NativeMethods.SetWindowLongPtr(_handle, NativeMethods.GWL_EXSTYLE, style);
    }

    /// <summary>
    /// Segurança extra: mesmo que alguém adicione uma "Captura de tela/janela" no OBS,
    /// o aviso não aparece na transmissão. (Windows 10 versão 2004 ou mais novo.)
    /// Na TV física ele continua aparecendo normalmente.
    /// </summary>
    private void HideFromScreenCapture()
    {
        NativeMethods.SetWindowDisplayAffinity(_handle, NativeMethods.WDA_EXCLUDEFROMCAPTURE);
    }
}

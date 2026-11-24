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
///
/// Pensado para ser LEVE (o mesmo computador roda o OBS):
///   - a janela tem só o tamanho do cartão, não a TV inteira;
///   - sem sombra nem efeitos;
///   - o sino balança só algumas vezes e para;
///   - a barrinha de tempo anda poucas vezes por segundo;
///   - sem aviso na tela, a janela fica escondida e nada roda.
/// </summary>
public partial class OverlayWindow : Window
{
    /// <summary>Largura do cartão: 80% da largura da TV.</summary>
    private const double CardWidthShareOfScreen = 0.8;

    /// <summary>Distância entre o cartão e a borda de baixo da TV (em unidades do WPF).</summary>
    private const double BottomMargin = 56;

    /// <summary>Quantas vezes o sino balança antes de parar.</summary>
    private const int BellSwingCount = 3;

    /// <summary>
    /// Quadros por segundo das animações. O padrão do WPF é 60; menos que isso
    /// já fica suave e gasta bem menos processador.
    /// </summary>
    private const int AnimationFrameRate = 30;

    /// <summary>A barrinha de tempo não precisa ser suave: poucas atualizações por segundo bastam.</summary>
    private const int TimeLeftBarFrameRate = 5;

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

        PlaceAtBottomOfMonitor(monitor);
        BringToFront();
        _keepOnTopTimer.Start();

        PlayEntrance();
        StartBellSwing();
        StartTimeLeftBar(duration);
    }

    /// <summary>Tira o aviso da TV (com uma animação curta de saída).</summary>
    public void HideAlert()
    {
        if (!IsVisible)
            return;

        var alertBeingHidden = _alertNumber;

        var fadeOut = Animation(new DoubleAnimation(0, TimeSpan.FromMilliseconds(300)));
        fadeOut.Completed += (_, _) =>
        {
            // Se um aviso novo chegou durante a saída, não esconda.
            if (alertBeingHidden == _alertNumber)
                HideWindowAndStopAnimations();
        };

        Card.BeginAnimation(OpacityProperty, fadeOut);
    }

    // ----- Animações (todas curtas) -----

    /// <summary>O cartão aparece "estufando" de leve.</summary>
    private void PlayEntrance()
    {
        // Sem "quicar" para fora do tamanho: a janela tem o tamanho exato do cartão
        // e qualquer coisa maior seria cortada.
        var pop = Animation(new DoubleAnimation(0.9, 1, TimeSpan.FromMilliseconds(350))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        });

        CardPop.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
        CardPop.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
        Card.BeginAnimation(OpacityProperty, Animation(new DoubleAnimation(1, TimeSpan.FromMilliseconds(250))));
    }

    /// <summary>O sino balança algumas vezes e depois fica parado.</summary>
    private void StartBellSwing()
    {
        var swing = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(1200),
            RepeatBehavior = new RepeatBehavior(BellSwingCount),
        };

        double[] angles = [0, -22, 18, -14, 10, -5, 0];
        for (var i = 0; i < angles.Length; i++)
        {
            swing.KeyFrames.Add(new EasingDoubleKeyFrame(
                angles[i],
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(i * 140)),
                new SineEase { EasingMode = EasingMode.EaseInOut }));
        }

        BellSwing.BeginAnimation(RotateTransform.AngleProperty, Animation(swing));
    }

    /// <summary>A barrinha de baixo esvazia no mesmo tempo que o aviso dura.</summary>
    private void StartTimeLeftBar(TimeSpan duration)
    {
        var shrink = new DoubleAnimation(1, 0, duration);
        Timeline.SetDesiredFrameRate(shrink, TimeLeftBarFrameRate);
        TimeLeftBar.BeginAnimation(ScaleTransform.ScaleXProperty, shrink);
    }

    /// <summary>Limita os quadros por segundo de uma animação (veja <see cref="AnimationFrameRate"/>).</summary>
    private static T Animation<T>(T timeline) where T : Timeline
    {
        Timeline.SetDesiredFrameRate(timeline, AnimationFrameRate);
        return timeline;
    }

    private void HideWindowAndStopAnimations()
    {
        _keepOnTopTimer.Stop();
        Hide();

        // Passar "null" para BeginAnimation para a animação.
        BellSwing.BeginAnimation(RotateTransform.AngleProperty, null);
        CardPop.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        CardPop.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        TimeLeftBar.BeginAnimation(ScaleTransform.ScaleXProperty, null);
    }

    // ----- Janela (Windows) -----

    /// <summary>
    /// Coloca a janela (do tamanho do cartão) centralizada na parte de baixo da TV.
    /// As contas são feitas em pixels reais da tela, para funcionar com
    /// qualquer escala (100%, 150%...) em cada monitor.
    /// </summary>
    private void PlaceAtBottomOfMonitor(MonitorInfo monitor)
    {
        // 1) Leva a janela para dentro da TV, para ela "pegar" a escala (DPI) da TV.
        SetBounds(monitor.Left, monitor.Top, 1, 1);
        var scale = NativeMethods.GetDpiForWindow(_handle) / 96.0;

        // 2) Mede a altura do cartão com a mensagem atual na largura desejada.
        var widthPixels = (int)(monitor.Width * CardWidthShareOfScreen);
        Card.Measure(new Size(widthPixels / scale, double.PositiveInfinity));
        var heightPixels = (int)Math.Ceiling(Card.DesiredSize.Height * scale);

        // 3) Centraliza na horizontal, encostado embaixo (com uma margem).
        var left = monitor.Left + (monitor.Width - widthPixels) / 2;
        var top = monitor.Top + monitor.Height - heightPixels - (int)(BottomMargin * scale);
        SetBounds(left, top, widthPixels, heightPixels);
    }

    private void SetBounds(int left, int top, int width, int height)
    {
        NativeMethods.SetWindowPos(
            _handle,
            NativeMethods.HWND_TOPMOST,
            left, top, width, height,
            NativeMethods.SWP_NOACTIVATE);
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

using System.Runtime.InteropServices;

namespace IevcLivestreamHelper.Native;

/// <summary>
/// Chamadas diretas à API do Windows (user32.dll) que o WPF não oferece pronto:
/// lista de monitores e janela "fantasma" (que não recebe cliques).
/// </summary>
internal static class NativeMethods
{
    // ----- Estilos estendidos de janela -----

    public const int GWL_EXSTYLE = -20;

    /// <summary>Cliques "atravessam" a janela e vão para o que estiver embaixo.</summary>
    public const int WS_EX_TRANSPARENT = 0x00000020;

    /// <summary>Não aparece na barra de tarefas nem no Alt+Tab.</summary>
    public const int WS_EX_TOOLWINDOW = 0x00000080;

    /// <summary>Necessário para janelas com transparência.</summary>
    public const int WS_EX_LAYERED = 0x00080000;

    /// <summary>Nunca rouba o foco (o OBS continua com o teclado).</summary>
    public const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    public static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    public static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    // ----- Posição e "sempre no topo" -----

    public static readonly nint HWND_TOPMOST = new(-1);

    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    // ----- Trazer uma janela para a frente -----

    /// <summary>Restaura a janela se estiver minimizada.</summary>
    public const int SW_RESTORE = 9;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(nint hWnd);

    // ----- Esconder a janela de capturas de tela (ex.: "Captura de tela" do OBS) -----

    public const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowDisplayAffinity(nint hWnd, uint dwAffinity);

    // ----- Monitores -----

    public const uint MONITORINFOF_PRIMARY = 0x00000001;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    public delegate bool MonitorEnumProc(nint hMonitor, nint hdcMonitor, ref RECT lprcMonitor, nint dwData);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumDisplayMonitors(nint hdc, nint lprcClip, MonitorEnumProc lpfnEnum, nint dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFOEX lpmi);
}

using System.Windows;
using System.Windows.Interop;
using IevcLivestreamHelper.Native;

namespace IevcLivestreamHelper.Hotkeys;

/// <summary>
/// Registra atalhos de teclado que funcionam em qualquer programa —
/// inclusive quando o OBS está em primeiro plano.
/// </summary>
public sealed class GlobalHotkeyManager : IDisposable
{
    private readonly nint _windowHandle;
    private readonly Dictionary<int, Action> _actionsById = new();
    private int _nextId = 1;

    /// <param name="window">Janela que vai receber os atalhos. Precisa já ter sido aberta.</param>
    public GlobalHotkeyManager(Window window)
    {
        _windowHandle = new WindowInteropHelper(window).EnsureHandle();
        HwndSource.FromHwnd(_windowHandle).AddHook(OnWindowMessage);
    }

    /// <summary>
    /// Registra um atalho. Retorna false se o Windows recusar
    /// (geralmente porque outro programa já usa a mesma combinação).
    /// </summary>
    public bool Register(Hotkey hotkey, Action action)
    {
        var id = _nextId++;
        var modifiers = hotkey.Modifiers | NativeMethods.MOD_NOREPEAT;

        if (!NativeMethods.RegisterHotKey(_windowHandle, id, modifiers, hotkey.VirtualKeyCode))
            return false;

        _actionsById[id] = action;
        return true;
    }

    /// <summary>Remove todos os atalhos (usado antes de recarregar a configuração).</summary>
    public void UnregisterAll()
    {
        foreach (var id in _actionsById.Keys)
            NativeMethods.UnregisterHotKey(_windowHandle, id);

        _actionsById.Clear();
    }

    public void Dispose() => UnregisterAll();

    private nint OnWindowMessage(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message == NativeMethods.WM_HOTKEY && _actionsById.TryGetValue((int)wParam, out var action))
        {
            action();
            handled = true;
        }

        return 0;
    }
}

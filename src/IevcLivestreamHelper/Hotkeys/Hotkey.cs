using System.Windows.Input;
using IevcLivestreamHelper.Native;

namespace IevcLivestreamHelper.Hotkeys;

/// <summary>
/// Um atalho de teclado, como "Ctrl+Alt+1" ou "F9".
/// </summary>
public sealed record Hotkey(uint Modifiers, Key Key, string Text)
{
    /// <summary>
    /// Lê um atalho escrito como texto. Exemplos aceitos:
    /// "Ctrl+Alt+1", "Ctrl+Shift+A", "F9", "Alt+F2".
    /// </summary>
    public static bool TryParse(string? text, out Hotkey? hotkey)
    {
        hotkey = null;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        uint modifiers = 0;
        Key? key = null;

        foreach (var rawPart in text.Split('+'))
        {
            var part = rawPart.Trim();
            switch (part.ToLowerInvariant())
            {
                case "ctrl" or "control":
                    modifiers |= NativeMethods.MOD_CONTROL;
                    break;
                case "alt":
                    modifiers |= NativeMethods.MOD_ALT;
                    break;
                case "shift":
                    modifiers |= NativeMethods.MOD_SHIFT;
                    break;
                case "win" or "windows":
                    modifiers |= NativeMethods.MOD_WIN;
                    break;
                default:
                    if (key is not null)
                        return false; // duas teclas "normais" no mesmo atalho
                    key = ParseKey(part);
                    if (key is null)
                        return false;
                    break;
            }
        }

        if (key is null)
            return false;

        hotkey = new Hotkey(modifiers, key.Value, text.Trim());
        return true;
    }

    /// <summary>Código da tecla no formato que o Windows entende.</summary>
    public uint VirtualKeyCode => (uint)KeyInterop.VirtualKeyFromKey(Key);

    public override string ToString() => Text;

    /// <summary>Aceita "1" (vira D1), "A", "F9", "NumPad1" etc.</summary>
    private static Key? ParseKey(string name)
    {
        if (name.Length == 1 && char.IsDigit(name[0]))
            name = "D" + name;

        return Enum.TryParse<Key>(name, ignoreCase: true, out var key) && key != Key.None
            ? key
            : null;
    }
}

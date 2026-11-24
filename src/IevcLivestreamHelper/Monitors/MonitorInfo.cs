namespace IevcLivestreamHelper.Monitors;

/// <summary>
/// Um monitor conectado ao computador.
/// As medidas estão em pixels reais da tela (não em unidades do WPF).
/// </summary>
public sealed record MonitorInfo(
    string DeviceName,
    int Number,
    int Left,
    int Top,
    int Width,
    int Height,
    bool IsPrimary)
{
    /// <summary>Texto mostrado na lista "Tela da TV".</summary>
    public string DisplayName =>
        $"Tela {Number} — {Width}×{Height}" + (IsPrimary ? " (principal)" : "");

    public override string ToString() => DisplayName;
}

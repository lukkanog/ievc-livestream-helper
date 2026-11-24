using System.Text.Json.Serialization;

namespace IevcLivestreamHelper.Settings;

/// <summary>
/// Tudo que o app guarda no arquivo "configuracoes.json" (ao lado do .exe).
/// Os nomes entre aspas são os que aparecem no arquivo, em português,
/// para facilitar a edição à mão.
/// </summary>
public sealed class AppSettings
{
    public const int MinAlertDurationSeconds = 5;
    public const int MaxAlertDurationSeconds = 300;

    /// <summary>Quanto tempo o aviso fica na TV antes de sumir sozinho.</summary>
    [JsonPropertyName("duracaoDoAvisoEmSegundos")]
    public int AlertDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Nome interno do monitor da TV no Windows (ex.: "\\.\DISPLAY2").
    /// Escolhido pela lista "Tela da TV" no app; vazio = escolher automaticamente.
    /// </summary>
    [JsonPropertyName("telaDaTv")]
    public string? TvMonitorDeviceName { get; set; }

    /// <summary>Atalho para tirar o aviso da TV.</summary>
    [JsonPropertyName("atalhoParaLimpar")]
    public string ClearHotkey { get; set; } = "Ctrl+Alt+0";

    /// <summary>Botões de mensagem rápida.</summary>
    [JsonPropertyName("mensagensRapidas")]
    public List<QuickMessage> QuickMessages { get; set; } =
    [
        new() { Text = "O áudio não está chegando", Hotkey = "Ctrl+Alt+1" },
        new() { Text = "Favor colocar o fone/headset", Hotkey = "Ctrl+Alt+2" },
    ];

    /// <summary>Corrige valores fora do esperado (ex.: alguém digitou 0 segundos no arquivo).</summary>
    public void Normalize()
    {
        AlertDurationSeconds = Math.Clamp(AlertDurationSeconds, MinAlertDurationSeconds, MaxAlertDurationSeconds);
        QuickMessages = QuickMessages
            .Where(message => !string.IsNullOrWhiteSpace(message.Text))
            .ToList();
    }
}

/// <summary>Uma mensagem pronta, com atalho opcional.</summary>
public sealed class QuickMessage
{
    [JsonPropertyName("texto")]
    public string Text { get; set; } = "";

    /// <summary>Ex.: "Ctrl+Alt+1". Pode ficar vazio.</summary>
    [JsonPropertyName("atalho")]
    public string? Hotkey { get; set; }
}

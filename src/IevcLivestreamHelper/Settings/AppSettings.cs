using System.Text.Json.Serialization;

namespace IevcLivestreamHelper.Settings;

/// <summary>
/// Tudo que o app guarda no arquivo "configuracoes.json".
/// Os nomes entre aspas são os que aparecem no arquivo, em português,
/// para facilitar a edição à mão.
/// </summary>
public sealed class AppSettings
{
    public const int MinAlertDurationSeconds = 5;
    public const int MaxAlertDurationSeconds = 300;

    /// <summary>Quanto tempo o aviso fica na TV antes de sumir sozinho.</summary>
    [JsonPropertyName("duracaoDoAvisoEmSegundos")]
    public int AlertDurationSeconds { get; set; } = 15;

    /// <summary>
    /// Nome interno do monitor da TV no Windows (ex.: "\\.\DISPLAY2").
    /// Escolhido pela lista "Tela da TV" no app; vazio = escolher automaticamente.
    /// </summary>
    [JsonPropertyName("telaDaTv")]
    public string? TvMonitorDeviceName { get; set; }

    /// <summary>Botões de mensagem rápida.</summary>
    [JsonPropertyName("mensagensRapidas")]
    public List<QuickMessage> QuickMessages { get; set; } =
    [
        new() { Text = "O áudio não está chegando" },
        new() { Text = "Favor colocar o fone/headset" },
    ];

    /// <summary>
    /// Onde a janelinha do operador estava na última vez (para abrir no mesmo lugar).
    /// Vazio = canto inferior direito da tela principal.
    /// </summary>
    [JsonPropertyName("posicaoDaJanela")]
    public WindowPosition? ControlWindowPosition { get; set; }

    /// <summary>Corrige valores fora do esperado (ex.: alguém digitou 0 segundos no arquivo).</summary>
    public void Normalize()
    {
        AlertDurationSeconds = Math.Clamp(AlertDurationSeconds, MinAlertDurationSeconds, MaxAlertDurationSeconds);
        QuickMessages = QuickMessages
            .Where(message => !string.IsNullOrWhiteSpace(message.Text))
            .ToList();
    }
}

/// <summary>Uma mensagem pronta, que vira um botão na janela do operador.</summary>
public sealed class QuickMessage
{
    [JsonPropertyName("texto")]
    public string Text { get; set; } = "";
}

/// <summary>Posição de uma janela na área de trabalho (em unidades do WPF).</summary>
public sealed class WindowPosition
{
    [JsonPropertyName("esquerda")]
    public double Left { get; set; }

    [JsonPropertyName("topo")]
    public double Top { get; set; }
}

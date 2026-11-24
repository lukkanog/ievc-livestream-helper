using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace IevcLivestreamHelper.Settings;

/// <summary>
/// Lê e grava o arquivo "configuracoes.json", que fica na mesma pasta do .exe
/// (assim o app é portátil: basta copiar a pasta).
/// </summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        // Mantém acentos legíveis no arquivo ("áudio" em vez de "\u00E1udio").
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public string FilePath { get; } = Path.Combine(AppContext.BaseDirectory, "configuracoes.json");

    /// <summary>
    /// Carrega a configuração. Se o arquivo não existir, cria um com os valores padrão.
    /// Se o arquivo estiver com erro, usa os padrões e devolve a mensagem em <paramref name="error"/>
    /// (sem apagar o arquivo, para a pessoa poder corrigir).
    /// </summary>
    public AppSettings Load(out string? error)
    {
        error = null;

        if (!File.Exists(FilePath))
        {
            var defaults = new AppSettings();
            TrySave(defaults);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            settings.Normalize();
            return settings;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            error = $"Não consegui ler \"{Path.GetFileName(FilePath)}\": {ex.Message}";
            return new AppSettings();
        }
    }

    /// <summary>Grava a configuração. Retorna false se não for possível (ex.: pasta só de leitura).</summary>
    public bool TrySave(AppSettings settings)
    {
        try
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, JsonOptions));
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}

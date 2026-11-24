using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace IevcLivestreamHelper.Settings;

/// <summary>
/// Lê e grava o arquivo "configuracoes.json".
///
/// Onde ele fica:
///   - Versão portátil (o .exe numa pasta qualquer): na mesma pasta do .exe,
///     assim basta copiar a pasta para levar tudo junto.
///   - Versão instalada (.msi em "Arquivos de Programas", onde o Windows não
///     deixa gravar): em %AppData%\IEVC Livestream Helper.
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

    private const string FileName = "configuracoes.json";

    public string FilePath { get; } = ChooseFilePath();

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

    /// <summary>Decide onde o arquivo de configuração fica (veja o comentário da classe).</summary>
    private static string ChooseFilePath()
    {
        var besideExe = Path.Combine(AppContext.BaseDirectory, FileName);
        if (File.Exists(besideExe) || CanWriteTo(AppContext.BaseDirectory))
            return besideExe;

        var appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "IEVC Livestream Helper");
        Directory.CreateDirectory(appDataFolder);
        return Path.Combine(appDataFolder, FileName);
    }

    /// <summary>Testa se dá para gravar na pasta criando (e apagando) um arquivo temporário.</summary>
    private static bool CanWriteTo(string folder)
    {
        try
        {
            var testFile = Path.Combine(folder, $".teste-de-escrita-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(testFile, "");
            File.Delete(testFile);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}

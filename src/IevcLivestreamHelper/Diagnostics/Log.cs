using System.Diagnostics;
using System.IO;

namespace IevcLivestreamHelper.Diagnostics;

/// <summary>
/// Registro (log) simples em arquivo texto, para entender problemas depois.
///
/// Os arquivos ficam em %LocalAppData%\IEVC Livestream Helper\logs,
/// um por dia (ex.: log-2026-11-23.txt). Os com mais de 14 dias são apagados.
///
/// Gravar no log nunca pode derrubar o app: qualquer falha aqui é ignorada.
/// </summary>
public static class Log
{
    private const int DaysToKeep = 14;
    private static readonly object WriteLock = new();

    public static string FolderPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "IEVC Livestream Helper",
        "logs");

    public static string CurrentFilePath =>
        Path.Combine(FolderPath, $"log-{DateTime.Now:yyyy-MM-dd}.txt");

    public static void Info(string message) => Write("INFO", message);

    public static void Warning(string message) => Write("AVISO", message);

    /// <summary>Registra um erro com todos os detalhes (inclusive onde no código ele aconteceu).</summary>
    public static void Error(string message, Exception? exception = null) =>
        Write("ERRO", exception is null ? message : $"{message}{Environment.NewLine}{exception}");

    /// <summary>Abre a pasta dos logs no Explorador de Arquivos.</summary>
    public static void OpenFolder()
    {
        Directory.CreateDirectory(FolderPath);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{FolderPath}\"") { UseShellExecute = true });
    }

    /// <summary>Apaga logs antigos para a pasta não crescer para sempre.</summary>
    public static void DeleteOldFiles()
    {
        try
        {
            if (!Directory.Exists(FolderPath))
                return;

            foreach (var file in Directory.GetFiles(FolderPath, "log-*.txt"))
            {
                if (File.GetLastWriteTime(file) < DateTime.Now.AddDays(-DaysToKeep))
                    File.Delete(file);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Sem problema: tentamos de novo na próxima vez que o app abrir.
        }
    }

    private static void Write(string level, string message)
    {
        try
        {
            lock (WriteLock)
            {
                Directory.CreateDirectory(FolderPath);
                File.AppendAllText(CurrentFilePath, $"{DateTime.Now:HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Não conseguir gravar o log não é motivo para atrapalhar o culto.
        }
    }
}

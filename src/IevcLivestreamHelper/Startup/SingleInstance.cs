using System.ComponentModel;
using System.Diagnostics;
using IevcLivestreamHelper.Diagnostics;
using IevcLivestreamHelper.Native;

namespace IevcLivestreamHelper.Startup;

/// <summary>
/// Garante que só um IEVC Livestream Helper rode por vez
/// (duas janelinhas mandando avisos para a mesma TV só confundem).
///
/// Se já houver um aberto:
///   - com janela: traz a janela dele para a frente;
///   - sem janela (travado, ex.: uma versão antiga que deu erro ao abrir):
///     oferece fechar o travado para abrir um novo.
/// </summary>
public sealed class SingleInstance : IDisposable
{
    private const string LockName = "IevcLivestreamHelper.SingleInstance";

    private readonly Mutex _lock;

    private SingleInstance(Mutex @lock) => _lock = @lock;

    /// <summary>Tenta ser a única instância. Retorna null se já existe outra.</summary>
    public static SingleInstance? TryAcquire()
    {
        var @lock = new Mutex(initiallyOwned: true, LockName, out var isFirstInstance);
        if (isFirstInstance)
            return new SingleInstance(@lock);

        @lock.Dispose();
        return null;
    }

    /// <summary>
    /// Espera a outra instância fechar (depois de <see cref="CloseOtherInstances"/>)
    /// e fica com a vaga. Retorna null se ela não fechar a tempo.
    /// </summary>
    public static SingleInstance? WaitAndAcquire(TimeSpan timeout)
    {
        var @lock = new Mutex(initiallyOwned: false, LockName);
        try
        {
            if (@lock.WaitOne(timeout))
                return new SingleInstance(@lock);
        }
        catch (AbandonedMutexException)
        {
            // A outra instância foi encerrada à força: a vaga passou a ser nossa.
            return new SingleInstance(@lock);
        }

        @lock.Dispose();
        return null;
    }

    /// <summary>
    /// Traz para a frente a janela da instância que já está aberta.
    /// Retorna false se ela não tem nenhuma janela visível (está travada).
    /// </summary>
    public static bool BringOtherInstanceToFront()
    {
        foreach (var process in FindOtherInstances())
        {
            using (process)
            {
                var window = process.MainWindowHandle; // 0 = sem janela visível
                if (window == 0)
                    continue;

                NativeMethods.ShowWindow(window, NativeMethods.SW_RESTORE);
                NativeMethods.SetForegroundWindow(window);
                return true;
            }
        }

        return false;
    }

    /// <summary>Encerra à força as outras instâncias (usado só quando estão travadas).</summary>
    public static void CloseOtherInstances()
    {
        foreach (var process in FindOtherInstances())
        {
            using (process)
            {
                try
                {
                    Log.Warning($"Encerrando instância travada (processo {process.Id}).");
                    process.Kill();
                    process.WaitForExit(5000);
                }
                catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
                {
                    Log.Error($"Não consegui encerrar o processo {process.Id}", ex);
                }
            }
        }
    }

    public void Dispose() => _lock.Dispose();

    private static IEnumerable<Process> FindOtherInstances()
    {
        using var current = Process.GetCurrentProcess();
        return Process.GetProcessesByName(current.ProcessName)
            .Where(process => process.Id != current.Id)
            .ToList();
    }
}

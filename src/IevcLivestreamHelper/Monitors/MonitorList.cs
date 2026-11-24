using System.Runtime.InteropServices;
using IevcLivestreamHelper.Native;

namespace IevcLivestreamHelper.Monitors;

/// <summary>Descobre quais monitores estão ligados ao computador.</summary>
public static class MonitorList
{
    /// <summary>Lista os monitores, em ordem de número (Tela 1, Tela 2...).</summary>
    public static IReadOnlyList<MonitorInfo> GetAll()
    {
        var monitors = new List<MonitorInfo>();

        bool OnMonitorFound(nint hMonitor, nint hdc, ref NativeMethods.RECT rect, nint data)
        {
            var info = new NativeMethods.MONITORINFOEX
            {
                cbSize = Marshal.SizeOf<NativeMethods.MONITORINFOEX>(),
            };

            if (NativeMethods.GetMonitorInfo(hMonitor, ref info))
            {
                var bounds = info.rcMonitor;
                monitors.Add(new MonitorInfo(
                    DeviceName: info.szDevice,
                    Number: ReadNumberFromDeviceName(info.szDevice, fallback: monitors.Count + 1),
                    Left: bounds.Left,
                    Top: bounds.Top,
                    Width: bounds.Right - bounds.Left,
                    Height: bounds.Bottom - bounds.Top,
                    IsPrimary: (info.dwFlags & NativeMethods.MONITORINFOF_PRIMARY) != 0));
            }

            return true; // continuar procurando
        }

        NativeMethods.EnumDisplayMonitors(0, 0, OnMonitorFound, 0);

        return monitors.OrderBy(m => m.Number).ToList();
    }

    /// <summary>
    /// Escolhe o monitor onde o aviso deve aparecer:
    /// o que está salvo na configuração; se ele não existir mais, o primeiro
    /// monitor que não é o principal (normalmente a TV); senão, o principal.
    /// </summary>
    public static MonitorInfo? FindTvMonitor(string? savedDeviceName)
    {
        var monitors = GetAll();

        return monitors.FirstOrDefault(m => m.DeviceName == savedDeviceName)
            ?? monitors.FirstOrDefault(m => !m.IsPrimary)
            ?? monitors.FirstOrDefault();
    }

    /// <summary>"\\.\DISPLAY2" vira 2 — o mesmo número das Configurações de Vídeo do Windows.</summary>
    private static int ReadNumberFromDeviceName(string deviceName, int fallback)
    {
        var digits = new string(deviceName.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var number) ? number : fallback;
    }
}

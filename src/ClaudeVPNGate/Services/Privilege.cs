using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace ClaudeVPNGate.Services;

public static class Privilege
{
    public static bool IsAdmin()
    {
        if (OperatingSystem.IsWindows())
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }

        try
        {
            return geteuid() == 0;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
    }

    public static void RelaunchElevated()
    {
        var exe = Environment.ProcessPath ?? "";
        if (exe.Length == 0)
            return;

        if (OperatingSystem.IsWindows())
        {
            ShellExecute(IntPtr.Zero, "runas", exe, "", null, 1);
            return;
        }

        if (OperatingSystem.IsMacOS())
        {
            var script = "do shell script \"" + exe.Replace("\"", "\\\"") + "\" with administrator privileges";
            Process.Start(new ProcessStartInfo
            {
                FileName = "osascript",
                ArgumentList = { "-e", script },
                UseShellExecute = false,
            });
            return;
        }

        var pkexec = File.Exists("/usr/bin/pkexec") ? "/usr/bin/pkexec" : "pkexec";
        Process.Start(new ProcessStartInfo
        {
            FileName = pkexec,
            ArgumentList = { exe },
            UseShellExecute = false,
        });
    }

    [DllImport("libc")]
    private static extern uint geteuid();

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int ShellExecute(IntPtr hwnd, string verb, string file, string args, string? dir, int show);
}

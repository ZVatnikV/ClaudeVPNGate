using System.Diagnostics;

namespace ClaudeVPNGate.Services;

public static class ProcessRunner
{
    public static string Run(string file, string arguments, int timeoutMs = 20000)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = file,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            process.Start();
            if (!process.WaitForExit(timeoutMs))
            {
                try { process.Kill(entireProcessTree: true); } catch { /* already gone */ }
                return "";
            }

            return process.StandardOutput.ReadToEnd() + "\n" + process.StandardError.ReadToEnd();
        }
        catch
        {
            return "";
        }
    }

    public static int RunCode(string file, string arguments, int timeoutMs = 20000)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = file,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            process.Start();
            if (!process.WaitForExit(timeoutMs))
            {
                try { process.Kill(entireProcessTree: true); } catch { /* already gone */ }
                return -1;
            }

            return process.ExitCode;
        }
        catch
        {
            return -1;
        }
    }
}

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DravusSensorPanel.Services.InfoExtractors;

/// <summary>
///     Runs small command line tools (pactl, wpctl, nvidia-smi...) used to read sensors on Linux/macOS.
/// </summary>
internal static class ProcessRunner {
    /// <returns>stdout when the process exits with code 0 before the timeout, otherwise null.</returns>
    public static string? Run(string fileName, string[] args, int timeoutMs = 1000) {
        try {
            using Process? process = Process.Start(CreateStartInfo(fileName, args, true));
            if ( process == null ) return null;

            Task<string> output = process.StandardOutput.ReadToEndAsync();
            _ = process.StandardError.ReadToEndAsync();

            if ( !process.WaitForExit(timeoutMs) ) {
                Kill(process);
                return null;
            }

            return process.ExitCode == 0 ? output.Result : null;
        }
        catch ( Exception ) {
            // Usually the tool is not installed
            return null;
        }
    }

    public static ProcessStartInfo CreateStartInfo(string fileName, string[] args, bool redirectStdout) {
        var startInfo = new ProcessStartInfo(fileName) {
            RedirectStandardOutput = redirectStdout,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach ( string arg in args ) {
            startInfo.ArgumentList.Add(arg);
        }

        // Numbers with "." as decimal separator, whatever the user language is
        startInfo.Environment["LC_ALL"] = "C";

        return startInfo;
    }

    public static void Kill(Process process) {
        try {
            if ( !process.HasExited ) {
                process.Kill(true);
            }
        }
        catch ( Exception ) {
            // Already gone
        }
    }
}

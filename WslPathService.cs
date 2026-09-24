using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace WslPathConverter
{
    internal sealed class WslPathService
    {
        public string Convert(string path, bool toWsl)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            var normalized = toWsl ? path.Replace('\\', '/') : path;
            var arguments = "wslpath " + (toWsl ? "-u " : "-w ") + QuoteArgument(normalized);
            var startInfo = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            try
            {
                using (var process = Process.Start(startInfo))
                {
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    process.WaitForExit(5000);
                    if (!process.HasExited || process.ExitCode != 0)
                        return path;
                    return output.TrimEnd('\r', '\n', '\0');
                }
            }
            catch
            {
                return path;
            }
        }

        private static string QuoteArgument(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }
    }
}

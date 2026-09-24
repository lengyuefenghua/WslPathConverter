using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WslPathConverter
{
    internal static class PathConverter
    {
        public static string ConvertClipboardText(string text, Func<string, string> converter)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var lines = text.Replace("\r\n", "\n").Split('\n');
            var changed = false;
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var leading = Regex.Match(line, "^\\s*").Value;
                var trailing = Regex.Match(line, "\\s*$").Value;
                var coreLength = line.Length - leading.Length - trailing.Length;
                if (coreLength <= 0)
                    continue;

                var core = line.Substring(leading.Length, coreLength);
                var quote = string.Empty;
                if (core.Length >= 2 && ((core[0] == '"' && core[core.Length - 1] == '"') || (core[0] == '\'' && core[core.Length - 1] == '\'')))
                {
                    quote = core.Substring(0, 1);
                    core = core.Substring(1, core.Length - 2);
                }

                var converted = converter(core);
                if (!string.IsNullOrEmpty(converted) && !string.Equals(converted, core, StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = leading + quote + converted + quote + trailing;
                    changed = true;
                }
            }

            var result = string.Join("\n", lines);
            return changed ? result.Replace("\n", text.Contains("\r\n") ? "\r\n" : "\n") : text;
        }

        public static string FormatImagePath(string path, string format)
        {
            if (format == "at")
                return "@" + path;
            if (format == "quoted")
                return "\"" + path + "\"";
            return path;
        }
    }
}

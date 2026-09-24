using System;
using System.Collections.Generic;
using System.IO;

namespace WslPathConverter
{
    internal sealed class AppSettings
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public bool ImageEnabled { get; set; }
        public string ImageFileName { get; set; }
        public string ImageSaveDirectory { get; set; }
        public string ImagePathFormat { get; set; }
        public string Hotkey { get; set; }

        public string ConfigPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Wsl Path Converter CSharp",
                    "settings.ini");
            }
        }

        public AppSettings()
        {
            ImageEnabled = true;
            ImageFileName = "clipboard.png";
            ImageSaveDirectory = Path.Combine(Path.GetTempPath(), "wsl-path-converter");
            ImagePathFormat = "plain";
            Hotkey = "Ctrl+Shift+V";
        }

        public static AppSettings Load()
        {
            var settings = new AppSettings();
            if (!File.Exists(settings.ConfigPath))
                return settings;

            foreach (var rawLine in File.ReadAllLines(settings.ConfigPath))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";") || line.StartsWith("["))
                    continue;
                var separator = line.IndexOf('=');
                if (separator <= 0)
                    continue;
                settings.values[line.Substring(0, separator).Trim()] = line.Substring(separator + 1).Trim();
            }

            settings.ImageEnabled = ReadBool(settings, "Image.Enabled", settings.ImageEnabled);
            settings.ImageFileName = SanitizeFileName(Read(settings, "Image.FileName", settings.ImageFileName));
            settings.ImageSaveDirectory = ExpandEnvironmentVariables(Read(settings, "Image.SaveDir", settings.ImageSaveDirectory));
            settings.ImagePathFormat = NormalizePathFormat(Read(settings, "Image.PathFormat", settings.ImagePathFormat));
            settings.Hotkey = Read(settings, "Hotkey.Convert", settings.Hotkey);
            return settings;
        }

        public void Save()
        {
            var directory = Path.GetDirectoryName(ConfigPath);
            Directory.CreateDirectory(directory);
            File.WriteAllLines(ConfigPath, new[]
            {
                "[Image]",
                "Enabled=" + (ImageEnabled ? "1" : "0"),
                "FileName=" + ImageFileName,
                "SaveDir=" + ImageSaveDirectory,
                "PathFormat=" + ImagePathFormat,
                "",
                "[Hotkey]",
                "Convert=" + Hotkey
            });
        }

        public static string SanitizeFileName(string value)
        {
            value = (value ?? string.Empty).Trim();
            foreach (var invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid.ToString(), string.Empty);
            if (value.Length == 0)
                return "clipboard.png";
            if (!value.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                value = Path.GetFileNameWithoutExtension(value) + ".png";
            return value;
        }

        public static string NormalizePathFormat(string value)
        {
            value = (value ?? string.Empty).Trim().ToLowerInvariant();
            if (value == "at" || value == "prefix" || value == "@")
                return "at";
            if (value == "quoted" || value == "quote")
                return "quoted";
            return "plain";
        }

        private static string Read(AppSettings settings, string key, string fallback)
        {
            string value;
            return settings.values.TryGetValue(key, out value) && value.Length > 0 ? value : fallback;
        }

        private static bool ReadBool(AppSettings settings, string key, bool fallback)
        {
            var value = Read(settings, key, fallback ? "1" : "0");
            return value != "0" && !value.Equals("false", StringComparison.OrdinalIgnoreCase);
        }

        private static string ExpandEnvironmentVariables(string value)
        {
            return Environment.ExpandEnvironmentVariables(value ?? string.Empty);
        }
    }
}

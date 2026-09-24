using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WslPathConverter
{
    internal static class AutoStartService
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "WslPathConverter";

        public static bool IsEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey, false))
                {
                    if (key == null)
                        return false;
                    var value = key.GetValue(ValueName) as string;
                    return !string.IsNullOrEmpty(value) && string.Equals(value, BuildCommand(), StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return false;
            }
        }

        public static void SetEnabled(bool enabled)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (key == null)
                    throw new InvalidOperationException("无法打开注册表启动项。");
                if (enabled)
                    key.SetValue(ValueName, BuildCommand());
                else if (key.GetValue(ValueName) != null)
                    key.DeleteValue(ValueName, false);
            }
        }

        private static string BuildCommand()
        {
            return "\"" + Application.ExecutablePath + "\"";
        }
    }
}

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WslPathConverter
{
    internal sealed class TrayApplicationContext : ApplicationContext
    {
        private const int HotkeyId = 1001;
        private readonly NotifyIcon tray;
        private readonly ContextMenuStrip menu;
        private readonly ToolStripMenuItem imageMenu;
        private readonly ToolStripMenuItem imageEnabledItem;
        private readonly ToolStripMenuItem pathFormatMenu;
        private readonly ToolStripMenuItem hotkeyLabel;
        private readonly AppSettings settings;
        private readonly WslPathService wsl = new WslPathService();
        private readonly ClipboardImageService imageService = new ClipboardImageService();
        private readonly GlobalHotkeyWindow hotkeyWindow;
        private IntPtr originalKeyboardLayout;

        public TrayApplicationContext()
        {
            settings = AppSettings.Load();
            menu = new ContextMenuStrip();
            menu.Items.Add("WSL Path Converter", null, delegate { });
            menu.Items[0].Enabled = false;
            menu.Items.Add(new ToolStripSeparator());
            var distro = DetectDefaultDistro();
            var distroItem = menu.Items.Add("Default distro: " + distro);
            distroItem.Enabled = false;
            hotkeyLabel = new ToolStripMenuItem("Hotkey: " + settings.Hotkey);
            menu.Items.Add(hotkeyLabel);
            hotkeyLabel.Enabled = false;
            menu.Items.Add(new ToolStripSeparator());

            var hotkeyMenu = new ToolStripMenuItem("Conversion hotkey");
            hotkeyMenu.DropDownItems.Add("Set conversion hotkey...", null, SetHotkey);
            hotkeyMenu.DropDownItems.Add("Reset to Ctrl+Shift+V", null, ResetHotkey);
            menu.Items.Add(hotkeyMenu);

            imageMenu = new ToolStripMenuItem("Image processing");
            imageEnabledItem = new ToolStripMenuItem("Enable image support", null, ToggleImageSupport);
            imageMenu.DropDownItems.Add(imageEnabledItem);
            imageMenu.DropDownItems.Add("Set file name...", null, SetImageFileName);
            imageMenu.DropDownItems.Add("Set save directory...", null, SetImageSaveDirectory);
            pathFormatMenu = new ToolStripMenuItem("Path format");
            pathFormatMenu.DropDownItems.Add("Plain path", null, delegate { SetPathFormat("plain"); });
            pathFormatMenu.DropDownItems.Add("@ prefix", null, delegate { SetPathFormat("at"); });
            pathFormatMenu.DropDownItems.Add("Double quotes", null, delegate { SetPathFormat("quoted"); });
            imageMenu.DropDownItems.Add(pathFormatMenu);
            menu.Items.Add(imageMenu);
            UpdateImageMenu();

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Open configuration folder", null, OpenConfigurationFolder);
            menu.Items.Add("Exit", null, delegate { ExitThread(); });

            tray = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "WSL Path Converter",
                ContextMenuStrip = menu,
                Visible = true
            };
            tray.DoubleClick += delegate { tray.ShowBalloonTip(1500, "WSL Path Converter", "Press " + settings.Hotkey + " to convert and paste.", ToolTipIcon.Info); };

            hotkeyWindow = new GlobalHotkeyWindow(HotkeyId, HandleHotkey);
            RegisterConfiguredHotkey();
            tray.ShowBalloonTip(1500, "WSL Path Converter", "Ready. Hotkey: " + settings.Hotkey, ToolTipIcon.Info);
        }

        private void HandleHotkey()
        {
            if (settings.ImageEnabled && IsClipboardImage())
            {
                var target = Path.Combine(settings.ImageSaveDirectory, settings.ImageFileName);
                string actual;
                string error;
                if (!imageService.TrySave(target, out actual, out error))
                {
                    Notify(error, ToolTipIcon.Error);
                    return;
                }

                var convertedImagePath = wsl.Convert(actual, true);
                if (string.Equals(convertedImagePath, actual, StringComparison.OrdinalIgnoreCase))
                {
                    Notify("Could not convert image path: " + actual, ToolTipIcon.Error);
                    return;
                }
                PasteText(PathConverter.FormatImagePath(convertedImagePath, settings.ImagePathFormat), true);
                if (!string.Equals(actual, target, StringComparison.OrdinalIgnoreCase))
                    Notify("Image file was locked; saved as: " + actual, ToolTipIcon.Info);
                return;
            }

            string text;
            try { text = Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty; }
            catch { text = string.Empty; }

            var converted = PathConverter.ConvertClipboardText(text, ConvertPath);
            if (!string.Equals(converted, text, StringComparison.Ordinal))
            {
                PasteText(converted, false);
                return;
            }

            SendKeys.SendWait("^+v");
        }

        private string ConvertPath(string path)
        {
            if (Regex.IsMatch(path, "^[A-Za-z]:[\\\\/]"))
                return wsl.Convert(path, true);
            if (Regex.IsMatch(path, "^/mnt/[A-Za-z](/|$)") || Regex.IsMatch(path, "^/(home|etc|usr|var|tmp|opt|root|srv|bin|sbin|lib|lib64|dev|proc|sys|run|media|boot|snap)(/|$)"))
                return wsl.Convert(path, false);
            if (path.StartsWith("\\\\wsl$\\", StringComparison.OrdinalIgnoreCase) || path.StartsWith("\\\\wsl.localhost\\", StringComparison.OrdinalIgnoreCase))
                return wsl.Convert(path, true);
            return path;
        }

        private void PasteText(string text, bool protectInput)
        {
            if (protectInput)
            {
                originalKeyboardLayout = NativeMethods.GetKeyboardLayout(0);
                var english = NativeMethods.LoadKeyboardLayout("00000409", NativeMethods.KlfActivate);
                if (english != IntPtr.Zero)
                    NativeMethods.ActivateKeyboardLayout(english, 0);
            }

            IDataObject previous = null;
            try
            {
                previous = Clipboard.GetDataObject();
                Clipboard.SetText(text);
                SendKeys.SendWait("^v");
            }
            catch (Exception ex)
            {
                Notify(ex.Message, ToolTipIcon.Error);
            }
            finally
            {
                if (previous != null)
                {
                    try { Clipboard.SetDataObject(previous, true); } catch { }
                }
                if (protectInput && originalKeyboardLayout != IntPtr.Zero)
                    NativeMethods.ActivateKeyboardLayout(originalKeyboardLayout, 0);
            }
        }

        private bool IsClipboardImage()
        {
            try { return Clipboard.ContainsImage(); }
            catch { return false; }
        }

        private void RegisterConfiguredHotkey()
        {
            uint modifiers;
            Keys key;
            if (!TryParseHotkey(settings.Hotkey, out modifiers, out key) || !hotkeyWindow.Register(modifiers, key))
            {
                settings.Hotkey = "Ctrl+Shift+V";
                hotkeyWindow.Register(NativeMethods.ModControl | NativeMethods.ModShift, Keys.V);
            }
            hotkeyLabel.Text = "Hotkey: " + settings.Hotkey;
        }

        private static bool TryParseHotkey(string value, out uint modifiers, out Keys key)
        {
            modifiers = 0;
            key = Keys.None;
            foreach (var part in (value ?? string.Empty).Split('+'))
            {
                var token = part.Trim();
                if (token.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || token.Equals("Control", StringComparison.OrdinalIgnoreCase)) modifiers |= NativeMethods.ModControl;
                else if (token.Equals("Alt", StringComparison.OrdinalIgnoreCase)) modifiers |= NativeMethods.ModAlt;
                else if (token.Equals("Shift", StringComparison.OrdinalIgnoreCase)) modifiers |= NativeMethods.ModShift;
                else if (token.Equals("Win", StringComparison.OrdinalIgnoreCase) || token.Equals("Windows", StringComparison.OrdinalIgnoreCase)) modifiers |= NativeMethods.ModWin;
                else
                {
                    Keys parsed;
                    if (Enum.TryParse<Keys>(token, true, out parsed)) key = parsed;
                }
            }
            return key != Keys.None;
        }

        private void SetHotkey(object sender, EventArgs e)
        {
            string value;
            if (!HotkeyDialog.TryGet(null, settings.Hotkey, out value)) return;
            uint modifiers; Keys key;
            if (!TryParseHotkey(value, out modifiers, out key) || !hotkeyWindow.Register(modifiers, key))
            {
                Notify("The hotkey is invalid or already in use.", ToolTipIcon.Error);
                return;
            }
            settings.Hotkey = value;
            settings.Save();
            hotkeyLabel.Text = "Hotkey: " + value;
        }

        private void ResetHotkey(object sender, EventArgs e)
        {
            settings.Hotkey = "Ctrl+Shift+V";
            settings.Save();
            RegisterConfiguredHotkey();
        }

        private void ToggleImageSupport(object sender, EventArgs e)
        {
            settings.ImageEnabled = !settings.ImageEnabled;
            settings.Save();
            UpdateImageMenu();
        }

        private void SetImageFileName(object sender, EventArgs e)
        {
            string value;
            if (!HotkeyDialog.TryGet(null, settings.ImageFileName, out value)) return;
            settings.ImageFileName = AppSettings.SanitizeFileName(value);
            settings.Save();
        }

        private void SetImageSaveDirectory(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog { SelectedPath = settings.ImageSaveDirectory, Description = "Select the image save directory" })
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                settings.ImageSaveDirectory = dialog.SelectedPath;
                settings.Save();
            }
        }

        private void SetPathFormat(string format)
        {
            settings.ImagePathFormat = AppSettings.NormalizePathFormat(format);
            settings.Save();
            UpdateImageMenu();
        }

        private void UpdateImageMenu()
        {
            imageEnabledItem.Checked = settings.ImageEnabled;
            foreach (ToolStripMenuItem item in pathFormatMenu.DropDownItems)
                item.Checked = item.Text.Equals(settings.ImagePathFormat == "at" ? "@ prefix" : settings.ImagePathFormat == "quoted" ? "Double quotes" : "Plain path", StringComparison.Ordinal);
        }

        private void OpenConfigurationFolder(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", "/select,\"" + settings.ConfigPath + "\"");
        }

        private void Notify(string message, ToolTipIcon icon)
        {
            tray.ShowBalloonTip(3000, "WSL Path Converter", message, icon);
        }

        private static string DetectDefaultDistro()
        {
            try
            {
                var info = new ProcessStartInfo("wsl.exe", "-l -q") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
                using (var process = Process.Start(info))
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(3000);
                    foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        return line.Trim().TrimStart('*').Trim();
                }
            }
            catch { }
            return "Unknown";
        }

        protected override void ExitThreadCore()
        {
            hotkeyWindow.Dispose();
            tray.Visible = false;
            tray.Dispose();
            menu.Dispose();
            base.ExitThreadCore();
        }
    }
}

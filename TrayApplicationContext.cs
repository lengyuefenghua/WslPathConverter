using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
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
        private readonly ToolStripMenuItem autoStartItem;
        private readonly AppSettings settings;
        private readonly WslPathService wsl = new WslPathService();
        private readonly ClipboardImageService imageService = new ClipboardImageService();
        private readonly GlobalHotkeyWindow hotkeyWindow;
        private IntPtr originalKeyboardLayout;

        public TrayApplicationContext()
        {
            settings = AppSettings.Load();
            menu = new ContextMenuStrip();
            menu.RenderMode = ToolStripRenderMode.System;
            menu.Font = SystemFonts.MenuFont;
            menu.ShowImageMargin = true;
            menu.Items.Add("WSL 路径转换器", null, delegate { });
            menu.Items[0].Enabled = false;
            menu.Items.Add(new ToolStripSeparator());
            var distro = DetectDefaultDistro();
            var distroItem = menu.Items.Add("默认发行版：" + distro);
            distroItem.Enabled = false;
            hotkeyLabel = new ToolStripMenuItem("快捷键：" + settings.Hotkey);
            menu.Items.Add(hotkeyLabel);
            hotkeyLabel.Enabled = false;
            menu.Items.Add(new ToolStripSeparator());

            var hotkeyMenu = new ToolStripMenuItem("转换快捷键", MenuIcons.Get("hotkey"));
            hotkeyMenu.DropDownItems.Add("设置转换快捷键...", null, SetHotkey);
            hotkeyMenu.DropDownItems.Add("重置为 Ctrl+Shift+V", null, ResetHotkey);
            menu.Items.Add(hotkeyMenu);

            imageMenu = new ToolStripMenuItem("图片处理", MenuIcons.Get("image"));
            imageEnabledItem = new ToolStripMenuItem("启用图片支持", null, ToggleImageSupport);
            imageMenu.DropDownItems.Add(imageEnabledItem);
            imageMenu.DropDownItems.Add("设置文件名...", null, SetImageFileName);
            imageMenu.DropDownItems.Add("设置保存目录...", null, SetImageSaveDirectory);
            pathFormatMenu = new ToolStripMenuItem("路径格式", MenuIcons.Get("path"));
            pathFormatMenu.DropDownItems.Add(CreateFormatItem("纯路径", "plain"));
            pathFormatMenu.DropDownItems.Add(CreateFormatItem("@ 前缀", "at"));
            pathFormatMenu.DropDownItems.Add(CreateFormatItem("双引号", "quoted"));
            imageMenu.DropDownItems.Add(pathFormatMenu);
            menu.Items.Add(imageMenu);
            UpdateImageMenu();

            menu.Items.Add(new ToolStripSeparator());
            autoStartItem = new ToolStripMenuItem("开机自启动", MenuIcons.Get("power"), ToggleAutoStart);
            autoStartItem.Checked = AutoStartService.IsEnabled();
            menu.Items.Add(autoStartItem);
            menu.Items.Add("打开配置目录", MenuIcons.Get("folder"), OpenConfigurationFolder);
            menu.Items.Add("退出", MenuIcons.Get("exit"), delegate { ExitThread(); });
            menu.Opening += delegate { autoStartItem.Checked = AutoStartService.IsEnabled(); };

            tray = new NotifyIcon
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
                Text = "WSL 路径转换器",
                ContextMenuStrip = menu,
                Visible = true
            };
            tray.DoubleClick += delegate { tray.ShowBalloonTip(1500, "WSL 路径转换器", "按 " + settings.Hotkey + " 转换并粘贴。", ToolTipIcon.Info); };

            hotkeyWindow = new GlobalHotkeyWindow(HotkeyId, HandleHotkey);
            RegisterConfiguredHotkey();
            tray.ShowBalloonTip(1500, "WSL 路径转换器", "已就绪。快捷键：" + settings.Hotkey, ToolTipIcon.Info);
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
                    Notify("无法转换图片路径：" + actual, ToolTipIcon.Error);
                    return;
                }
                PasteText(PathConverter.FormatImagePath(convertedImagePath, settings.ImagePathFormat), true);
                if (!string.Equals(actual, target, StringComparison.OrdinalIgnoreCase))
                    Notify("图片文件被占用；已另存为：" + actual, ToolTipIcon.Info);
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
            hotkeyLabel.Text = "快捷键：" + settings.Hotkey;
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
                Notify("快捷键无效或已被占用。", ToolTipIcon.Error);
                return;
            }
            settings.Hotkey = value;
            settings.Save();
            hotkeyLabel.Text = "快捷键：" + value;
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

        private void ToggleAutoStart(object sender, EventArgs e)
        {
            try
            {
                AutoStartService.SetEnabled(!AutoStartService.IsEnabled());
            }
            catch (Exception ex)
            {
                Notify("无法修改开机自启动设置：" + ex.Message, ToolTipIcon.Error);
            }
            autoStartItem.Checked = AutoStartService.IsEnabled();
        }

        private ToolStripMenuItem CreateFormatItem(string text, string format)
        {
            var item = new ToolStripMenuItem(text);
            item.Tag = format;
            item.Click += delegate { SetPathFormat(format); };
            return item;
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
            using (var dialog = new FolderBrowserDialog { SelectedPath = settings.ImageSaveDirectory, Description = "选择图片保存目录" })
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
                item.Checked = string.Equals(item.Tag as string, settings.ImagePathFormat, StringComparison.Ordinal);
        }

        private void OpenConfigurationFolder(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", "/select,\"" + settings.ConfigPath + "\"");
        }

        private void Notify(string message, ToolTipIcon icon)
        {
            tray.ShowBalloonTip(3000, "WSL 路径转换器", message, icon);
        }

        private static string DetectDefaultDistro()
        {
            try
            {
                var info = new ProcessStartInfo("wsl.exe", "-l -q") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, StandardOutputEncoding = Encoding.Unicode };
                using (var process = Process.Start(info))
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(3000);
                    foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        return line.Trim().TrimStart('*').Trim();
                }
            }
            catch { }
            return "未知";
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

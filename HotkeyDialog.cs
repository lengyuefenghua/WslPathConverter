using System;
using System.Drawing;
using System.Windows.Forms;

namespace WslPathConverter
{
    internal sealed class HotkeyDialog : Form
    {
        private readonly TextBox input;

        private HotkeyDialog(string current)
        {
            Text = "转换快捷键";
            Font = SystemFonts.DialogFont;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(360, 125);

            Controls.Add(new Label { Left = 12, Top = 12, Width = 330, Text = "请输入修饰键和按键，例如 Ctrl+Shift+V：" });
            input = new TextBox { Left = 12, Top = 38, Width = 330, Text = current };
            Controls.Add(input);

            var save = new Button { Left = 182, Top = 78, Width = 75, Text = "保存", DialogResult = DialogResult.OK };
            var cancel = new Button { Left = 267, Top = 78, Width = 75, Text = "取消", DialogResult = DialogResult.Cancel };
            Controls.Add(save);
            Controls.Add(cancel);
            AcceptButton = save;
            CancelButton = cancel;
        }

        public static bool TryGet(IWin32Window owner, string current, out string value)
        {
            using (var dialog = new HotkeyDialog(current))
            {
                if (dialog.ShowDialog(owner) != DialogResult.OK)
                {
                    value = current;
                    return false;
                }
                value = dialog.input.Text.Trim();
                return true;
            }
        }
    }
}

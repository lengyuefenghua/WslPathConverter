using System;
using System.Windows.Forms;

namespace WslPathConverter
{
    internal sealed class GlobalHotkeyWindow : NativeWindow, IDisposable
    {
        private readonly int id;
        private bool registered;
        private readonly Action callback;

        public GlobalHotkeyWindow(int id, Action callback)
        {
            this.id = id;
            this.callback = callback;
            CreateHandle(new CreateParams());
        }

        public bool Register(uint modifiers, Keys key)
        {
            Unregister();
            registered = NativeMethods.RegisterHotKey(Handle, id, modifiers, (uint)key);
            return registered;
        }

        public void Unregister()
        {
            if (registered)
            {
                NativeMethods.UnregisterHotKey(Handle, id);
                registered = false;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WmHotkey && m.WParam.ToInt32() == id)
                callback();
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            Unregister();
            DestroyHandle();
        }
    }
}

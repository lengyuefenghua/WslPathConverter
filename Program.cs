using System;
using System.Threading;
using System.Windows.Forms;

namespace WslPathConverter
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool isOwner;
            using (var mutex = new Mutex(true, "WslPathConverter.CSharp.SingleInstance", out isOwner))
            {
                if (!isOwner)
                    return;

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayApplicationContext());
            }
        }
    }
}

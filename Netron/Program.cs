using System;
using System.Windows.Forms;

namespace Netron
{
    public static class Program
    {
        public static Log Log;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var frm = new MainWindow();
            Log.WriteLine("Application starting");
            Application.Run(frm);
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;




namespace Netron
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var frm = new MainWindow();
            MainWindow.Log.WriteLine("Application starting");
            Application.Run(frm);
        }
    }
}

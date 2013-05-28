using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Netron
{
    public partial class Log : Form
    {
        private List<string> text;
        
        public Log()
        {
            InitializeComponent();
            text = new List<string>();
        }

        private void Log_Load(object sender, EventArgs e)
        {
            UpdateLines(text.ToArray());
        }
        public void WriteLine(string str, bool debugOutput = true, bool dateStamp = true)
        {
            if (dateStamp)
            {
                text.Add(string.Format("[{0}] => {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), str));
            }
            else
                text.Add(str);
            if (debugOutput)
                Debug.WriteLine(str);
            if (IsHandleCreated && Visible)
                UpdateLines(text.ToArray());
        }
        private void UpdateLines(string[] lines)
        {
            if (textBox1.InvokeRequired)
            {
                var iar = textBox1.BeginInvoke((Action) (() => UpdateLines(lines)));
                try
                {
                    textBox1.EndInvoke(iar);
                }
                catch (Exception e)
                {
                    Debug.Print("Caught exception {0}", e.Message);
                }
            }
            else
            {
                textBox1.Lines = lines;
            }
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void Log_FormClosing(object sender, FormClosingEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }
    }
}

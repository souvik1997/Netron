using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;

namespace Netron
{
    public partial class Log : Form
    {
        private readonly List<string> _text;
        
        public Log()
        {
            InitializeComponent();
            _text = new List<string>();
        }

        private void Log_Load(object sender, EventArgs e)
        {
            UpdateLines(_text.ToArray());
        }
        public void WriteLine(string str, bool debugOutput = true, bool dateStamp = true)
        {
            _text.Add(dateStamp
                          ? string.Format("[{0}] => {1}", DateTime.Now.ToString(CultureInfo.InvariantCulture), str)
                          : str); //Add line to text buffer
            if (debugOutput) //Output to console
                Debug.WriteLine(str);
            if (IsHandleCreated && Visible) 
                UpdateLines(_text.ToArray()); //Update form
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

        private void Log_FormClosing(object sender, FormClosingEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }
    }
}

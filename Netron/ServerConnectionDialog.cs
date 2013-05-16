using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Netron
{
    public partial class ServerConnectionDialog : Form
    {
        public string Hostname;
        public ServerConnectionDialog()
        {
            InitializeComponent();
        }

        private void ServerConnectionDialog_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Hostname = textBox1.Text;
            Close();
        }
    }
}

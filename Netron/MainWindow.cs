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
    public partial class MainWindow : Form
    {
        public static Communicator Comm;
        private Grid gr;
        public MainWindow()
        {
            InitializeComponent();
            gr = new Grid(32, 32);
        }

        private void setUpServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Comm = new Communicator(gr);
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void connectToServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ServerConnectionDialog scd = new ServerConnectionDialog();
            scd.ShowDialog();
            Comm = new Communicator(gr, scd.Hostname);
        }
    }
}

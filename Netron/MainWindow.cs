using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
namespace Netron
{
    public partial class MainWindow : Form
    {
        public static Communicator Comm;
        private static Grid gr;
        private BackgroundWorker bw;
        public static Player player;
        public MainWindow()
        {
            InitializeComponent();
            gr = new Grid(32, 32);
            bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            player = new Player(0);
        }
        void Initialize()
        {
            if (Comm.Tcs == TronCommunicatorStatus.Master)
            {
                int gap = gr.Width/Comm.Players.Count;
                int x = 0;
                foreach (Player p in Comm.Players)
                {
                    Comm.Send(Comm.GeneratePacket(p, TronInstruction.MoveEntity, x, 1));
                    x += gap;
                }
            }
            Comm.Send(Comm.GeneratePacket(player, TronInstruction.DoNothing, player.XPos, player.YPos));
        }
        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            
        }
        void Draw()
        {
            if (gameWindow.Image == null)
                gameWindow.Image = new Bitmap(gameWindow.Width, gameWindow.Height);
            
            float cellWidth = gameWindow.Width / gr.Width;
            float cellHeight = gameWindow.Height / gr.Height;
            Graphics g = Graphics.FromImage(gameWindow.Image);
            foreach (TronBase tb in gr.Map)
            {
                Bitmap icon = resize(tb.Image, (int)cellWidth, (int)cellHeight);
                if (tb.Direction == TronBase.DirectionType.East)
                {
                    icon.RotateFlip(RotateFlipType.Rotate90FlipNone);
                }
                else if (tb.Direction == TronBase.DirectionType.South)
                {
                    icon.RotateFlip(RotateFlipType.Rotate180FlipNone);
                }
                else if (tb.Direction == TronBase.DirectionType.West)
                {
                    icon.RotateFlip(RotateFlipType.Rotate270FlipNone);
                }
                g.DrawImage(icon, new PointF(tb.XPos/gr.Width * cellWidth, tb.YPos/gr.Height * cellHeight));
            }
        }
        private Bitmap resize(Bitmap src, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
                g.DrawImage(src, 0, 0, width, height);
            return result;
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

        private void gameWindow_Click(object sender, EventArgs e)
        {

        }

        private void keyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left)
            {
                player.AcceptUserInput(TronBase.DirectionType.West);
            }
            else if (e.KeyCode == Keys.Right)
            {
                player.AcceptUserInput(TronBase.DirectionType.East);
            }            
        }

        private void keyUp(object sender, KeyEventArgs e)
        {
            
        }
    }
}

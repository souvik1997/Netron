using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Netron
{
    public partial class MainWindow : Form
    {
        public static Communicator Comm;
        private static Grid _gr;
        private readonly BackgroundWorker _bw;
        public static Player MePlayer;
        public MainWindow()
        {
            InitializeComponent();
            _gr = new Grid(20, 20);
            _bw = new BackgroundWorker();
            _bw.DoWork += bw_DoWork;
            MePlayer = new Player(0);
            MePlayer.Color = Color.Fuchsia;
            MePlayer.PutSelfInGrid(_gr, 2, 3);
        }
        void Initialize()
        {
            if (Comm.Tcs == TronCommunicatorStatus.Master)
            {
                int gap = _gr.Width/Comm.Players.Count;
                int x = 0;
                foreach (Player p in Comm.Players)
                {
                    Comm.Send(Comm.GeneratePacket(p, TronInstruction.MoveEntity, x, 1));
                    x += gap;
                }
            }
            Comm.Send(Comm.GeneratePacket(MePlayer, TronInstruction.DoNothing, MePlayer.XPos, MePlayer.YPos));
        }
        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            
        }
        void Draw()
        {
            if (gameWindow.Image == null)
                gameWindow.Image = new Bitmap(gameWindow.Width, gameWindow.Height);
            
            float cellWidth = (float)gameWindow.Width / _gr.Width;
            float cellHeight = (float)gameWindow.Height / _gr.Height;
            Graphics g = Graphics.FromImage(gameWindow.Image);
            g.Clear(Color.Transparent);
            float testx = 0;
            float testy = 0;
            for(testx = 0; testx < cellWidth*_gr.Width;testx+=cellWidth)
            {
                for(testy = 0; testy < cellHeight*_gr.Height;testy+=cellHeight)
                {
                    g.DrawRectangle(new Pen(Brushes.CadetBlue), testx, testy, testx + cellWidth, testy + cellHeight);
                }
            }

            foreach (TronBase tb in _gr.Map)
            {
                if (tb != null)
                {
                    
                    float x = (float) tb.XPos*cellWidth;
                    float y = (float) tb.YPos*cellHeight;
                    
                    if (tb.Image != null)
                    {
                        Bitmap icon = resize(tb.Image, (int)cellWidth, (int)cellHeight);
                        g.DrawImage(icon,
                                    new PointF(x, y));
                    }
                }
            }
            gameWindow.Refresh();
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
            Comm = new Communicator(_gr);
            SetupEventHandlers();
        }
        private void SetupEventHandlers()
        {
            Comm.OnInitComplete += Comm_OnInitComplete;
            Comm.OnNewPlayerConnect += Comm_OnNewPlayerConnect;
            Comm.OnPlayerDisconnect += Comm_OnPlayerDisconnect;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            MePlayer.Act();
            Draw();
        }

        private void connectToServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ServerConnectionDialog scd = new ServerConnectionDialog();
            scd.ShowDialog();
            Comm = new Communicator(_gr, scd.Hostname);
            SetupEventHandlers();
        }

        void Comm_OnPlayerDisconnect(object sender, EventArgs e)
        {
            
        }

        void Comm_OnNewPlayerConnect(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Player connected! Now " + Comm.Players.Count + " players connected";
        }

        void Comm_OnInitComplete(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Initialization complete";
        }

        private void gameWindow_Click(object sender, EventArgs e)
        {

        }

        private void keyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left)
            {
                MePlayer.AcceptUserInput(TronBase.DirectionType.West);
            }
            else if (e.KeyCode == Keys.Right)
            {
                MePlayer.AcceptUserInput(TronBase.DirectionType.East);
            }            
        }

        private void keyUp(object sender, KeyEventArgs e)
        {
            
        }
    }
}

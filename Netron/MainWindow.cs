//#define DRAW_GRID
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Data;
namespace Netron
{
    public partial class MainWindow : Form
    {
        
        public static Communicator Comm;
        private static Grid _gr;
        private readonly BackgroundWorker _bw;
        public static Player MePlayer;
        private static readonly object _gLock = new object();
        public static List<Wall> Walls = new List<Wall>();
        private float cellWidth;
        private float cellHeight;

        private Bitmap bWall;
        private Bitmap bPlayers;
        private Graphics gMain;
        private Graphics gWall;
        private Graphics gPlayers;
        public MainWindow()
        {
            InitializeComponent();
            _gr = new Grid(20, 20);
            _bw = new BackgroundWorker();
            _bw.DoWork += bw_DoWork;
            MePlayer = new Player(0) {Color = Color.Tomato};
            MePlayer.PutSelfInGrid(_gr, 2, 3);
            cellWidth = (float)gameWindow.Width / _gr.Width;
            cellHeight = (float)gameWindow.Height / _gr.Height;
            gameWindow.Image = new Bitmap(gameWindow.Width, gameWindow.Height);
            bWall = new Bitmap(gameWindow.Width, gameWindow.Height);
            bPlayers = new Bitmap(gameWindow.Width, gameWindow.Height); 
            gMain = Graphics.FromImage(gameWindow.Image);
            gWall = Graphics.FromImage(bWall);
            gPlayers = Graphics.FromImage(bPlayers);
            
        }
        
        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            for (int x = 0; x < 1000; x++)
            {
                foreach (Player player in Comm.Players)
                    player.Act();
                Draw();
            }
        }

        
        void Draw()
        {

            lock (_gLock)
            {                    

                
                gMain.Clear(Color.Transparent);
                gPlayers.Clear(Color.Transparent);
#if DRAW_GRID
                float testx = 0;
                float testy = 0;
                for (testx = 0; testx < cellWidth*_gr.Width; testx += cellWidth)
                {
                    for (testy = 0; testy < cellHeight*_gr.Height; testy += cellHeight)
                    {
                        g.DrawRectangle(new Pen(Brushes.DimGray), testx, testy, testx + cellWidth,
                                        testy + cellHeight);
                    }
                }
#endif
                foreach (Wall tb in Walls)
                {
                    if (tb != null && tb.Image != null)
                    {

                        float x = tb.XPos * cellWidth;
                        float y = tb.YPos * cellHeight;


                        Bitmap icon = resize(tb.Image, (int)cellWidth + 1, (int)cellHeight + 1);
                        gWall.DrawImage(icon,
                                new PointF((int)Math.Round(x), (int)Math.Round(y)));

                    }
                }
                foreach (Player tb in Comm.Players)
                {
                    if (tb != null && tb.Image != null)
                    {

                        float x = tb.XPos * cellWidth;
                        float y = tb.YPos * cellHeight;

                        
                        Bitmap icon = resize(tb.Image, (int)cellWidth + 1, (int)cellHeight + 1);
                        gPlayers.DrawImage(icon,
                                new PointF((int)Math.Round(x), (int)Math.Round(y)));
                        
                    }
                }
                gMain.DrawImage(bPlayers, 0, 0);
                gMain.DrawImage(bWall, 0, 0);
                Walls.Clear();
                RefreshGameWindow();
            }
        }
        void RefreshGameWindow()
        {
            if (gameWindow.InvokeRequired)
            {
                var asyncRes = BeginInvoke (new MethodInvoker(RefreshGameWindow));
                try
                {
                    EndInvoke(asyncRes);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Caught exception {0}", e.Message);
                }
            }
            else
            {
                gameWindow.Refresh();
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
            Player p = new Player(1) { Color = Color.BlueViolet };
            p.PutSelfInGrid(_gr, 5, 4);
            Comm.Players.Add(p);
            p = new Player(2) { Color = Color.DarkGreen };
            p.PutSelfInGrid(_gr, 7, 8);
            Comm.Players.Add(p);
            _bw.RunWorkerAsync();
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

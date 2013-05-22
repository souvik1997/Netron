#define DRAW_GRID

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Netron
{
    public partial class MainWindow : Form
    {
        public static Communicator Comm;
        private static Grid _gr;
        public static Player MePlayer;
        private static readonly object _gLock = new object();
        public static List<Wall> Walls = new List<Wall>();
        private readonly Bitmap _bPlayers;
        private readonly Bitmap _bWall;
        private readonly BackgroundWorker _bw;
        private readonly float _cellHeight;
        private readonly float _cellWidth;
        private readonly Graphics _gMain;
        private readonly Graphics _gPlayers;
        private readonly Graphics _gWall;

        private volatile TronBase.DirectionType _nextTurn;
        public MainWindow()
        {
            InitializeComponent();
            _gr = new Grid(70, 50);
            _bw = new BackgroundWorker();
            _bw.DoWork += bw_DoWork;
            MePlayer = new Player(0) {Color = Color.Tomato};
            MePlayer.PutSelfInGrid(_gr, 2, 3);
            _cellWidth = (float) gameWindow.Width/_gr.Width;
            _cellHeight = (float) gameWindow.Height/_gr.Height;
            gameWindow.Image = new Bitmap(gameWindow.Width, gameWindow.Height);
            _bWall = new Bitmap(gameWindow.Width, gameWindow.Height);
            _bPlayers = new Bitmap(gameWindow.Width, gameWindow.Height);
            _gMain = Graphics.FromImage(gameWindow.Image);
            _gWall = Graphics.FromImage(_bWall);
            _gPlayers = Graphics.FromImage(_bPlayers);
            _nextTurn = TronBase.DirectionType.Null;
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            for (int x = 0; x < 1000; x++)
            {
                if (_nextTurn != TronBase.DirectionType.Null)
                {
                    MePlayer.AcceptUserInput(_nextTurn);
                    _nextTurn = TronBase.DirectionType.Null;
                }
                foreach (Player player in Comm.Players)
                    player.Act();
                Draw();
                Thread.Sleep(100);
            }
        }


        private void Draw()
        {
            lock (_gLock)
            {
                _gMain.Clear(Color.Transparent);
                _gPlayers.Clear(Color.Transparent);
#if DRAW_GRID
                
                for (float testx = 0; testx < _cellWidth*_gr.Width; testx += _cellWidth)
                {
                    
                    for (float testy = 0; testy < _cellHeight*_gr.Height; testy += _cellHeight)
                    {
                        _gMain.DrawRectangle(new Pen(Brushes.DimGray), testx, testy, testx + _cellWidth,
                                        testy + _cellHeight);
                    }
                }
#endif
                foreach (Wall tb in Walls)
                {
                    if (tb != null && tb.Image != null)
                    {
                        float x = tb.XPos*_cellWidth;
                        float y = tb.YPos*_cellHeight;


                        Bitmap icon = resize(tb.Image, (int) _cellWidth + 1, (int) _cellHeight + 1);
                        _gWall.DrawImage(icon,
                                         new PointF((int) Math.Round(x), (int) Math.Round(y)));
                    }
                }
                
                foreach (Player tb in Comm.Players)
                {
                    if (tb != null && tb.Image != null)
                    {
                        float x = tb.XPos*_cellWidth;
                        float y = tb.YPos*_cellHeight;


                        Bitmap icon = resize(tb.Image, (int) _cellWidth + 1, (int) _cellHeight + 1);
                        _gPlayers.DrawImage(icon,
                                            new PointF((int) Math.Round(x), (int) Math.Round(y)));
                    }
                }
                _gMain.DrawImage(_bWall, 0, 0);
                _gMain.DrawImage(_bPlayers, 0, 0);
                
                Walls.Clear();
                RefreshGameWindow();
            }
        }

        private void RefreshGameWindow()
        {
            if (gameWindow.InvokeRequired)
            {
                IAsyncResult asyncRes = BeginInvoke(new MethodInvoker(RefreshGameWindow));
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

        private static Bitmap resize(Bitmap src, int width, int height)
        {
            var result = new Bitmap(width, height);
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
            Comm.OnInitTimerTick += Comm_OnInitTimerTick;
        }

        void Comm_OnInitTimerTick(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "" + (Communicator.Timeout - Comm.ElapsedTime)/1000 + " seconds left";
        }

        private void button1_Click(object sender, EventArgs e)
        {
        }

        private void connectToServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var scd = new ServerConnectionDialog();
            scd.ShowDialog();
            Comm = new Communicator(_gr, scd.Hostname);
            SetupEventHandlers();
        }

        private void Comm_OnPlayerDisconnect(object sender, EventArgs e)
        {
        }

        private void Comm_OnNewPlayerConnect(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Player connected! Now " + Comm.Players.Count + " players connected";
        }

        private void Comm_OnInitComplete(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Initialization complete";
           /* Player p = new Player(1) {Color = Color.BlanchedAlmond};
            p.PutSelfInGrid(_gr, 6, 7);
            Comm.Players.Add(p);*/
            _bw.RunWorkerAsync();
        }

        private void gameWindow_Click(object sender, EventArgs e)
        {
        }

        private void keyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A)
            {
                _nextTurn = TronBase.DirectionType.West;
                //MePlayer.AcceptUserInput(TronBase.DirectionType.West);
            }
            else if (e.KeyCode == Keys.D)
            {
                _nextTurn = TronBase.DirectionType.East;
                //MePlayer.AcceptUserInput(TronBase.DirectionType.East);
            }
            else if (e.KeyCode == Keys.S)
            {
                _nextTurn = TronBase.DirectionType.South;
                //MePlayer.AcceptUserInput(TronBase.DirectionType.South);
            }
            else if (e.KeyCode == Keys.W)
            {
                _nextTurn = TronBase.DirectionType.North;
                //MePlayer.AcceptUserInput(TronBase.DirectionType.North);
            }
        }

        private void keyUp(object sender, KeyEventArgs e)
        {
        }
    }
}
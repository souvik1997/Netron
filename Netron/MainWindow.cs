//#define DRAW_GRID //Preprocessor statement

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Netron
{
    public partial class MainWindow : Form //The main window
    {
        public static Communicator Comm; //Static communicator
        private static Grid _gr; //Static grid
        public static Player MePlayer; //Static player
        private static readonly object _gLock = new object(); //Static object to lock drawing to only one thread
        public static List<Wall> Walls = new List<Wall>(); //static list of walls
        private readonly Bitmap _bPlayers; //Bitmap to draw players to
        private readonly Bitmap _bWall; //Bitmap to draw walls to
        private readonly BackgroundWorker _bw; //BackgroundWorker which runs in a different thread
        private readonly float _cellHeight; //Cell height and width are constant 
        private readonly float _cellWidth;
        private readonly Graphics _gMain; //Graphics objects for each bitmap buffer
        private readonly Graphics _gPlayers;
        private readonly Graphics _gWall;

        private const int SleepInterval = 10; //Constant amount of time to make the thread sleep
        public MainWindow() //Constructor
        {
            InitializeComponent(); //Initialize WinForms
            _gr = new Grid(100, 80); //Create grid
            _bw = new BackgroundWorker(); //Create background worker
            _bw.DoWork += bw_DoWork; //Set up events
            MePlayer = new Player(0) {Color = Color.Magenta}; //Create player with color
            MePlayer.PutSelfInGrid(_gr, 2, 3); //Put player in grid
            _cellWidth = (float) gameWindow.Width/_gr.Width; //Set cell width and height
            _cellHeight = (float) gameWindow.Height/_gr.Height;
            gameWindow.Image = new Bitmap(gameWindow.Width, gameWindow.Height); //Create bitmaps
            _bWall = new Bitmap(gameWindow.Width, gameWindow.Height);
            _bPlayers = new Bitmap(gameWindow.Width, gameWindow.Height);
            _gMain = Graphics.FromImage(gameWindow.Image); //Create graphics
            _gWall = Graphics.FromImage(_bWall);
            _gPlayers = Graphics.FromImage(_bPlayers);
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e) //Runs in a different thread
        {
            for (int x = 0; x < 1000; x++) //Loop
            {
                
                if (Comm.Tcs == TronCommunicatorStatus.Master) //If this is a master
                {
                    Comm.Send(Comm.GeneratePacket(MePlayer, TronInstruction.SyncToClient, MePlayer.XPos, MePlayer.YPos)); //Synchronize
                }
                Debug.WriteLine("Waiting for sync");
                Comm.SyncComplete.WaitOne(); //Wait for acknowledgement
                Debug.WriteLine("Sync complete");
                foreach (Player player in Comm.Players) //Loop through players
                {
                    if (!player.FlushTurns()) //Flush pending turns
                        player.Act(); //Act if no turns were flushed
                }
                Draw(); //Draw everything
                Thread.Sleep(SleepInterval); //sleep for an amount of time
                toolStripStatusLabel1.Text = "Frame number " + x; //Update toolstrip text
            }
        }


        private void Draw() //Draws stuff to the buffers
        {
            lock (_gLock) //Lock to prevent concurrent access
            {
                _gMain.Clear(Color.Transparent); //Clear images
                _gPlayers.Clear(Color.Transparent);
#if DRAW_GRID //If the program should draw the grid. Useful for debugging
                
                for (float testx = 0; testx < _cellWidth*_gr.Width; testx += _cellWidth) //Double for loop to go through each cell
                {
                    
                    for (float testy = 0; testy < _cellHeight*_gr.Height; testy += _cellHeight)
                    {
                        _gMain.DrawRectangle(new Pen(Brushes.DimGray), testx, testy, testx + _cellWidth,
                                        testy + _cellHeight); //Draw rectangles in each cell
                    }
                }
#endif
                foreach (Wall tb in Walls) //Go through each wall to draw
                {
                    if (tb != null && tb.Image != null) //Draw if it is not null
                    {
                        float x = tb.XPos*_cellWidth; //Get x and y coordinate
                        float y = tb.YPos*_cellHeight;


                        Bitmap icon = resize(tb.Image, (int) _cellWidth + 1, (int) _cellHeight + 1); //Resize image
                        //_gWall.FillRectangle(new SolidBrush(gameWindow.BackColor), (int)Math.Round(x)+1, (int)Math.Round(y)+1, (int)_cellWidth , (int)_cellHeight);
                        _gWall.DrawImage(icon,
                                         new PointF((int) Math.Round(x), (int) Math.Round(y))); //Draw image
                    }
                }
                
                foreach (Player tb in Comm.Players) //Go through each player to draw
                {
                    if (tb != null && tb.Image != null) //if it is not null
                    {
                        float x = tb.XPos*_cellWidth; //Get x and y coordinate
                        float y = tb.YPos*_cellHeight;


                        Bitmap icon = resize(tb.Image, (int) _cellWidth + 1, (int) _cellHeight + 1); //Resize
                        _gPlayers.DrawImage(icon, //Draw image
                                            new PointF((int) Math.Round(x), (int) Math.Round(y)));
                    }
                }
                /*foreach(TronBase tb in _gr.Map)
                {
                    if (tb != null && tb.Image != null)
                    {
                        float x = tb.XPos * _cellWidth;
                        float y = tb.YPos * _cellHeight;


                        Bitmap icon = resize(tb.Image, (int)_cellWidth + 1, (int)_cellHeight + 1);
                        _gPlayers.DrawImage(icon,
                                            new PointF((int)Math.Round(x), (int)Math.Round(y)));
                    }

                }*/
                _gMain.DrawImage(_bWall, 0, 0); //Draw buffers to main buffer
                _gMain.DrawImage(_bPlayers, 0, 0);
                
                Walls.Clear(); //Clear list
                RefreshGameWindow(); //Refresh
            }
        }

        private void RefreshGameWindow()
        {
            if (gameWindow.InvokeRequired) //If this is on a different thread
            {
                IAsyncResult asyncRes = BeginInvoke(new MethodInvoker(RefreshGameWindow)); //Invoke the same method on the other thread
                try
                {
                    EndInvoke(asyncRes); //End invocation
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Caught exception {0}", e.Message);
                }
            }
            else
            {
                gameWindow.Refresh(); //Refresh if it is on the same thread
            }
        }
        
        private static Bitmap resize(Bitmap src, int width, int height)
        {
            var result = new Bitmap(width, height); //Create new bitmap
            using (Graphics g = Graphics.FromImage(result)) //Using a graphics object from the image
                g.DrawImage(src, 0, 0, width, height); //draw the image resized
            return result; //return the bitmap
        }

        private void setUpServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Comm = new Communicator(_gr); //Create communicator


            SetupEventHandlers(); //Set up events
        }

        private void SetupEventHandlers()
        {
            Comm.OnInitComplete += Comm_OnInitComplete; //Set up events
            Comm.OnNewPlayerConnect += Comm_OnNewPlayerConnect;
            Comm.OnPlayerDisconnect += Comm_OnPlayerDisconnect;
            Comm.OnInitTimerTick += Comm_OnInitTimerTick;
        }

        void Comm_OnInitTimerTick(object sender, EventArgs e) //Called when the timer ticks
        {
            toolStripStatusLabel1.Text = "" + (Communicator.Timeout - Comm.ElapsedTime)/1000 + " seconds left"; //Write how many seconds are left
        }

        private void connectToServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var scd = new ServerConnectionDialog(); //Show a new dialog
            scd.ShowDialog();
            Comm = new Communicator(_gr, scd.Hostname); //Create a communicator using the ip address
            SetupEventHandlers();
        }

        private void Comm_OnPlayerDisconnect(object sender, EventArgs e)
        {
        }

        private void Comm_OnNewPlayerConnect(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Player connected! Now " + Comm.Players.Count + " players connected"; //Write that a player has connected
        }

        private void Comm_OnInitComplete(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Initialization complete"; //Write text
           /* Player p = new Player(1) {Color = Color.BlanchedAlmond};
            p.PutSelfInGrid(_gr, 6, 7);
            Comm.Players.Add(p);*/
            _bw.RunWorkerAsync(); //Start the other thread
        }

        private void gameWindow_Click(object sender, EventArgs e)
        {
        }

        private void keyDown(object sender, KeyEventArgs e) //Fired when a key is pressed
        {
            if (e.KeyCode == Keys.A) //if key is A
            {
                MePlayer.AcceptUserInput(TronBase.DirectionType.West); //go left
            }
            else if (e.KeyCode == Keys.D) //if key is D
            {
                MePlayer.AcceptUserInput(TronBase.DirectionType.East); //go right
            }
            else if (e.KeyCode == Keys.S) //if key is S
            {
                MePlayer.AcceptUserInput(TronBase.DirectionType.South); //go down
            }
            else if (e.KeyCode == Keys.W) //if key is W
            {
                MePlayer.AcceptUserInput(TronBase.DirectionType.North); //go up
            }
        }

        private void keyUp(object sender, KeyEventArgs e)
        {
        }
    }
}
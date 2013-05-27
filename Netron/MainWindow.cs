﻿//#define DRAW_GRID //Preprocessor statement

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace Netron
{
    public partial class MainWindow : Form //The main window
    {
        private const int SleepInterval = 80; //Constant amount of time to make the thread sleep
        public static Communicator Comm; //Static communicator
        private static Grid _gr; //Static grid
        public static Player MePlayer; //Static player
        private static readonly object _gLock = new object(); //Static object to lock drawing to only one thread
        public static List<Wall> Walls = new List<Wall>(); //static list of walls
        private readonly Bitmap _bPlayers; //Bitmap to draw players to
        private readonly Bitmap _bWall; //Bitmap to draw walls to
        private readonly BackgroundWorker _bw; //BackgroundWorker which runs in a different thread
        private float _cellHeight; //Cell height and width are constant 
        private float _cellWidth;
        private readonly Graphics _gMain; //Graphics objects for each bitmap buffer
        private readonly Graphics _gPlayers;
        private readonly Graphics _gWall;

        public MainWindow() //Constructor
        {
            InitializeComponent(); //Initialize WinForms
            _gr = new Grid(100, 80); //Create grid
            _bw = new BackgroundWorker {WorkerSupportsCancellation = true}; //Create background worker
            _bw.DoWork += bw_DoWork; //Set up events
            _bw.RunWorkerCompleted += _bw_RunWorkerCompleted;
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

        private void _bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Reinitialize();
        }

        public void Initialize()
        {
            _gr.Clear();
            MePlayer = new Player(0) {Color = Color.Magenta}; //Create player with color
            MePlayer.PutSelfInGrid(_gr, 2, 3); //Put player in grid
            _gMain.Clear(Color.Transparent);
            _gWall.Clear(Color.Transparent);
            _gPlayers.Clear(Color.Transparent);
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e) //Runs in a different thread
        {
            var worker = sender as BackgroundWorker;
            if (worker == null) return;
            int x = 0;
            while (!worker.CancellationPending)
            {
                if (Comm.Tcs == TronCommunicatorStatus.Server) //If this is a server
                {
                    Comm.Send(Comm.GeneratePacket(MePlayer, TronInstruction.SyncToClient, MePlayer.XPos, MePlayer.YPos));
                        //Synchronize
                }
                Debug.WriteLine("Waiting for sync");
                if (!Comm.SyncComplete.WaitOne(2000, false)) //Wait for acknowledgement
                {
                    if (!worker.CancellationPending)
                    {
                        string peer = Comm.Tcs == TronCommunicatorStatus.Server ? "client" : "server";
                        MessageBox.Show(
                            "Waited for 2000 milliseconds without any response from " + peer + ". Disconnecting...",
                            "The " + peer + " has disconnected.");
                    }
                    break;
                }
                Debug.WriteLine("Sync complete");
                foreach (Player player in Comm.Players) //Loop through players
                {
                    if (!player.FlushTurns()) //Flush pending turns
                        player.Act(); //Act if no turns were flushed
                }
                Draw(); //Draw everything
                Thread.Sleep(SleepInterval); //sleep for an amount of time
                toolStripStatusLabel1.Text = "Frame number " + x++; //Update toolstrip text
            }
        }


        private void Draw() //Draws stuff to the buffers
        {
            lock (_gLock) //Lock to prevent concurrent access
            {
                _gMain.Clear(Color.Black); //Clear images
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
                IAsyncResult asyncRes = BeginInvoke(new MethodInvoker(RefreshGameWindow));
                    //Invoke the same method on the other thread
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
            if (_bw.IsBusy)
            {
                MessageBox.Show("A game is already running!");
            }
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

        private void Comm_OnInitTimerTick(object sender, EventArgs e) //Called when the timer ticks
        {
            toolStripStatusLabel1.Text = "" + (Communicator.Timeout - Comm.ElapsedTime)/1000 + " seconds left";
                //Write how many seconds are left
        }

        private void connectToServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_bw.IsBusy)
            {
                MessageBox.Show("A game is already running!");
            }
            var scd = new ServerConnectionDialog(); //Show a new dialog
            scd.ShowDialog();
            if (String.IsNullOrWhiteSpace(scd.Hostname)) return;
            try
            {
                Comm = new Communicator(_gr, scd.Hostname); //Create a communicator using the ip address
            }
            catch (SocketException se)
            {
                MessageBox.Show(
                    "SocketException occurred when connecting. Please make sure the hostname/IP address is correct");
                Debug.Print("Caught SocketException {0}", se.Message);
                return;
            }
            SetupEventHandlers();
        }

        private void Comm_OnPlayerDisconnect(object sender, EventArgs e)
        {
        }

        private void Comm_OnNewPlayerConnect(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Player connected! Now " + Comm.Players.Count + " players connected";
                //Write that a player has connected
        }

        private void Comm_OnInitComplete(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Initialization complete"; //Write text
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

        public void Reinitialize()
        {
            try
            {
                Comm.Disconnect();
                _bw.CancelAsync();
                Initialize();
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught exception {0}", e.Message);
            }
        }

        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Reinitialize();
        }

        private void MainWindow_SizeChanged(object sender, EventArgs e)
        {
            _cellWidth = (float)gameWindow.Width / _gr.Width; //Set cell width and height
            _cellHeight = (float)gameWindow.Height / _gr.Height;
            lock (_gLock)
            {
                _gMain.Clear(Color.Transparent);
                RefreshGameWindow();
            }
        }
    }
}
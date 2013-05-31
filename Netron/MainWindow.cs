#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

#endregion

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
        private readonly Graphics _gMain; //Graphics objects for each bitmap buffer
        private readonly Graphics _gPlayers;
        private readonly Graphics _gWall;

        private bool _hasWonPerm; //Set if this player has won

        public MainWindow() //Constructor
        {
            InitializeComponent(); //Initialize WinForms
            Program.Log = new Log();
            _gr = new Grid(100, 80); //Create grid
            _bw = new BackgroundWorker {WorkerSupportsCancellation = true}; //Create background worker
            _bw.DoWork += bw_DoWork; //Set up events
            _bw.RunWorkerCompleted += _bw_RunWorkerCompleted;
            MePlayer = new Player(0) {Color = Color.Magenta}; //Create player with color
            MePlayer.PutSelfInGrid(_gr, 2, 3); //Put player in grid
            _gr.CellWidth = (float) gameWindow.Width/_gr.Width; //Set cell width and height
            _gr.CellHeight = (float) gameWindow.Height/_gr.Height;
            gameWindow.Image = new Bitmap(gameWindow.Width, gameWindow.Height); //Create bitmaps
            _bWall = new Bitmap(gameWindow.Width, gameWindow.Height);
            _bPlayers = new Bitmap(gameWindow.Width, gameWindow.Height);
            _gMain = Graphics.FromImage(gameWindow.Image); //Create graphics
            _gWall = Graphics.FromImage(_bWall);
            _gPlayers = Graphics.FromImage(_bPlayers);
            _hasWonPerm = false;
            EditVisibilityOfProgressBar(false);
        }

        private void _bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Reinitialize();
        }

        public void Initialize()
        {
            _gr.Clear();
            _hasWonPerm = false;
            MePlayer = new Player(0) {Color = Color.Magenta}; //Create player with color
            MePlayer.PutSelfInGrid(_gr, 2, 3); //Put player in grid
            _gMain.Clear(Color.Transparent);
            _gWall.Clear(Color.Transparent);
            _gPlayers.Clear(Color.Transparent);
        }

        private void EditText(ToolStrip ctrl, ToolStripStatusLabel label, string text)
        {
            if (ctrl.InvokeRequired)
            {
                var asyncRes = BeginInvoke((Action) (() => EditText(ctrl, label, text)));
                //Invoke the same method on the other thread
                try
                {
                    EndInvoke(asyncRes); //End invocation
                }
                catch (Exception e)
                {
                    Program.Log.WriteLine(string.Format("Caught exception {0}", e.Message));
                }
            }
            else
            {
                label.Text = text; //edit if it is on the same thread
            }
        }

        private void EditVisibilityOfProgressBar(bool val)
        {
            if (statusStrip1.InvokeRequired)
            {
                var asyncRes = BeginInvoke((Action) (() => EditVisibilityOfProgressBar(val)));
                //Invoke the same method on the other thread
                try
                {
                    EndInvoke(asyncRes); //End invocation
                }
                catch (Exception e)
                {
                    Program.Log.WriteLine(string.Format("Caught exception {0}", e.Message));
                }
            }
            else
            {
                toolStripProgressBar1.Visible = val;
            }
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e) //Runs in a different thread
        {
            var worker = sender as BackgroundWorker;
            if (worker == null) return;
            while (!worker.CancellationPending)
            {
                foreach (var player in Comm.Players) //Loop through players
                {
                    if (!player.FlushTurns()) //Flush pending turns
                        player.Act(); //Act if no turns were flushed
                }

                Draw(); //Draw everything

                Thread.Sleep(SleepInterval); //sleep for an amount of time
                if (Comm.Tcs == TronCommunicatorStatus.Server) //If this is a server
                {
                    Comm.Send(Comm.GeneratePacket(MePlayer, TronInstruction.SyncToClient, MePlayer.XPos, MePlayer.YPos));
                    //Synchronize
                }
                Program.Log.WriteLine("Waiting for sync");
                if (!Comm.SyncComplete.WaitOne(2000, false)) //Wait for acknowledgement
                {
                    if (!worker.CancellationPending)
                    {
                        var peer = Comm.Tcs == TronCommunicatorStatus.Server ? "client" : "server";
                        MessageBox.Show(
                            "Waited for 2000 milliseconds without any response from " + peer + ". Disconnecting...",
                            "The " + peer + " has disconnected.");
                        Program.Log.WriteLine("Disconnected because peer disconnected");
                    }
                    break;
                }
                Program.Log.WriteLine("Sync complete");
            }
        }


        private void Draw() //Draws stuff to the buffers
        {
            lock (_gLock) //Lock to prevent concurrent access
            {
                _gMain.Clear(Color.Black); //Clear images
                _gPlayers.Clear(Color.Transparent);
#if DRAW_GRID //If the program should draw the grid. Useful for debugging
                
                for (float testx = 0; testx < _gr.CellWidth*_gr.Width; testx += _gr.CellWidth) //Double for loop to go through each cell
                {
                    
                    for (float testy = 0; testy < _gr.CellHeight*_gr.Height; testy += _gr.CellHeight)
                    {
                        _gMain.DrawRectangle(new Pen(Brushes.DimGray), testx, testy, testx + _gr.CellWidth,
                                        testy + _gr.CellHeight); //Draw rectangles in each cell
                    }
                }
#endif
                //Go through each wall to draw
// ReSharper disable ForCanBeConvertedToForeach
                for (var i = 0; i < Walls.Count; i++)
// ReSharper restore ForCanBeConvertedToForeach
                {
                    var tb = Walls[i];
                    if (tb == null || tb.Image == null) continue;
                    var x = tb.XPos*_gr.CellWidth; //Get x and y coordinate
                    var y = tb.YPos*_gr.CellHeight;


                    _gWall.DrawImage(tb.Image,
                                     new PointF((int) Math.Round(x), (int) Math.Round(y))); //Draw image
                }
                //Go through each player to draw
// ReSharper disable ForCanBeConvertedToForeach
                for (var i = 0; i < Comm.Players.Count; i++)
// ReSharper restore ForCanBeConvertedToForeach
                {
                    var tb = Comm.Players[i];
                    if (tb == null || tb.Image == null) continue;
                    var x = tb.XPos*_gr.CellWidth; //Get x and y coordinate
                    var y = tb.YPos*_gr.CellHeight;


                    _gPlayers.DrawImage(tb.Image, //Draw image
                                        new PointF((int) Math.Round(x), (int) Math.Round(y)));
                }
                _gMain.DrawImage(_bWall, 0, 0); //Draw buffers to main buffer
                _gMain.DrawImage(_bPlayers, 0, 0);
                var hasWon = true;
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable ForCanBeConvertedToForeach
                for (var x = 0; x < Comm.Players.Count; x++) //go through each player
// ReSharper restore ForCanBeConvertedToForeach
// ReSharper restore LoopCanBeConvertedToQuery
                {
                    if (Comm.Players[x].PlayerNum != MePlayer.PlayerNum) //if it isn't itself
                        hasWon &= Comm.Players[x].Dead;
                }
                _hasWonPerm = _hasWonPerm || (hasWon && !MePlayer.Dead);
                if (_hasWonPerm)
                {
                    _gMain.DrawString("You won!", new Font("Comic Sans MS", 20), Brushes.Orange, 70, 30);
                    //Everyone LOVES Comic Sans!
                }
                else if (MePlayer.Dead)
                    _gMain.DrawString("You died!", new Font("Comic Sans MS", 20), Brushes.Orange, 70, 30);

                Walls.Clear(); //Clear list
                RefreshGameWindow(); //Refresh
            }
        }

        private void RefreshGameWindow()
        {
            if (gameWindow.InvokeRequired) //If this is on a different thread
            {
                var asyncRes = BeginInvoke(new MethodInvoker(RefreshGameWindow));
                //Invoke the same method on the other thread
                try
                {
                    EndInvoke(asyncRes); //End invocation
                }
                catch (Exception e)
                {
                    Program.Log.WriteLine(string.Format("Caught exception {0}", e.Message));
                }
            }
            else
            {
                gameWindow.Refresh(); //Refresh if it is on the same thread
            }
        }

        private void UpdateTitle() //Updates the window title to show player number
        {
            if (InvokeRequired) //If this is on a different thread
            {
                var asyncRes = BeginInvoke(new MethodInvoker(UpdateTitle));
                //Invoke the same method on the other thread
                try
                {
                    EndInvoke(asyncRes); //End invocation
                }
                catch (Exception e)
                {
                    Program.Log.WriteLine(string.Format("Caught exception {0}", e.Message));
                }
            }
            else
            {
                Text = "Netron - Player number " + MePlayer.PlayerNum; //Refresh if it is on the same thread
            }
        }


        private void setUpServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_bw.IsBusy)
            {
                MessageBox.Show("A game is already running!");
            }
            MessageBox.Show(
                string.Format("Possible IP addresses:\nThis computer: {0,10}\nLAN: {1,10}\nExternal IP: {2,10}",
                              "127.0.0.1", Communicator.GetInternalIP(), Communicator.GetExternalIP()));

            Comm = new Communicator(_gr); //Create communicator
            SetupEventHandlers(); //Set up events
        }

        private void SetupEventHandlers()
        {
            Comm.OnInitComplete += Comm_OnInitComplete; //Set up events
            Comm.OnInitTimerTick += Comm_OnInitTimerTick;
            EditVisibilityOfProgressBar(true);
        }

        private void Comm_OnInitTimerTick(object sender, EventArgs e) //Called when the timer ticks
        {
            EditText(statusStrip1, toolStripStatusLabel1, string.Format("{0} seconds left",
                                                                        (Communicator.Timeout - Comm.ElapsedTime)/1000));
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
            if (String.IsNullOrWhiteSpace(scd.Hostname)) return; //return if it is an invalid string
            try
            {
                Comm = new Communicator(_gr, scd.Hostname); //Create a communicator using the ip address 
            }
            catch (SocketException se)
            {
                MessageBox.Show(
                    "SocketException occurred when connecting. Please make sure the hostname/IP address is correct");
                //Show error
                Program.Log.WriteLine(string.Format("Caught SocketException {0}", se.Message)); //log error
                return; //exit
            }
            SetupEventHandlers();
        }


        private void Comm_OnInitComplete(object sender, EventArgs e)
        {
            UpdateTitle();
            EditVisibilityOfProgressBar(false);
            _bw.RunWorkerAsync(); //Start the other thread
        }

        private void gameWindow_Click(object sender, EventArgs e)
        {
        }

        private void keyDown(object sender, KeyEventArgs e) //Fired when a key is pressed
        {
            switch (e.KeyCode)
            {
                case Keys.A:
                    MePlayer.AcceptUserInput(TronBase.DirectionType.West); //go left
                    break;
                case Keys.D:
                    MePlayer.AcceptUserInput(TronBase.DirectionType.East); //go right
                    break;
                case Keys.S:
                    MePlayer.AcceptUserInput(TronBase.DirectionType.South); //go down
                    break;
                case Keys.W:
                    MePlayer.AcceptUserInput(TronBase.DirectionType.North); //go up
                    break;
            }
        }

        private void keyUp(object sender, KeyEventArgs e)
        {
        }

        public void Reinitialize() //Reinitializes the game
        {
            try
            {
                Comm.Disconnect(); //Disconnect and stop thread
                _bw.CancelAsync();
                Initialize(); //Initialize variables
            }
            catch (Exception e)
            {
                Program.Log.WriteLine(string.Format("Caught exception {0}", e.Message));
            }
        }

        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Reinitialize();
        }

        private void MainWindow_SizeChanged(object sender, EventArgs e)
        {
            _gr.CellWidth = (float) gameWindow.Width/_gr.Width; //Set cell width and height
            _gr.CellHeight = (float) gameWindow.Height/_gr.Height;
            lock (_gLock)
            {
                _gMain.Clear(Color.Transparent);
                RefreshGameWindow();
            }
        }

        private void showHideLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Program.Log.Visible)
                Program.Log.Hide();
            else
                Program.Log.Show();
        }
    }
}
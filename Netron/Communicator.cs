#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ServerFramework.NET;
using Timer = System.Timers.Timer;

#endregion

namespace Netron
{
    //Declare event handler delegate
    public delegate void CommunicatorEventHandler(object sender, EventArgs e);

    /*
     * Regular packet
     * [instruction]0x01[xcoord]0x01[ycoord]0x01[objecttype]0x01[serialized object]
     * 
     * Initialization packet
     * [ChangePlayerNum]0x01[num]
     */
    //List of possible instructions to transmit/receive
    public enum TronInstruction
    {
        AddToGrid,
        MoveEntity,
        RemoveFromGrid,
        DoNothing,
        ChangePlayerNum,
        Connect,
        AddAndThenMoveEntity,
        InitComplete,
        TurnLeft,
        TurnRight,
        TurnUp,
        TurnDown,
        SyncToClient,
        SyncToServer,
        Kill,
        InstructionEnd = '\n'
    }

    //Status of this communicator instance
    public enum TronCommunicatorStatus
    {
        Server,
        Client
    }

    /*          [Client]    [Client]
     *               ^       ^
     *               |       |
     *                \      /  
     *  [Client] =>=> [Server] -> [Client]
     *                 |    \
     *                 V     \
     *              [Client]  \
     *                         \->[Client]
     *                          
     */

    public class Communicator
    {
        //Declare events
        public const double Timeout = 10000; //Constant for the timeout for the timer
        public const char Separator = ';'; //Constant to separate parts of a message
        public const int Port = 1337; //Port to use for communication
        private readonly Grid _gr; //Declare Grid that will be used later
        private readonly Server _server; //Declare Server to use to receive data
        private readonly TcpClient _serverConnection; // Used by Client Communicators instead of a TCP server
        private readonly NetworkStream _serverConnectionStream;
        private readonly string _serverIP; //Stores the IP address of the Server player
        private readonly Timer _timer; //Declare Timer that will wait for players to connect
        public AutoResetEvent SyncComplete; //Used to wait for completion of OnSyncComplete
        public TronCommunicatorStatus Tcs; //Status of this communicator
        private bool _hasFinalized; //Set if init is complete

        public Communicator(Grid gr, string serverIP = null) //Constructor with optional parameter
        {
            _serverIP = serverIP; // Store to instance variable
            _gr = gr;

            Players = new List<Player> {MainWindow.MePlayer}; //Create a new List with a collection initializer 
            Tcs = serverIP == null ? TronCommunicatorStatus.Server : TronCommunicatorStatus.Client;
            //If Serverip is null, then it is a Client. Otherwise it is a Server

            if (Tcs == TronCommunicatorStatus.Server) //If this is a Server
            {
                _server = new Server(Port, new List<char> {(char) TronInstruction.InstructionEnd, '\n', '\r'});
                //Create TCP server with default line terminators
                _server.OnClientConnect += server_OnClientConnect; //Set up events for the TCP server
                _server.OnClientDisconnect += server_OnClientDisconnect;
                _server.OnMessageReceived += server_OnMessageReceived;
                _server.StartAsync(); //Start server

                _timer = new Timer {Interval = 100}; //Create a Timer with an interval of 100 ms
                _timer.Elapsed += _timer_Elapsed; //Set up events
                _timer.Start(); //Start timer
                ElapsedTime = 0; //Set variable
            }
            else if (serverIP != null) //If this is not a Server
            {
                _serverConnection = new TcpClient(); //Create TcpClient to use to communicate with server
                _serverConnection.Connect(_serverIP, Port); //Connect to server
                _serverConnectionStream = _serverConnection.GetStream(); //Get network stream and store in variable
                _serverConnectionStream.BeginRead(new byte[0], 0, 0, ServerConnectionStreamOnRead,
                                                  //Begin asynchronous read with a callback
                                                  _serverConnectionStream);
            }
            OnSyncComplete += Communicator_OnSyncComplete; //Set up additional events
            SyncComplete = new AutoResetEvent(false);
            Program.Log.WriteLine("Running as " + Tcs); //MainWindow.Logging
        }

        public List<Player> Players //List of all players in the game
        { get; //Define accessor and setter through auto-property
            set; }

        public double ElapsedTime //Property to check elapsed time when waiting for players
        { get; set; }

        public event CommunicatorEventHandler OnNewPlayerConnect; //Fired when a player connects
        public event CommunicatorEventHandler OnPlayerDisconnect; //Fired when a player disconnects
        public event CommunicatorEventHandler OnInitComplete; //Fired when initialization is complete
        public event CommunicatorEventHandler OnInitTimerTick; //Fired as init timer is ticking

        private event CommunicatorEventHandler OnSyncComplete; //Fired when sync is complete

        protected virtual void FireOnSyncCompleteEvent() //Helper method to fire OnSyncComplete
        {
            if (OnSyncComplete != null) //Call if is not null
                OnSyncComplete(this, new EventArgs());
        }

        protected virtual void FireOnNewPlayerConnectEvent() //Helper method to fire OnNewPlayerConnect
        {
            if (OnNewPlayerConnect != null) //Call if is not null
                OnNewPlayerConnect(this, new EventArgs());
        }

        protected virtual void FireOnInitTimerTickEvent() //Helper method to fire OnInitTimerTic
        {
            if (OnInitTimerTick != null) //Call if is not null
                OnInitTimerTick(this, new EventArgs());
        }

        protected virtual void FireOnPlayerDisconnectEvent() //Helper method to fire OnPlayerDisconnect
        {
            if (OnPlayerDisconnect != null) //Call if is not null
                OnPlayerDisconnect(this, new EventArgs());
        }

        protected virtual void FireOnInitCompleteEvent() //Helper method to fire OnInitComplete
        {
            if (OnInitComplete != null) //Call if is not null
                OnInitComplete(this, new EventArgs());
        }

        private void Communicator_OnSyncComplete(object sender, EventArgs e) //Fired when sync is complete
        {
            SyncComplete.Set(); //Set the AutoResetEvent
        }

        private void ServerConnectionStreamOnRead(IAsyncResult iar) //Async callback for stream read
        {
            var stream = iar.AsyncState as NetworkStream; //Safely typecast stream from asyncstate
            if (stream == null) return; //Return if called improperly
            try //Catch exceptions
            {
                stream.EndRead(iar); //End async read
                var list = new List<byte>(); //Create list to store data
                while (stream.DataAvailable) //While there is data to be read
                {
                    var b = stream.ReadByte(); //Get the next byte
                    if (b != (byte) TronInstruction.InstructionEnd)
                        list.Add((byte) b); //Add byte to list
                    else
                        break;
                }
                Program.Log.WriteLine(string.Join(",", list));
                Parse(list.ToArray()); //Parse instruction
                _serverConnectionStream.BeginRead(new byte[0], 0, 0, ServerConnectionStreamOnRead,
                                                  _serverConnectionStream); //Restart async read
            }
            catch (IOException e) //Catch exceptions
            {
                Program.Log.WriteLine(string.Format("Caught I/O exception: {0}", e.Message));
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void _timer_Elapsed(object sender, EventArgs e) //Fired as server is waiting for clients
        {
            var t = sender as Timer; //Safely typecast and exit cleanly if there are errors
            if (t == null) return;
            FireOnInitTimerTickEvent(); //Fire event
            ElapsedTime += t.Interval; //Increment variable
            if (ElapsedTime >= Timeout) //If timeout has been reached
            {
                t.Stop(); //Stop timer
                FinalizeConnections(); //perform last initialization tasks
            }
        }

        private void FinalizeConnections()
        {
            _hasFinalized = true; //Set variable
            if (Players.Count == 0) return; //If there are no players to process, exit
            Program.Log.WriteLine("Finalizing connections");
            var gap = _gr.Width/Players.Count; //Gap between players
            var curx = 0; //current x coordinate
// ReSharper disable ForCanBeConvertedToForeach
            for (var x = 0; x < Players.Count; x++)
// ReSharper restore ForCanBeConvertedToForeach
            {
                var p = Players[x]; //Get player
                var ins = GeneratePacket(p, TronInstruction.MoveEntity, curx, _gr.Height/2);
                //Generate message to move player to a specific location
                Parse(ins); //Interpret instruction
                Send(ins); //Broadcast instruction

                p.XPos = curx; //Store variables
                p.YPos = _gr.Height/2;
                curx += gap; //Move to next coordinate
            }
            Send(GeneratePacket(MainWindow.MePlayer, TronInstruction.InitComplete, MainWindow.MePlayer.XPos,
                                MainWindow.MePlayer.YPos)); //Send message that game is about to begin
            FireOnInitCompleteEvent(); //Fire event
        }

        public void Disconnect()
        {
            if (Tcs == TronCommunicatorStatus.Server)
            {
                Players.Clear();
                _server.Stop();
                if (_timer.Enabled)
                    _timer.Stop();
            }
            else
            {
                Players.Clear();
                _serverConnectionStream.Close();
                _serverConnection.Close();
            }
            Program.Log.WriteLine
                ("Disconnected!");
        }

        private void server_OnClientDisconnect(object sender, ClientEventArgs e) //Called when player disconnects
        {
            for (var x = 0; x < Players.Count; x++) //Search list for player
            {
                if (Players[x].PlayerNum == (int) e.Client.Tag) //if player has been found
                {
                    Players[x].Dead = true; //Kill the player
                    Program.Log.WriteLine("Player " + x + " removed");
                    return;
                }
            }
            FireOnPlayerDisconnectEvent(); //Fire event
        }

        public static string GetInternalIP()
        {
            string ip = null;
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var i in host.AddressList.Where(i => i.AddressFamily == AddressFamily.InterNetwork))
            {
                ip = i.ToString();
            }
            return ip;
        }

        public static string GetExternalIP()
        {
            return
                Encoding.ASCII.GetString((new WebClient()).DownloadData("http://checkip.dyndns.org/")).Replace(
                    @"<html><head><title>Current IP Check</title></head><body>Current IP Address: ", "").Replace(
                        @"</body></html>", "");
        }

        private void server_OnClientConnect(object sender, ClientEventArgs e) //Fired when a player connects
        {
            if (_hasFinalized) return; //Return if game is already running
            if (Tcs == TronCommunicatorStatus.Server) //If this is a Server
            {
                var color = (new Random()).Next(255*255*255); //Create random color
                var player = new Player(Players.Count) {Color = Color.FromArgb(color)};
                Players.Insert(player.PlayerNum, player); //Add player to list
                e.Client.Tag = player.PlayerNum; //Set tag for client       
                e.Client.SendData("" + (int) TronInstruction.ChangePlayerNum + Separator + player.PlayerNum +
                                  (char) TronInstruction.InstructionEnd);
                //Send message to change the player number to the next available one
                e.Client.SendData(GeneratePacket(player, TronInstruction.DoNothing, player.XPos, player.YPos));
                //Send an acknowledgement message
                Program.Log.WriteLine("Player joined!");
            }
// ReSharper disable ForCanBeConvertedToForeach
            for (var x = 0; x < Players.Count; x++) //Go through all players
// ReSharper restore ForCanBeConvertedToForeach
            {
                var p = Players[x];
                var ins = GeneratePacket(p, TronInstruction.AddToGrid, p.XPos, p.YPos);
                //Send all players to the newly connected player
                e.Client.SendData(ins);
            }
            Program.Log.WriteLine("Connection!");
            FireOnNewPlayerConnectEvent(); //Fire event
        }

        private void server_OnMessageReceived(object sender, ClientEventArgs e) //Fired when data is received
        {
            Parse(e.Client.Message); //parse instruction
        }

        private static byte[] GetBytes(string str) //Converts a string to bytes
        {
            return str == null ? null : Encoding.ASCII.GetBytes(str);
        }

        private void Parse(string str) //Overloaded method
        {
            Parse(GetBytes(str)); //Call Parse with bytes for string
        }

        private void Parse(byte[] instr) //Parses instructions
        {
            if (instr.Length < 2) return; //Return if instruciton is too short
            SyncComplete.Reset(); //Reset AutoResetEvent
            var str = Encoding.ASCII.GetString(instr); //Get string from bytes
            Program.Log.WriteLine(string.Format("Received {0}", str));
            var strs = str.Split(Separator); //Separate instruction with separator
            var whattodo = (TronInstruction) Int32.Parse(strs[0]); //Get the TronInstruction
            if (whattodo == TronInstruction.InitComplete) //if initialization is complete
            {
                FireOnInitCompleteEvent(); //fire event
            }
            else if (whattodo == TronInstruction.ChangePlayerNum) //request to change player number
            {
                MainWindow.MePlayer.PlayerNum = Int32.Parse(strs[1]);
                //Get the player number from instruction and store it
                Program.Log.WriteLine("Changing player number to " + MainWindow.MePlayer.PlayerNum);
            }
            else if (whattodo == TronInstruction.SyncToClient)
            {
                Send(GeneratePacket(MainWindow.MePlayer, TronInstruction.SyncToServer, MainWindow.MePlayer.XPos,
                                    MainWindow.MePlayer.YPos)); //Send acknowledgement to server

                FireOnSyncCompleteEvent(); //fire event
            }
            else if (whattodo == TronInstruction.SyncToServer) //When a Client player acknowledges a sync request
            {
                FireOnSyncCompleteEvent(); //Fire event
            }
            else
            {
                var xcoord = Int32.Parse(strs[1]); //Get xcoord and ycoord from arg1 and arg2
                var ycoord = Int32.Parse(strs[2]);
                var type = (TronType) Int32.Parse(strs[3]); //Get TronType (Player or wall)
                switch (type)
                {
                    case TronType.Player:
                        {
                            var player = Player.Deserialize(strs[4]); //Deserialize from data

                            if (player.PlayerNum == MainWindow.MePlayer.PlayerNum)
                            {
                                MainWindow.MePlayer.Color = player.Color; //Set color
                                _gr.Exec(whattodo, xcoord, ycoord, MainWindow.MePlayer);
                                //perform instruction on this player
                            }
                            else
                            {
                                var found = false; //Search through player list
                                for (var x = 0; x < Players.Count; x++)
                                {
                                    if (Players[x].PlayerNum == player.PlayerNum)
                                    {
                                        Players[x] = player; //store new player to list
                                        found = true; //set variable
                                        break;
                                    }
                                }
                                if (!found)
                                {
                                    if (player.PlayerNum >= Players.Count)
                                        Players.Add(player); //Add player if it wasn't found in the list
                                    else
                                        Players.Insert(player.PlayerNum, player);
                                    Program.Log.WriteLine(string.Format("Adding new player: {0}", player.PlayerNum));
                                    if (player.PlayerNum == 0) Program.Log.WriteLine("This is the Server player");
                                }
                                _gr.Exec(whattodo, xcoord, ycoord, player); //Execute instruction
                                if (Tcs == TronCommunicatorStatus.Server)
                                    Send(instr, player.PlayerNum);
                                //Send to all Clients, ignoring the player that sent it
                            }
                        }
                        break;
                    case TronType.Wall:
                        {
                            var wall = Wall.Deserialize(strs[4]); //Deserialize wall
                            _gr.Exec(whattodo, xcoord, ycoord, wall); //Execute instruction
                        }
                        break;
                }
            }
        }

        public void Send(string tosend, int ignore = -1) //Overloaded function
        {
            Send(GetBytes(tosend), ignore); //Calls Send with bytes from string
        }

        public void Send(byte[] buf, int ignore = -1) //Sends data, optional parameter to ignore a player number
        {
            switch (Tcs)
            {
                case TronCommunicatorStatus.Client: //If this is a Client
                    {
                        //Send data to server
                        _serverConnectionStream.Write(buf, 0, buf.Length);
                    }
                    break;
                case TronCommunicatorStatus.Server: //if this is a Server
                    foreach (var c in _server.ConnectedClients) //go through all players
                    {
                        if ((int) c.Tag != ignore) //ignore certain players
                            c.SendData(buf); //send data
                        /*if (buf[buf.Length - 1] != (byte)'\n')
                            c.SendData(new[] {(byte) '\n'});*/
                    }

                    break;
            }
        }

        //Generates a packet from a base, instruction, and arguments
        public string GeneratePacket(TronBase te, TronInstruction instr, int arg1, int arg2)
        {
            var sb = new StringBuilder(); //Create StringBuilder
            sb.Append((byte) instr); //Append info
            sb.Append(Separator);
            sb.Append(arg1);
            sb.Append(Separator);
            sb.Append(arg2);
            sb.Append(Separator);
            sb.Append((int) te.GetTronType());
            sb.Append(Separator);
            sb.Append(te.Serialize());
            sb.Append((char) TronInstruction.InstructionEnd); //End of instruction
            return sb.ToString(); //return as a string
        }
    }
}
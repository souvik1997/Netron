using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Net.Sockets;
using ServerFramework.NET;
using System.IO;
using System.Threading;
using Timer = System.Timers.Timer;



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
        AddToGrid, MoveEntity, RemoveFromGrid, DoNothing, ChangePlayerNum, Connect, AddAndThenMoveEntity, InitComplete, TurnLeft, TurnRight, TurnUp, TurnDown, SyncToClient, SyncToServer, InstructionEnd = '\n'
    }
    //Status of this communicator instance
    public enum TronCommunicatorStatus
    {
        Master, Slave
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
        public event CommunicatorEventHandler OnNewPlayerConnect; //Fired when a player connects
        public event CommunicatorEventHandler OnPlayerDisconnect; //Fired when a player disconnects
        public event CommunicatorEventHandler OnInitComplete; //Fired when initialization is complete
        public event CommunicatorEventHandler OnInitTimerTick;//Fired as init timer is ticking
        
        private event CommunicatorEventHandler OnSyncComplete; //Fired when sync is complete

        public AutoResetEvent SyncComplete; //Used to wait for completion of OnSyncComplete
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
        public TronCommunicatorStatus Tcs; //Status of this communicator
        private readonly string _masterIP; //Stores the IP address of the master player
        public List<Player> Players //List of all players in the game
        {
            get; //Define accessor and setter through auto-property
            set;
        }
        private readonly Server _server; //Declare Server to use to receive data
        private readonly Grid _gr; //Declare Grid that will be used later
        private readonly Timer _timer; //Declare Timer that will wait for players to connect
        public double ElapsedTime //Property to check elapsed time when waiting for players
        {
            get;
            set;
        }
        public const double Timeout = 10000; //Constant for the timeout for the timer
        public const char Separator = ';'; //Constant to separate parts of a message
        public const int Port = 1337; //Port to use for communication

        private readonly TcpClient _serverConnection; // Used by slave Communicators instead of a TCP server
        private readonly NetworkStream _serverConnectionStream;

        private bool _hasFinalized; //Set if init is complete
        public Communicator(Grid gr, string masterIP = null) //Constructor with optional parameter
        {
            
            _masterIP = masterIP; // Store to instance variable
            _gr = gr;
            
            Players = new List<Player> {MainWindow.MePlayer}; //Create a new List with a collection initializer 
            Tcs = masterIP == null ? TronCommunicatorStatus.Master : TronCommunicatorStatus.Slave; //If masterip is null, then it is a slave. Otherwise it is a master

            if (Tcs == TronCommunicatorStatus.Master) //If this is a master
            {
                _server = new Server(Port, new List<char> { (char)TronInstruction.InstructionEnd, '\n', '\r' });  //Create TCP server with default line terminators
                _server.OnClientConnect += server_OnClientConnect; //Set up events for the TCP server
                _server.OnClientDisconnect += server_OnClientDisconnect;
                _server.OnMessageReceived += server_OnMessageReceived;
                _server.StartAsync(); //Start server

                _timer = new Timer {Interval = 100}; //Create a Timer with an interval of 100 ms
                _timer.Elapsed += _timer_Elapsed; //Set up events
                _timer.Start(); //Start timer
                ElapsedTime = 0; //Set variable
            }
            else if (masterIP != null) //If this is not a master
            {                
                _serverConnection = new TcpClient(); //Create TcpClient to use to communicate with server
                _serverConnection.Connect(_masterIP, Port); //Connect to server
                _serverConnectionStream = _serverConnection.GetStream(); //Get network stream and store in variable
                _serverConnectionStream.BeginRead(new byte[0], 0, 0, ServerConnectionStreamOnRead, //Begin asynchronous read with a callback
                                                  _serverConnectionStream);
               
            }
            OnSyncComplete += Communicator_OnSyncComplete; //Set up additional events
            SyncComplete = new AutoResetEvent(false); 
            Debug.WriteLine("Running as " + Tcs); //Logging
        }

        void Communicator_OnSyncComplete(object sender, EventArgs e) //Fired when sync is complete
        {
            SyncComplete.Set(); //Set the AutoResetEvent
        }
        private void ServerConnectionStreamOnRead(IAsyncResult iar) //Async callback for stream read
        {
            var stream = iar.AsyncState as NetworkStream; //Safely typecast stream from asyncstate
            if (stream == null) return; //Return if called improperly
            try  //Catch exceptions
            {
                stream.EndRead(iar); //End async read
                List<byte> list = new List<byte>(); //Create list to store data
                while (stream.DataAvailable) //While there is data to be read
                {
                    int b = stream.ReadByte(); //Get the next byte
                    Debug.Write(b+",");
                    if (b != (byte)TronInstruction.InstructionEnd)
                        list.Add((byte)b); //Add byte to list
                    else
                        break;
                }
                Debug.WriteLine("\n"); 
                Parse(list.ToArray()); //Parse instruction
                _serverConnectionStream.BeginRead(new byte[0], 0, 0, ServerConnectionStreamOnRead,
                                                  _serverConnectionStream); //Restart async read
            }
            catch (IOException e) //Catch exceptions
            {
                Debug.Print("Caught I/O exception: {0}", e.Message);
            }
            catch (ObjectDisposedException e)
            {
                
            }
            
        }
        void _timer_Elapsed(object sender, EventArgs e) //Fired as server is waiting for clients
        {
            Timer t = sender as Timer; //Safely typecast and exit cleanly if there are errors
            if (t == null) return;
            FireOnInitTimerTickEvent(); //Fire event
            ElapsedTime += t.Interval; //Increment variable
            if (ElapsedTime >= Timeout) //If timeout has been reached
            {
                t.Stop(); //Stop timer
                FinalizeConnections(); //perform last initialization tasks
            }
        }
        void FinalizeConnections()
        {
            _hasFinalized = true; //Set variable
            if (Players.Count == 0) return; //If there are no players to process, exit
            Debug.WriteLine("Finalizing connections");
            int gap = _gr.Width/Players.Count; //Gap between players
            int curx = 0; //current x coordinate
// ReSharper disable ForCanBeConvertedToForeach
            for (int x = 0; x < Players.Count; x++ )
// ReSharper restore ForCanBeConvertedToForeach
            {
                Player p = Players[x]; //Get player
                string ins = GeneratePacket(p, TronInstruction.MoveEntity, curx, _gr.Height / 2); //Generate message to move player to a specific location
                Parse(ins); //Interpret instruction
                Send(ins); //Broadcast instruction

                p.XPos = curx; //Store variables
                p.YPos = _gr.Height / 2;
                curx += gap; //Move to next coordinate
            }
            Send(GeneratePacket(MainWindow.MePlayer,TronInstruction.InitComplete,MainWindow.MePlayer.XPos,MainWindow.MePlayer.YPos)); //Send message that game is about to begin
            FireOnInitCompleteEvent(); //Fire event
        }
        public void Disconnect()
        {
            if (Tcs == TronCommunicatorStatus.Master)
            {
                Players.Clear();
                _server.Stop();
            }
            else
            {
                Players.Clear();
                _serverConnectionStream.Close();
                _serverConnection.Close();
            }

        }
        void server_OnClientDisconnect(object sender, ClientEventArgs e) //Called when player disconnects
        {
            for(int x= 0 ; x < Players.Count; x++) //Search list for player
            {
                if (Players[x].PlayerNum == (int)e.Client.Tag) //if player has been found
                {
                    Players[x].Dead = true; //Kill the player
                    Debug.WriteLine("Player " + x + " removed");
                    return;
                }
            }
            FireOnPlayerDisconnectEvent(); //Fire event
        }

        void server_OnClientConnect(object sender, ClientEventArgs e) //Fired when a player connects
        {
            if (_hasFinalized) return; //Return if game is already running
            if (Tcs == TronCommunicatorStatus.Master) //If this is a master
            {
                int color = (new Random()).Next(255*255*255); //Create random color
                var player = new Player(Players.Count) {Color = Color.FromArgb(color)};
                Players.Add(player); //Add player to list
                e.Client.Tag = player.PlayerNum;          //Set tag for client       
                e.Client.SendData("" + (int) TronInstruction.ChangePlayerNum + Separator + player.PlayerNum + (char)TronInstruction.InstructionEnd); //Send message to change the player number to the next available one
                e.Client.SendData(GeneratePacket(player, TronInstruction.DoNothing, player.XPos, player.YPos)); //Send an acknowledgement message
                Debug.WriteLine("Player joined!");
            }
// ReSharper disable ForCanBeConvertedToForeach
            for (int x = 0; x < Players.Count; x++) //Go through all players
// ReSharper restore ForCanBeConvertedToForeach
            {
                Player p = Players[x]; 
                string ins = GeneratePacket(p, TronInstruction.AddToGrid, p.XPos, p.YPos); //Send all players to the newly connected player
                e.Client.SendData(ins);
            }
            Debug.WriteLine("Connection!");
            FireOnNewPlayerConnectEvent(); //Fire event
        }

        void server_OnMessageReceived(object sender, ClientEventArgs e) //Fired when data is received
        {
            Parse(e.Client.Message); //parse instruction
        }
        private static byte[] GetBytes(string str) //Converts a string to bytes
        {
            return str == null ? null : Encoding.ASCII.GetBytes(str);
        }
        void Parse(string str) //Overloaded method
        {
            Parse(GetBytes(str)); //Call Parse with bytes for string
        }
        void Parse(byte[] instr) //Parses instructions
        {
            if (instr.Length < 2) return; //Return if instruciton is too short
            SyncComplete.Reset(); //Reset AutoResetEvent
            string str = Encoding.ASCII.GetString(instr); //Get string from bytes
            Debug.Print("Received {0}", str);
            string[] strs = str.Split(Separator); //Separate instruction with separator
            var whattodo = (TronInstruction)Int32.Parse(strs[0]); //Get the TronInstruction
            if (whattodo == TronInstruction.InitComplete) //if initialization is complete
            {
                FireOnInitCompleteEvent(); //fire event
            }
            else if (whattodo == TronInstruction.ChangePlayerNum) //request to change player number
            {
                MainWindow.MePlayer.PlayerNum = Int32.Parse(strs[1]); //Get the player number from instruction and store it
                Debug.WriteLine("Changing player number to " + MainWindow.MePlayer.PlayerNum); 
            }
            else if (whattodo == TronInstruction.SyncToClient) 
            {
                Send(GeneratePacket(MainWindow.MePlayer, TronInstruction.SyncToServer, MainWindow.MePlayer.XPos, MainWindow.MePlayer.YPos)); //Send acknowledgement to server

                FireOnSyncCompleteEvent(); //fire event
            }
            else if (whattodo == TronInstruction.SyncToServer)  //When a slave player acknowledges a sync request
            {
                FireOnSyncCompleteEvent(); //Fire event
            }
            else
            {


                var xcoord = Int32.Parse(strs[1]); //Get xcoord and ycoord from arg1 and arg2
                var ycoord = Int32.Parse(strs[2]); 
                var type = (TronType)Int32.Parse(strs[3]); //Get TronType (Player or wall)
                switch (type)
                {
                    case TronType.Player:
                        {
                            var player = Player.Deserialize(strs[4]); //Deserialize from data

                            if (player.PlayerNum == MainWindow.MePlayer.PlayerNum)
                            {
                                MainWindow.MePlayer.Color = player.Color; //Set color
                                _gr.Exec(whattodo, xcoord, ycoord, MainWindow.MePlayer); //perform instruction on this player
                            }
                            else
                            {
                                bool found = false; //Search through player list
                                for (int x = 0; x < Players.Count; x++)
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
                                    Players.Add(player); //Add player if it wasn't found in the list
                                    Debug.Print("Adding new player: {0}", player.PlayerNum);
                                    if (player.PlayerNum == 0) Debug.WriteLine("This is the MASTER player");
                                }
                                _gr.Exec(whattodo, xcoord, ycoord, player); //Execute instruction
                                if (Tcs == TronCommunicatorStatus.Master)
                                    Send(instr, player.PlayerNum); //Send to all slaves, ignoring the player that sent it
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
                case TronCommunicatorStatus.Slave: //If this is a slave
                    {

                        //Send data to server
                        _serverConnectionStream.Write(buf, 0, buf.Length);
                    }
                    break;
                case TronCommunicatorStatus.Master: //if this is a master
                    foreach(Client c in _server.ConnectedClients) //go through all players
                    {
                        if ((int)c.Tag != ignore) //ignore certain players
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
            StringBuilder sb = new StringBuilder(); //Create StringBuilder
            sb.Append((byte)instr); //Append info
            sb.Append(Separator);
            sb.Append(arg1);
            sb.Append(Separator);
            sb.Append(arg2);
            sb.Append(Separator);
            sb.Append((int) te.GetTronType());
            sb.Append(Separator);
            sb.Append(te.Serialize());
            sb.Append((char)TronInstruction.InstructionEnd); //End of instruction
            return sb.ToString(); //return as a string

        }
        
    }
}

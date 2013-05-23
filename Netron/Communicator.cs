using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Net.Sockets;
using ServerFramework.NET;
using System.IO;
using System.Threading;
using Timer = System.Timers.Timer;

namespace Netron
{

    public delegate void CommunicatorEventHandler(object sender, EventArgs e);
    /*
     * Regular packet
     * [instruction]0x01[xcoord]0x01[ycoord]0x01[objecttype]0x01[serialized object]
     * 
     * Initialization packet
     * [ChangePlayerNum]0x01[num]
     */ 
    public enum TronInstruction
    {
        AddToGrid, MoveEntity, RemoveFromGrid, DoNothing, ChangePlayerNum, Connect, AddAndThenMoveEntity, InitComplete, TurnLeft, TurnRight, TurnUp, TurnDown, SyncToClient, SyncToServer, InstructionEnd = '\n'
    }
    public enum TronCommunicatorStatus
    {
        Master, Slave
    }

    public class Communicator
    {
        public event CommunicatorEventHandler OnNewPlayerConnect;
        public event CommunicatorEventHandler OnPlayerDisconnect;
        public event CommunicatorEventHandler OnInitComplete;
        public event CommunicatorEventHandler OnInitTimerTick;
        private event CommunicatorEventHandler OnSyncComplete;

        public AutoResetEvent SyncComplete;
        protected virtual void FireOnSyncCompleteEvent()
        {
            if (OnSyncComplete != null)
                OnSyncComplete(this, new EventArgs());
        }
        protected virtual void FireOnNewPlayerConnectEvent()
        {
            if (OnNewPlayerConnect != null)
                OnNewPlayerConnect(this, new EventArgs());
        }
        protected virtual void FireOnInitTimerTickEvent()
        {
            if (OnInitTimerTick != null)
                OnInitTimerTick(this, new EventArgs());
        }
        protected virtual void FireOnPlayerDisconnectEvent()
        {
            if (OnPlayerDisconnect != null)
                OnPlayerDisconnect(this, new EventArgs());
        }
        protected virtual void FireOnInitCompleteEvent()
        {
            if (OnInitComplete != null)
                OnInitComplete(this, new EventArgs());
        }
        public TronCommunicatorStatus Tcs;
        private readonly string _masterIP;
        public List<Player> Players
        {
            get;
            set;
        }
        private readonly Server _server;
        private readonly Grid _gr;
        private readonly Timer _timer;
        public double ElapsedTime
        {
            get;
            set;
        }
        public const double Timeout = 10000;
        public const char Separator = ';';

        private readonly TcpClient _serverConnection;
        private readonly NetworkStream _serverConnectionStream;

        private bool _hasFinalized;
        public Communicator(Grid gr, string masterIP = null)
        {
            
            _masterIP = masterIP;
            _gr = gr;
            
            Players = new List<Player> {MainWindow.MePlayer};
            Tcs = masterIP == null ? TronCommunicatorStatus.Master : TronCommunicatorStatus.Slave;

            if (Tcs == TronCommunicatorStatus.Master)
            {
                _server = new Server(1337, new List<char> { (char)TronInstruction.InstructionEnd, '\n', '\r' });
                _server.OnClientConnect += server_OnClientConnect;
                _server.OnClientDisconnect += server_OnClientDisconnect;
                _server.OnMessageReceived += server_OnMessageReceived;
                _server.StartAsync();

                _timer = new Timer {Interval = 100};
                _timer.Elapsed += _timer_Elapsed;
                _timer.Start();
                ElapsedTime = 0;
            }
            else if (masterIP != null)
            {                
                _serverConnection = new TcpClient();
                _serverConnection.Connect(_masterIP, 1337);
                _serverConnectionStream = _serverConnection.GetStream();
                _serverConnectionStream.BeginRead(new byte[0], 0, 0, ServerConnectionStreamOnRead,
                                                  _serverConnectionStream);
               
            }
            OnSyncComplete += new CommunicatorEventHandler(Communicator_OnSyncComplete);
            SyncComplete = new AutoResetEvent(false);
            Console.WriteLine("Running as " + Tcs);
        }

        void Communicator_OnSyncComplete(object sender, EventArgs e)
        {
            SyncComplete.Set();
        }
        private void ServerConnectionStreamOnRead(IAsyncResult iar)
        {
            var stream = iar.AsyncState as NetworkStream;
            if (stream == null) return;
            try
            {
                stream.EndRead(iar);
                List<byte> list = new List<byte>();
                while (stream.DataAvailable)
                {
                    int b = stream.ReadByte();
                    Console.Write(b+",");
                    if (b != (byte)TronInstruction.InstructionEnd)
                        list.Add((byte)b);
                    else
                        break;
                }
                Console.WriteLine("\n");
                Parse(list.ToArray());
                _serverConnectionStream.BeginRead(new byte[0], 0, 0, ServerConnectionStreamOnRead,
                                                  _serverConnectionStream);
            }
            catch (IOException e)
            {
                Console.WriteLine("Caught exception: {0}", e.Message);
            }
            
        }
        void _timer_Elapsed(object sender, EventArgs e)
        {
            Timer t = sender as Timer;
            if (t == null) return;
            FireOnInitTimerTickEvent();
            ElapsedTime += t.Interval;
            if (ElapsedTime >= Timeout)
            {
                t.Stop();
                FinalizeConnections();
            }
        }
        void FinalizeConnections()
        {
            _hasFinalized = true;
            if (Players.Count == 0) return;
            Console.WriteLine("Finalizing connections");
            int gap = _gr.Width/Players.Count;
            int curx = 0;
// ReSharper disable ForCanBeConvertedToForeach
            for (int x = 0; x < Players.Count; x++ )
// ReSharper restore ForCanBeConvertedToForeach
            {
                Player p = Players[x];
                string ins = GeneratePacket(p, TronInstruction.MoveEntity, curx, _gr.Height / 2);
                Parse(ins);
                Send(ins);

                p.XPos = curx;
                p.YPos = _gr.Height / 2;
                curx += gap;
            }
            Send(GeneratePacket(MainWindow.MePlayer,TronInstruction.InitComplete,MainWindow.MePlayer.XPos,MainWindow.MePlayer.YPos));
            FireOnInitCompleteEvent();
        }
        void server_OnClientDisconnect(object sender, ClientEventArgs e)
        {
            for(int x= 0 ; x < Players.Count; x++)
            {
                if (Players[x].PlayerNum == (int)e.Client.Tag)
                {
                    Players.RemoveAt(x);
                    Console.WriteLine("Player " + x + " removed");
                    /* TODO: Add code to "suspend" player */
                    return;
                }
            }
            FireOnPlayerDisconnectEvent();
        }

        void server_OnClientConnect(object sender, ClientEventArgs e)
        {
            if (_hasFinalized) return;
            if (Tcs == TronCommunicatorStatus.Master)
            {
                int color = (new Random()).Next(255*255*255);
                var player = new Player(Players.Count) {Color = Color.FromArgb(color)};
                Players.Add(player);
                e.Client.Tag = player.PlayerNum;                
                e.Client.SendData("" + (int) TronInstruction.ChangePlayerNum + Separator + player.PlayerNum + (char)TronInstruction.InstructionEnd);
                e.Client.SendData(GeneratePacket(player, TronInstruction.DoNothing, player.XPos, player.YPos));
                Console.WriteLine("Player joined!");
            }
// ReSharper disable ForCanBeConvertedToForeach
            for (int x = 0; x < Players.Count; x++)
// ReSharper restore ForCanBeConvertedToForeach
            {
                Player p = Players[x];
                string ins = GeneratePacket(p, TronInstruction.AddToGrid, p.XPos, p.YPos);
                e.Client.SendData(ins);
            }
            Console.WriteLine("Connection!");
            FireOnNewPlayerConnectEvent();
        }

        void server_OnMessageReceived(object sender, ClientEventArgs e)
        {
            Parse(e.Client.Message);
        }
        private static byte[] GetBytes(string str)
        {
            return str == null ? null : Encoding.ASCII.GetBytes(str);
        }
        void Parse(string str)
        {
            Parse(GetBytes(str));
        }
        void Parse(byte[] instr)
        {
            if (instr.Length < 2) return;
            SyncComplete.Reset();
            string str = Encoding.ASCII.GetString(instr);
            Console.WriteLine("Received {0}", str);
            string[] strs = str.Split(Separator);
            var whattodo = (TronInstruction)Int32.Parse(strs[0]);
            if (whattodo == TronInstruction.InitComplete)
            {
                FireOnInitCompleteEvent();
            }
            else if (whattodo == TronInstruction.ChangePlayerNum)
            {
                MainWindow.MePlayer.PlayerNum = Int32.Parse(strs[1]);
                Console.WriteLine("Changing player number to " + MainWindow.MePlayer.PlayerNum);
            }
            else if (whattodo == TronInstruction.SyncToClient)
            {
                Send(GeneratePacket(MainWindow.MePlayer, TronInstruction.SyncToServer, MainWindow.MePlayer.XPos, MainWindow.MePlayer.YPos));

                FireOnSyncCompleteEvent();
            }
            else if (whattodo == TronInstruction.SyncToServer)
            {
                FireOnSyncCompleteEvent();
            }
            else
            {


                var xcoord = Int32.Parse(strs[1]);
                var ycoord = Int32.Parse(strs[2]);
                var type = (TronType)Int32.Parse(strs[3]);
                switch (type)
                {
                    case TronType.Player:
                        {
                            var player = Player.Deserialize(strs[4]);

                            if (player.PlayerNum == MainWindow.MePlayer.PlayerNum)
                            {
                                MainWindow.MePlayer.Color = player.Color;
                                _gr.Exec(whattodo, xcoord, ycoord, MainWindow.MePlayer);
                            }
                            else
                            {
                                bool found = false;
                                for (int x = 0; x < Players.Count; x++)
                                {
                                    if (Players[x].PlayerNum == player.PlayerNum)
                                    {
                                        Players[x] = player;
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found)
                                {
                                    Players.Add(player);
                                    Console.WriteLine("Adding new player: {0}", player.PlayerNum);
                                    if (player.PlayerNum == 0) Console.WriteLine("This is the MASTER player");
                                }
                                _gr.Exec(whattodo, xcoord, ycoord, player);
                                if (Tcs == TronCommunicatorStatus.Master)
                                    Send(instr, player.PlayerNum);
                            }
                        }
                        break;
                    case TronType.Wall:
                        {
                            var wall = Wall.Deserialize(strs[4]);
                            _gr.Exec(whattodo, xcoord, ycoord, wall);
                        }
                        break;
                }
            }

        }
        public void Send(string tosend, int ignore = -1)
        {
            Send(GetBytes(tosend), ignore);
        }
        public void Send(byte[] buf, int ignore = -1)
        {
            switch (Tcs)
            {
                case TronCommunicatorStatus.Slave:
                    {

                        NetworkStream stream = _serverConnection.GetStream();
                        stream.Write(buf, 0, buf.Length);
                    }
                    break;
                case TronCommunicatorStatus.Master:
                    foreach(Client c in _server.ConnectedClients)
                    {
                        if ((int)c.Tag != ignore)
                            c.SendData(buf);
                        /*if (buf[buf.Length - 1] != (byte)'\n')
                            c.SendData(new[] {(byte) '\n'});*/
                    }
                    
                    break;
            }
        }

        public string GeneratePacket(TronBase te, TronInstruction instr, int arg1, int arg2)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append((byte)instr);
            sb.Append(Separator);
            sb.Append(arg1);
            sb.Append(Separator);
            sb.Append(arg2);
            sb.Append(Separator);
            sb.Append((int) te.GetTronType());
            sb.Append(Separator);
            sb.Append(te.Serialize());
            sb.Append((char)TronInstruction.InstructionEnd);
            return sb.ToString();

        }
        
    }
}

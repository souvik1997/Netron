using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using ServerFramework.NET;
using Timer = System.Windows.Forms.Timer;

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
        AddToGrid = 0x01, MoveEntity = 0x02, RemoveFromGrid = 0x03, DoNothing = 0x04, ChangePlayerNum=0x05, Connect=0x06, AddAndThenMoveEntity=0x07, InstructionEnd = 0xFF
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
        protected virtual void FireOnNewPlayerConnectEvent()
        {
            if (OnNewPlayerConnect != null)
                OnNewPlayerConnect(this, new EventArgs());
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
        public const char Separator = ';';

        private readonly TcpClient _serverConnection;
        private readonly NetworkStream _serverConnectionStream;
        public Communicator(Grid gr, string masterIP = null)
        {
            
            _masterIP = masterIP;
            _gr = gr;
            
            Players = new List<Player> {MainWindow.MePlayer};
            Tcs = masterIP == null ? TronCommunicatorStatus.Master : TronCommunicatorStatus.Slave;

            if (Tcs == TronCommunicatorStatus.Master)
            {
                _server = new Server(1337, new List<char> { '\n', '\r' });
                _server.OnClientConnect += server_OnClientConnect;
                _server.OnClientDisconnect += server_OnClientDisconnect;
                _server.OnMessageReceived += server_OnMessageReceived;
                _server.StartAsync();

                _timer = new Timer {Interval = 1000};
                _timer.Tick += _timer_Tick;
                _timer.Start();
            }
            else if (masterIP != null)
            {
                byte[] buf = new [] {(byte) TronInstruction.Connect, (byte)'\n'};
                _serverConnection = new TcpClient();
                _serverConnection.Connect(_masterIP, 1337);
                _serverConnectionStream = _serverConnection.GetStream();
                _serverConnectionStream.BeginRead(new byte[0], 0, 0, ServerConnectionStreamOnRead,
                                                  _serverConnectionStream);
               
                _serverConnectionStream.Write(buf.ToArray(), 0, buf.Length);
            }
            Console.WriteLine("Running as " + Tcs);
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
                    byte b = (byte) stream.ReadByte();
                    if (b != (byte) '\n')
                        list.Add(b);
                }
                Parse(list.ToArray());
                _serverConnectionStream.BeginRead(new byte[0], 0, 0, ServerConnectionStreamOnRead,
                                                  _serverConnectionStream);
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught exception: {0}", e.Message);
                /* TODO: Mark this Communicator as "dirty" so it can't be used to communicate anymore */
            }
            
        }
        void _timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();
            FinalizeConnections();
        }
        void FinalizeConnections()
        {
            if (Players.Count == 0) return;
            Console.WriteLine("Finalizing connections");
            int gap = _gr.Width/Players.Count;
            int curx = 0;
            foreach(Player p in Players)
            {
                string ins = GeneratePacket(p, TronInstruction.AddToGrid, curx, _gr.Height/2);
                Parse(ins);
                Send(ins);
                
                p.XPos = curx;
                p.YPos = _gr.Height/2;
                curx += gap;
            }
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
            if (Tcs == TronCommunicatorStatus.Master)
            {
                var player = new Player(Players.Count);
                Players.Add(player);
                e.Client.Tag = player.PlayerNum;
                Console.WriteLine("Waiting 100 milliseconds for client");
                Thread.Sleep(100);
                e.Client.SendData("" + (int) TronInstruction.ChangePlayerNum + Separator + player.PlayerNum + "\n");
                e.Client.SendData(GeneratePacket(MainWindow.MePlayer, TronInstruction.DoNothing, MainWindow.MePlayer.XPos,
                                                 MainWindow.MePlayer.YPos) + "\n");
                Console.WriteLine("Player joined!");
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
            if (Tcs == TronCommunicatorStatus.Master)
                Send(instr);
            string str = Encoding.ASCII.GetString(instr);
            string[] strs = str.Split(Separator);
            if (strs.Length == 2)
            {
                MainWindow.MePlayer.PlayerNum = Int32.Parse(strs[1]);
                Console.WriteLine("Changing player number to " + MainWindow.MePlayer.PlayerNum);
            }
            else
            {
                var whattodo = (TronInstruction) Int32.Parse(strs[0]);
                var xcoord = Int32.Parse(strs[1]);
                var ycoord = Int32.Parse(strs[2]);
                var type = (TronType) Int32.Parse(strs[3]);
                switch (type)
                {
                    case TronType.Player:
                        {
                            var player = Player.Deserialize(strs[4]);

                            if (player.PlayerNum == MainWindow.MePlayer.PlayerNum)
                                _gr.Exec(whattodo, xcoord, ycoord, MainWindow.MePlayer);
                            else
                            {
                                bool found = false;
                                for(int x = 0; x < Players.Count; x++)
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
                                    Console.WriteLine("Adding new player: {0}",player.PlayerNum);
                                    if (player.PlayerNum == 0) Console.WriteLine("This is the MASTER player");
                                }
                                _gr.Exec(whattodo, xcoord, ycoord, player);
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
        public void Send(string tosend)
        {
            if (!tosend.EndsWith("\n"))
                tosend += '\n';
            Send(GetBytes(tosend));
        }
        public void Send(byte[] buf)
        {
            switch (Tcs)
            {
                case TronCommunicatorStatus.Slave:
                    {

                        NetworkStream stream = _serverConnection.GetStream();
                        stream.Write(buf.ToArray(), 0, buf.Length);
                        if (buf[buf.Length - 1] != (byte)'\n')
                            stream.WriteByte((byte) '\n');
                    }
                    break;
                case TronCommunicatorStatus.Master:
                    foreach(Client c in _server.ConnectedClients)
                    {
                        c.SendData(buf);
                        if (buf[buf.Length - 1] != (byte)'\n')
                            c.SendData(new[] {(byte) '\n'});
                    }
                    
                    break;
            }
        }

        public string GeneratePacket(TronBase te, TronInstruction instr, int x, int y)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append((byte)instr);
            sb.Append(Separator);
            sb.Append(x);
            sb.Append(Separator);
            sb.Append(y);
            sb.Append(Separator);
            sb.Append((int) te.GetTronType());
            sb.Append(Separator);
            sb.Append(te.Serialize());
            return sb.ToString();

        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using ServerFramework.NET;
using System.Windows.Forms;
namespace Netron
{
    
    /* TODO: Add intended recipient code */
    /*
     * Regular packet
     * [intended recipient]0x01[instruction]0x01[xcoord]0x01[ycoord]0x01[objecttype]0x01[serialized object]
     * 
     * Initialization packet
     * [ChangePlayerNum]0x01[num]
     */ 
    public enum TronInstruction
    {
        AddToGrid = 0x01, MoveEntity = 0x02, RemoveFromGrid = 0x03, DoNothing = 0x04, ChangePlayerNum=0x05, Connect=0x06, InstructionEnd = 0xFF
    }
    public enum TronCommunicatorStatus
    {
        Master, Slave
    }

    public class Communicator
    {
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
        public const char Separator = (char)0xFE;

        private readonly TcpClient _serverConnection;
        public Communicator(Grid gr, string masterIP = null)
        {
            _server = new Server(1337, new List<char> {'\n','\r'});
            _server.OnClientConnect += server_OnClientConnect;
            _server.OnClientDisconnect += server_OnClientDisconnect;
            _server.OnMessageReceived += server_OnMessageReceived;
            _masterIP = masterIP;
            _gr = gr;
            
            Players = new List<Player> {MainWindow.player};
            _server.StartAsync();
            Tcs = masterIP == null ? TronCommunicatorStatus.Master : TronCommunicatorStatus.Slave;
            if (Tcs == TronCommunicatorStatus.Master)
            {
                _timer = new Timer {Interval = 10000};

                _timer.Tick += _timer_Tick;
                _timer.Start();
            }
            else if (masterIP != null)
            {
                byte[] buf = new [] {(byte) TronInstruction.Connect, (byte)'\n'};
                _serverConnection = new TcpClient(_masterIP, 1337);
                NetworkStream stream = _serverConnection.GetStream();
                stream.Write(buf.ToArray(), 0, buf.Length);
            }
            Console.WriteLine("Running as " + Tcs);
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
                Send(GeneratePacket(p, TronInstruction.MoveEntity, curx, _gr.Height/2));
                
                p.XPos = curx;
                p.YPos = _gr.Height/2;
                curx += gap;
            }

        }
        void server_OnClientDisconnect(object sender, ClientEventArgs e)
        {
            for(int x= 0 ; x < Players.Count; x++)
            {
                if (Players[x].PlayerNum == ((Player)e.Client.Tag).PlayerNum)
                {
                    Players.RemoveAt(x);
                    Console.WriteLine("Player " + x + "removed");
                    return;
                }
            }
        }

        void server_OnClientConnect(object sender, ClientEventArgs e)
        {
            if (Tcs == TronCommunicatorStatus.Master)
            {
                var player = new Player(Players.Count);
                Players.Add(player);
                e.Client.Tag = player;
                e.Client.SendData("" + (int) TronInstruction.ChangePlayerNum + Separator + player.PlayerNum + "\n");
                e.Client.SendData(GeneratePacket(MainWindow.player, TronInstruction.DoNothing, MainWindow.player.XPos,
                                                 MainWindow.player.YPos) + "\n");
                Console.WriteLine("Player joined!");
            }

        }

        void server_OnMessageReceived(object sender, ClientEventArgs e)
        {
            Parse(e.Client.Message);
        }
        void Parse(byte[] instr)
        {
            if (instr.Length < 2) return;
            if (Tcs == TronCommunicatorStatus.Master)
                Send(instr);
            char[] chars = new char[instr.Length / sizeof(char)];
            Buffer.BlockCopy(instr, 0, chars, 0, instr.Length);
            string str = new string(chars);
            string[] strs = str.Split(Separator);
            if (strs.Length == 2)
            {
                MainWindow.player.PlayerNum = Int32.Parse(strs[1]);
                Console.WriteLine("Changing player number to " + MainWindow.player.PlayerNum);
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

                            if (player.PlayerNum == MainWindow.player.PlayerNum)
                                _gr.Exec(whattodo, xcoord, ycoord, MainWindow.player);
                            else
                            {
                                bool found = Players.Any(p => p.PlayerNum == player.PlayerNum);
                                if (!found)
                                {
                                    Players.Add(player);
                                    Console.WriteLine("Adding new player (finalized)");
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
            tosend += '\n';
            byte[] buf = new byte[tosend.Length * sizeof(char)];
            Buffer.BlockCopy(tosend.ToCharArray(), 0, buf, 0, buf.Length);
            Send(buf);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Net.Sockets;
using ServerFramework.NET;

namespace Netron
{
    
    
    /*
     * [instruction]0x01[xcoord]0x01[ycoord]0x01[objecttype]0x01[serialized object]
     */ 
    public enum TronInstruction
    {
        AddToGrid = 0x01, MoveEntity = 0x02, RemoveFromGrid = 0x03, DoNothing = 0x04, InstructionEnd = 0xFF
    }
    public enum TronCommunicatorStatus
    {
        Master, Slave
    }

    public class Communicator
    {
        public TronCommunicatorStatus Tcs;
        private readonly string _masterIP;
        private readonly List<Player> _players;
        private readonly Server _server;
        private readonly Grid _gr;

        const char Separator = (char)0xFE;
        public Communicator(Grid gr, string masterIP = null)
        {
            _server = new Server(1337, new List<char> {Separator});
            _server.OnMessageReceived += server_OnMessageReceived;
            _server.OnClientConnect += server_OnClientConnect;
            _server.OnClientDisconnect += server_OnClientDisconnect;
            _masterIP = masterIP;
            _gr = gr;
            _players = new List<Player>();
        }
        private string GetIPAddress(TcpClient tc)
        {
            var ipe = (IPEndPoint)tc.Client.RemoteEndPoint;
            return ipe.Address.ToString();
        }
        void server_OnClientDisconnect(object sender, ClientEventArgs e)
        {
            for(int x= 0 ; x < _players.Count; x++)
            {
                if (GetIPAddress(_players[x].IPClient).Equals(GetIPAddress(e.Client.TcpClient)))
                {
                    _players.RemoveAt(x);
                    return;
                }
            }
        }

        void server_OnClientConnect(object sender, ClientEventArgs e)
        {
            _players.Add(new Player(e.Client.TcpClient));
        }

        void server_OnMessageReceived(object sender, ClientEventArgs e)
        {
            Parse(e.Client.Message);
        }
        void Parse(byte[] instr)
        {
            if (Tcs == TronCommunicatorStatus.Master)
                Send(instr);
            char[] chars = new char[instr.Length / sizeof(char)];
            Buffer.BlockCopy(instr, 0, chars, 0, instr.Length);
            string str = new string(chars);
            string[] strs = str.Split(Separator);
            var whattodo = (TronInstruction) UInt32.Parse(strs[0]);
            var xcoord = UInt32.Parse(strs[1]);
            var ycoord = UInt32.Parse(strs[2]);
            var type = (TronType) UInt32.Parse(strs[3]);
            if (type == TronType.Player)
            {
                var player = Player.Deserialize(strs[4]);
                _gr.Exec(whattodo, xcoord, ycoord, player);
            }
            else if (type == TronType.Wall)
            {
                var wall = Wall.Deserialize(strs[4]);
                _gr.Exec(whattodo, xcoord, ycoord, wall);
            }

        }
        public void Send(byte[] buf)
        {
            if (Tcs == TronCommunicatorStatus.Slave)
            {
                TcpClient client = new TcpClient(_masterIP, 1337);
                NetworkStream stream = client.GetStream();
                stream.Write(buf.ToArray(), 0, buf.Length);
            }
            else if (Tcs == TronCommunicatorStatus.Master)
            {
                for(int x = 1; x < _players.Count; x++)
                {
                    TcpClient client = new TcpClient(GetIPAddress(_players[x].IPClient), 1337);
                    NetworkStream stream = client.GetStream();
                    stream.Write(buf.ToArray(), 0, buf.Length);
                }
            }
        }
        public void Send(TronBase te, TronInstruction instr, uint x, uint y)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append((byte)instr);
            sb.Append(Separator);
            sb.Append(x);
            sb.Append(Separator);
            sb.Append(y);
            sb.Append(Separator);
            sb.Append((uint) te.GetTronType());
            sb.Append(Separator);
            sb.Append(te.Serialize());
            string tosend = sb.ToString();
            byte[] buf = new byte[tosend.Length * sizeof(char)];
            Buffer.BlockCopy(tosend.ToCharArray(), 0, buf, 0, buf.Length);
            Send(buf);

        }

    }
}

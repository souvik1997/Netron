using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Netron
{
    public class Player : TronBase
    {
        public TcpClient IPClient
        {
            get;
            set;
        }
        public new TronType GetTronType()
        {
            return TronType.Player;
        }
        private string GetIPAddress(TcpClient tc)
        {
            var ipe = (IPEndPoint)tc.Client.RemoteEndPoint;
            return ipe.Address.ToString();
        }
        
        public Player(TcpClient c = null)
        {
            IPClient = c;
        }
        public new void Draw(Graphics g)
        {
            float[] coords = GetEquivalentLocation();
            g.DrawEllipse(new Pen(Color), coords[0], coords[1], coords[0] + 3, coords[1] + 3);
        }

        public new void Erase(Graphics g)
        {
            throw new NotImplementedException();
        }
        public new string Serialize()
        {
            StringBuilder sb = new StringBuilder(base.Serialize()+",");
            sb.Append(IPClient == null ? "" :GetIPAddress(IPClient));
            return sb.ToString();
        }
        public new static Player Deserialize(string str)
        {
            var strs = str.Split(',');
            Player p = new Player(strs[4].Equals("") ? null :new TcpClient(strs[4], 1337))
                           {
                               XPos = UInt32.Parse(strs[0]),
                               YPos = UInt32.Parse(strs[1]),
                               Direction = (DirectionType) Int32.Parse(strs[2]),
                               Color = Color.FromArgb(Int32.Parse(strs[3])),
                           };
            return p;
        }
        public void AcceptUserInput(DirectionType toTurn)
        {
            Direction = toTurn;
            MainWindow.Comm.Send(this, TronInstruction.DoNothing, XPos, YPos);
        }
        public new void Act()
        {
            uint oldx = XPos;
            uint oldy = YPos;
            var coords = GetAdjacentLocation(Direction, 1);
            if (Grid.IsValidLocation(coords[0], coords[1]))
                MoveTo(coords[0], coords[1]);
            else
            {
                int dir = (int) Direction;
                dir += 90;
                dir %= 360;
                Direction = (DirectionType)dir;
            }
            Wall wl = new Wall();
            wl.PutSelfInGrid(Grid, oldx, oldy);
        }
    }
}

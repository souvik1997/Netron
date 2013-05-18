using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Netron
{
    public class Player : TronBase
    {
        public int PlayerNum
        {
            get;
            set;
        }
        public override TronType GetTronType()
        {
            return TronType.Player;
        }
        
        public Player(int num)
        {
            PlayerNum = num;
        }

        
        public override string Serialize()
        {
            StringBuilder sb = new StringBuilder(base.Serialize()+",");
            sb.Append(PlayerNum);
            return sb.ToString();
        }
        public static Player Deserialize(string str)
        {
            Console.WriteLine(str);
            var strs = str.Split(',');
            Player p = new Player(Int32.Parse(strs[4]))
                           {
                               XPos = Int32.Parse(strs[0]),
                               YPos = Int32.Parse(strs[1]),
                               Direction = (DirectionType) Int32.Parse(strs[2]),
                               Color = Color.FromArgb(Int32.Parse(strs[3])),
                           };
            return p;
        }
        public void AcceptUserInput(DirectionType toTurn)
        {
            Direction = toTurn;
            MainWindow.Comm.Send(MainWindow.Comm.GeneratePacket(this, TronInstruction.DoNothing, XPos, YPos));
        }
        public override bool Equals(object obj)
        {
            Player p = obj as Player;
            if (p == null) return false;
            return base.Equals(p) && p.PlayerNum == PlayerNum;
        }
        public override void Act()
        {
            int oldx = XPos;
            int oldy = YPos;
            var coords = GetAdjacentLocation(Direction, 1);
            if (Grid.IsValidLocation(coords[0], coords[1]))
            {
                MoveTo(coords[0], coords[1]);
                Wall wl = new Wall();
                wl.PutSelfInGrid(Grid, oldx, oldy);
            }
            else
            {
                int dir = (int)Direction;
                dir += 90;
                dir %= 360;
                Direction = (DirectionType)dir;
            }
            
        }
    }
}

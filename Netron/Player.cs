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

        private readonly Bitmap _oimage;
        public override sealed Bitmap Image { get; set; }

        public Player(int num)
        {
            PlayerNum = num;
            _oimage = Properties.Resources.TronLightcycleFinal;
            Image = _oimage;
        }

        
        public override string Serialize()
        {
            StringBuilder sb = new StringBuilder(base.Serialize()+",");
            sb.Append(PlayerNum);
            return sb.ToString();
        }

        private Color _color;
        public override Color Color
        {
            get { return _color; }
            set
            {
                _color = value;
                Image = TintBitmap(_oimage, value);
            }
        }

        public static Player Deserialize(string str)
        {
            
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
        private bool MoveForwardIfAbleTo()
        {
            var coords = GetAdjacentLocation(Direction, 1);
            if (Grid.IsValidLocation(coords[0], coords[1]) && Grid.Get(coords[0], coords[1]) == null)
            {
                MoveTo(coords[0], coords[1]);
                return true;
            }
            return false;
        }
        public override void Act()
        {
            int oldx = XPos;
            int oldy = YPos;
            if (MoveForwardIfAbleTo())
            {
                Wall wl = new Wall {Direction = Direction, Color = Color};
                wl.PutSelfInGrid(Grid, oldx, oldy);
                MainWindow.Walls.Add(wl);
            }
            else
            {
                int dir = (int)Direction;
                dir += (new Random()).Next(2) == 1 ? 90: -90;
                dir %= 360;
                Turn((DirectionType)dir);
            }
            
        }
        private void Turn(DirectionType newDir)
        {

            DirectionType olddir = Direction;
            Direction = newDir;
            if (!Grid.IsValidLocation(GetAdjacentLocation(newDir,1)) || Grid.Get(GetAdjacentLocation(newDir, 1)) != null) return;
            
            int oldx = XPos;
            int oldy = YPos;
            
            MoveForwardIfAbleTo();
            Wall wl = null;
            if ((olddir == DirectionType.North && newDir == DirectionType.East) || (olddir == DirectionType.West && newDir == DirectionType.South))
                wl = (new Wall { Direction = DirectionType.Northwest, Color = Color });
            else if ((olddir == DirectionType.North && newDir == DirectionType.West) || (olddir == DirectionType.East && newDir == DirectionType.South))
                wl = (new Wall { Direction = DirectionType.Northeast, Color = Color });
            else if ((olddir == DirectionType.South && newDir == DirectionType.East) || (olddir == DirectionType.West && newDir == DirectionType.North))
                wl = (new Wall { Direction = DirectionType.Southwest, Color = Color });
            else if ((olddir == DirectionType.South && newDir == DirectionType.West) || (olddir == DirectionType.East && newDir == DirectionType.North))
                wl = (new Wall { Direction = DirectionType.Southeast, Color = Color });
            wl.PutSelfInGrid(Grid, oldx, oldy);
            MainWindow.Walls.Add(wl);
            

        }
    }
}

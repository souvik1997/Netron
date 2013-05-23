using System;
using System.Drawing;
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

        public bool Dead
        {
            get;
            private set;
        }
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
        public void AcceptUserInput(DirectionType toTurn, bool broadcast = true)
        {
            //Turn((DirectionType)(((int)toTurn+(int)Direction)%360));
            if (Dead) return;
            if (!broadcast)
            {
                MainWindow.NextTurns.Enqueue(toTurn);
                return;
            }
            if (toTurn != Direction && toTurn != (DirectionType)(((int)Direction+180)%360))
                Turn(toTurn);
            if (broadcast)
            {
                string packet = null;
                switch (toTurn)
                {
                    case DirectionType.East:
                        packet = MainWindow.Comm.GeneratePacket(this, TronInstruction.TurnRight, XPos, YPos);
                        break;
                    case DirectionType.West:
                        packet = MainWindow.Comm.GeneratePacket(this, TronInstruction.TurnLeft, XPos, YPos);
                        break;
                    case DirectionType.North:
                        packet = MainWindow.Comm.GeneratePacket(this, TronInstruction.TurnUp, XPos, YPos);
                        break;
                    case DirectionType.South:
                        packet = MainWindow.Comm.GeneratePacket(this, TronInstruction.TurnDown, XPos, YPos);
                        break;
                }
                MainWindow.Comm.Send(packet);
            }
        }
#pragma warning disable 659
        public override bool Equals(object obj)
#pragma warning restore 659
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

        private readonly object _actLock = new object();
        public override void Act()
        {
            lock (_actLock)
            {
                if (Dead) return;
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
                    Dead = true;
                }
            }

        }
        private void Turn(DirectionType newDir)
        {

            DirectionType olddir = Direction;
            Direction = newDir;
            if (!Grid.IsValidLocation(GetAdjacentLocation(newDir,1)) || Grid.Get(GetAdjacentLocation(newDir, 1)) != null)
            {
                Dead = true;
                return;
            }
            
            int oldx = XPos;
            int oldy = YPos;
            Console.WriteLine("Turning from {0} to {1}", olddir, newDir);
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

            if (wl != null)
            {
                wl.PutSelfInGrid(Grid, oldx, oldy);
                MainWindow.Walls.Add(wl);
            }
        }
    }
}

using System;
using System.Drawing;
using System.Text;
using Netron.Properties;

namespace Netron
{
    public class Player : TronBase //Player inherits from TronBase
    {
        public int PlayerNum //Property for player number
        { get; set; }

        public override TronType GetTronType() //Override tron type
        {
            return TronType.Player;
        }

        public DirectionType NextTurn //Property for next turn
        { get; set; }

        private static Bitmap _oimage = Resources.TronLightcycleFinal;
        private Bitmap _bmp;
        public override sealed Bitmap Image 
        { 
            get
            {
                if (Grid == null)
                {
                    return _bmp;
                }
                if (_bmp.Width != (int)Grid.CellWidth + 1 || _bmp.Height != (int)Grid.CellHeight + 1)
                {
                    _bmp = resize(_bmp, (int)Grid.CellWidth + 1, (int)Grid.CellHeight + 1);
                    _oimage = resize(_oimage, (int)Grid.CellWidth + 1, (int)Grid.CellHeight + 1);
                }
                return _bmp;
            }
            set
            {
                if (Grid == null)
                {
                    _bmp = value;
                    return;
                }
                if (value.Width != (int)Grid.CellWidth+1 || value.Height != (int)Grid.CellHeight+1)
                {
                    _bmp = resize(value, (int) Grid.CellWidth + 1, (int) Grid.CellHeight + 1);
                    _oimage = resize(_oimage, (int)Grid.CellWidth + 1, (int)Grid.CellHeight + 1);
                }
            }
        } //Sealed property for the image

        public void Kill()
        {
            MainWindow.Comm.Send(MainWindow.Comm.GeneratePacket(this, TronInstruction.Kill, XPos, YPos));
            Dead = true;

        }
        public bool Dead //Property for if the player is dead
        {
            get;
            set;

        }

        public Player(int num) //Constructor
        {
            PlayerNum = num; //store player umber
             //Load image from resources
            Image = _oimage; //store image property
            NextTurn = DirectionType.Null; //set next turn to a null direction
        }


        public override string Serialize() //Serialize the object
        {
            var sb = new StringBuilder(base.Serialize()); //get the superclass serialization
            sb.Append(",");
            sb.Append(PlayerNum); //append the player number
            sb.Append(",");
            sb.Append(Dead ? "1" : "0");
            return sb.ToString(); //return string
        }

        private Color _color; //private variable for color

        public override Color Color //Property for color
        {
            get { return _color; } //Accessor returns color
            set
            {
                _color = value; //Set backing variable to value
                Image = TintBitmap(_oimage, value); //Tint image to the color
            }
        }

        public static Player Deserialize(string str) //Deserialize a string to a player
        {
            string[] strs = str.Split(','); //split string
            var p = new Player(Int32.Parse(strs[4]))
                //create player with player number, xpos, ypos, direction, and color
                        {
                            XPos = Int32.Parse(strs[0]),
                            YPos = Int32.Parse(strs[1]),
                            Direction = (DirectionType) Int32.Parse(strs[2]),
                            Color = Color.FromArgb(Int32.Parse(strs[3])),
                            Dead = Int32.Parse(strs[5]) == 1
                        };
            return p; //return player
        }

        public bool FlushTurns() //Flush pending turns. Returns false if no turns were flushed
        {
            if (NextTurn == DirectionType.Null) return false; //no turn
            Turn(NextTurn);
            NextTurn = DirectionType.Null;
            return true; //
        }

        public void AcceptUserInput(DirectionType toTurn, bool broadcast = true)
        {
            //Turn((DirectionType)(((int)toTurn+(int)Direction)%360));
            if (Dead) return;
            if (toTurn != Direction && toTurn != (DirectionType) (((int) Direction + 180)%360))
                //turn if the direction changes
            {
                NextTurn = toTurn;
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
        }
#pragma warning disable 659
        public override bool Equals(object obj)
#pragma warning restore 659
        {
            var p = obj as Player;
            if (p == null) return false;
            return base.Equals(p) && p.PlayerNum == PlayerNum;
        }

        private bool MoveForwardIfAbleTo()
        {
            int[] coords = GetAdjacentLocation(Direction, 1);
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
                FlushTurns();
                int oldx = XPos;
                int oldy = YPos;
                if (MoveForwardIfAbleTo())
                {
                    var wl = new Wall {Direction = Direction, Color = Color};
                    wl.PutSelfInGrid(Grid, oldx, oldy);
                    int x = 0;
                    for (x = 0; x < MainWindow.Walls.Count; x++)
                    {
                        if (MainWindow.Walls[x].XPos == wl.XPos && MainWindow.Walls[x].YPos == wl.YPos)
                        {
                            MainWindow.Walls[x] = wl;
                            break;
                        }
                    }
                    if (x >= MainWindow.Walls.Count)
                        MainWindow.Walls.Add(wl);
                }
                else
                {
                    Kill();
                }
            }
        }

        private void Turn(DirectionType newDir)
        {
            DirectionType olddir = Direction;
            Direction = newDir;
            if (!Grid.IsValidLocation(GetAdjacentLocation(newDir, 1)) ||
                Grid.Get(GetAdjacentLocation(newDir, 1)) != null)
            {
                Kill();
                return;
            }

            int oldx = XPos;
            int oldy = YPos;
            Program.Log.WriteLine(string.Format("Turning from {0} to {1}", olddir, newDir));
            MoveForwardIfAbleTo();
            Wall wl = null;
            if ((olddir == DirectionType.North && newDir == DirectionType.East) ||
                (olddir == DirectionType.West && newDir == DirectionType.South))
                wl = (new Wall {Direction = DirectionType.Northwest, Color = Color});
            else if ((olddir == DirectionType.North && newDir == DirectionType.West) ||
                     (olddir == DirectionType.East && newDir == DirectionType.South))
                wl = (new Wall {Direction = DirectionType.Northeast, Color = Color});
            else if ((olddir == DirectionType.South && newDir == DirectionType.East) ||
                     (olddir == DirectionType.West && newDir == DirectionType.North))
                wl = (new Wall {Direction = DirectionType.Southwest, Color = Color});
            else if ((olddir == DirectionType.South && newDir == DirectionType.West) ||
                     (olddir == DirectionType.East && newDir == DirectionType.North))
                wl = (new Wall {Direction = DirectionType.Southeast, Color = Color});

            if (wl != null)
            {
                wl.PutSelfInGrid(Grid, oldx, oldy);
                MainWindow.Walls.Add(wl);
            }
        }
    }
}
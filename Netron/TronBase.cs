using System.Drawing;
using System.Text;

namespace Netron
{
    public enum TronType
    {
        Bullet, Player, Wall
    }
    public abstract class TronBase 
    {
        public enum DirectionType
        {
            North = 45*0, Northeast = 45*1, East = 45*2, Southeast = 45*3, South = 45*4, Southwest = 45*5, West = 45*6, Northwest = 45*7
        }
        public abstract TronType GetTronType();
        public Bitmap Image
        {
            get;
            set;
        }

        public virtual string Serialize()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(XPos);
            sb.Append(",");
            sb.Append(YPos);
            sb.Append(",");
            sb.Append((int) Direction);
            sb.Append(",");
            sb.Append(Color.ToArgb());
            return sb.ToString();
        }
        public Color Color
        {
            get;
            set;
        }
        public Grid Grid
        {
            get;
            private set;
        }
        public bool IsInGrid
        {
            get;
            set;
        }
        public int XPos
        {
            get;
            set;
        }
        public int YPos
        {
            get;
            set;
        }
        public DirectionType Direction { get; set; }

        public void PutSelfInGrid(Grid gr, int x, int y)
        {
            Grid = gr;
            gr.Set(this, x, y);
            XPos = x;
            YPos = y;
            IsInGrid = true;
        }
        public void RemoveFromGrid()
        {
            Grid.Remove(XPos, YPos);
            IsInGrid = false;
        }
        public void MoveTo(int newx, int newy)
        {
            Grid.Move(XPos, YPos, newx, newy);
            XPos = newx;
            YPos = newy;
        }
        public int[] GetAdjacentLocation(DirectionType dt, int howMuchToMove)
        {
            int proposedx = XPos;
            int proposedy = YPos;
            if (dt == DirectionType.North && proposedy > howMuchToMove-1)
            {
                proposedy -= howMuchToMove;
            }
            else if (dt == DirectionType.Northeast && proposedy > howMuchToMove-1)
            {
                proposedy -= howMuchToMove;
                proposedx += howMuchToMove;
            }
            else if (dt == DirectionType.East)
            {
                proposedx += howMuchToMove;
            }
            else if (dt == DirectionType.Southeast)
            {
                proposedx += howMuchToMove;
                proposedy += howMuchToMove;
            }
            else if (dt == DirectionType.South)
            {
                proposedy += howMuchToMove;
            }
            else if (dt == DirectionType.Southwest && proposedx > howMuchToMove-1)
            {
                proposedx -= howMuchToMove;
                proposedy += howMuchToMove;
            }
            else if (dt == DirectionType.West && proposedx > howMuchToMove-1)
            {
                proposedx -= howMuchToMove;
            }
            else if (dt == DirectionType.Northwest && proposedy > howMuchToMove-1)
            {
                proposedx += howMuchToMove;
                proposedy -= howMuchToMove;
            }
            return new[] {proposedx, proposedy};

        }
        public override bool Equals(object obj)
        {
            TronBase tb = obj as TronBase;
            if (tb == null) return false;
            return tb.XPos == XPos && tb.YPos == YPos && tb.Color.ToArgb() == Color.ToArgb() &&
                   tb.Direction == Direction;
        }
        public abstract void Act();
    }
    
}

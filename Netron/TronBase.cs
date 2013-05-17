using System.Drawing;
using System.Text;

namespace Netron
{
    public enum TronType
    {
        Bullet, Player, Wall
    }
    public abstract class TronBase : IDrawable
    {
        public enum DirectionType
        {
            North = 45*0, Northeast = 45*1, East = 45*2, Southeast = 45*3, South = 45*4, Southwest = 45*5, West = 45*6, Northwest = 45*7
        }
        public abstract TronType GetTronType();
        
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
        public uint XPos
        {
            get;
            set;
        }
        public uint YPos
        {
            get;
            set;
        }
        public uint DrawableWidth
        {
            get; set;
        }
        public uint DrawableHeight
        {
            get; set;
        }
        public DirectionType Direction { get; set; }

        public void PutSelfInGrid(Grid gr, uint x, uint y)
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
        public void MoveTo(uint newx, uint newy)
        {
            Grid.Move(XPos, YPos, newx, newy);
            XPos = newx;
            YPos = newy;
        }
        public uint[] GetAdjacentLocation(DirectionType dt, uint howMuchToMove)
        {
            uint proposedx = XPos;
            uint proposedy = YPos;
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
        private float fix(uint oldVal, uint oldMax, uint oldMin, uint newMax, uint newMin)
        {
            return (((oldVal - oldMin) * (float)(newMax - newMin)) / (oldMax - oldMin)) + newMin;

        }
        public float[] GetEquivalentLocation()
        {
            return new[] { fix(XPos, Grid.Width, 0, DrawableWidth, 0), fix(YPos, Grid.Height, 0, DrawableHeight, 0) };
        }
        public abstract void Act();

        public abstract void Draw(System.Drawing.Graphics g);

        public abstract void Erase(System.Drawing.Graphics g);
    }
    
}

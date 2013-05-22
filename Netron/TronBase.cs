using System;
using System.Drawing;
using System.Drawing.Imaging;
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
            North = 45*0, Northeast = 45*1, East = 45*2, Southeast = 45*3, South = 45*4, Southwest = 45*5, West = 45*6, Northwest = 45*7, Null = -1
        }
        public abstract TronType GetTronType();
        public abstract Bitmap Image
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

        public abstract Color Color { get; set; }
        protected static Bitmap TintBitmap(Bitmap b, Color tintColor)
        {
            
            Bitmap b2 = new Bitmap(b.Width, b.Height);
            for (int x = 0; x < b.Width; x++)
            {
                for (int y = 0; y < b.Height; y++)
                {
                    
                    Color src = b.GetPixel(x, y);
                    
                    Color newColor = Color.FromArgb(src.A, (src.R + tintColor.R)/2, (src.G + tintColor.G)/2,
                                                    (src.B + tintColor.B)/2);
                    
                    b2.SetPixel(x, y,
                                newColor);
                    
                }
            }
            return b2;
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
            if (dt == DirectionType.North )
            {
                proposedy -= howMuchToMove;
            }
            else if (dt == DirectionType.Northeast)
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
            else if (dt == DirectionType.Southwest)
            {
                proposedx -= howMuchToMove;
                proposedy += howMuchToMove;
            }
            else if (dt == DirectionType.West)
            {
                proposedx -= howMuchToMove;
            }
            else if (dt == DirectionType.Northwest)
            {
                proposedx += howMuchToMove;
                proposedy -= howMuchToMove;
            }
            return new[] {proposedx, proposedy};

        }
#pragma warning disable 659
        public override bool Equals(object obj)
#pragma warning restore 659
        {
            TronBase tb = obj as TronBase;
            if (tb == null) return false;
            return tb.XPos == XPos && tb.YPos == YPos && tb.Color.ToArgb() == Color.ToArgb() &&
                   tb.Direction == Direction;
        }
        public abstract void Act();
    }
    
}

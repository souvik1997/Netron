using System;
using System.Drawing;

namespace Netron
{
    public class Grid
    {
        public TronBase[,] Map
        {
            get;
            private set;
        }
        public int Width
        {
            get { return (int)Map.GetLength(0); }
        }
        public int Height
        {
            get { return (int)Map.GetLength(1); }
        }
        public Grid(int width, int height)
        {
            Map = new TronBase[height,width];
        }
        public void Set(TronBase tb, int x, int y)
        {
            if (Get(x, y) != null)
                Get(x, y).IsInGrid = false;
            Map[y, x] = tb;
        }
        public TronBase Remove(int x, int y)
        {
            TronBase tb = Get(x, y);
            Set(null, x, y);
            return tb;
        }
        public void Move(int x, int y, int newx, int newy)
        {
            Set(Remove(x, y), newx, newy);
        }
        public void Move(TronBase tb, int newx, int newy)
        {
            Set(Remove(tb.XPos, tb.YPos), newx, newy);
        }
        public TronBase Get(int x, int y)
        {
            return Map[y, x];
        }
        public void ActAll()
        {
            foreach (TronBase tb in Map)
            {
                tb.Act();
            }
        }
        public bool IsValidLocation(int x, int y)
        {
            Console.WriteLine("" + x + "," + y);
            return (x >= 0) && (y >= 0) && (x < Width) && (y < Height);
        }
        public void Exec(TronInstruction ti, int x, int y, TronBase tb)
        {
            if (ti == TronInstruction.AddToGrid)
            {
                tb.PutSelfInGrid(this, x, y);
            }
            else if (ti == TronInstruction.MoveEntity)
            {
                tb.MoveTo(x,y);
            }
            else if (ti == TronInstruction.RemoveFromGrid)
            {
                tb.RemoveFromGrid();
            }
        }
    }
}

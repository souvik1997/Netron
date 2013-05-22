using System;
using System.Collections.Generic;
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
            get { return (int)Map.GetLength(1); }
        }
        public int Height
        {
            get { return (int)Map.GetLength(0); }
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
            Move(tb.XPos, tb.YPos, newx, newy);
        }
        public TronBase Get(int x, int y)
        {
            return Map[y, x];
        }
        public TronBase Get(int[] coords)
        {
            return Get(coords[0], coords[1]);
        }
        public void Set(TronBase tb, int[] coords)
        {
            Set(tb, coords[0], coords[1]);
        }
        public void ActAll()
        {
            foreach (TronBase tb in Map)
            {
                if (tb != null)
                    tb.Act();
            }
        }
        public List<TronBase> GetAllNeighboring(int xCoord, int yCoord)
        {
            List<TronBase> list = new List<TronBase>();
            for(int x = xCoord - 1; x < xCoord + 1; x++)
            {
                for (int y = yCoord -1; y < yCoord +1; y++)
                {
                    TronBase tb = Get(x, y);
                    if ((y != yCoord || x != xCoord) && IsValidLocation(x,y) && tb != null)
                        list.Add(tb);
                }
            }
            return list;
        }
        public bool IsValidLocation(int x, int y)
        {
            return (x >= 0) && (y >= 0) && (x < Width) && (y < Height);
        }
        public bool IsValidLocation(int[] coords)
        {
            return IsValidLocation(coords[0], coords[1]);
        }
        public void Exec(TronInstruction ti, int x, int y, TronBase tb)
        {
            switch (ti)
            {
                case TronInstruction.AddToGrid:
                    tb.PutSelfInGrid(this, x, y);
                    break;
                case TronInstruction.MoveEntity:
                    tb.MoveTo(x,y);
                    break;
                case TronInstruction.RemoveFromGrid:
                    tb.RemoveFromGrid();
                    break;
            }
        }
    }
}

using System.Drawing;

namespace Netron
{
    public class Grid
    {
        private readonly TronBase[,] _grid;
        public uint Width
        {
            get { return (uint)_grid.GetLength(0); }
        }
        public uint Height
        {
            get { return (uint)_grid.GetLength(1); }
        }
        public Grid(uint width, uint height)
        {
            _grid = new TronBase[height,width];
        }
        public void Set(TronBase tb, uint x, uint y)
        {
            _grid[y, x] = tb;
        }
        public TronBase Remove(uint x, uint y)
        {
            TronBase tb = Get(x, y);
            Set(null, x, y);
            return tb;
        }
        public void Move(uint x, uint y, uint newx, uint newy)
        {
            Set(Remove(x, y), newx, newy);
        }
        public void Move(TronBase tb, uint newx, uint newy)
        {
            Set(Remove(tb.XPos, tb.YPos), newx, newy);
        }
        public TronBase Get(uint x, uint y)
        {
            return _grid[y, x];
        }
        public void DrawAll(Graphics g)
        {
            foreach(TronBase tb in _grid)
            {
                tb.Draw(g);
            }
        }
        public void ActAll()
        {
            foreach (TronBase tb in _grid)
            {
                tb.Act();
            }
        }
        public bool IsValidLocation(uint x, uint y)
        {
            return (x < _grid.GetLength(0)) && (y < _grid.GetLength(1));
        }
        public void Exec(TronInstruction ti, uint x, uint y, TronBase tb)
        {
            if (ti == TronInstruction.AddToGrid)
            {
                Set(tb, x, y);
            }
            else if (ti == TronInstruction.MoveEntity)
            {
                Move(tb, x, y);
            }
            else if (ti == TronInstruction.RemoveFromGrid)
            {
                Remove(x,y);
            }
        }
    }
}

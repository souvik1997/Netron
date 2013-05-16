using System;
using System.Drawing;

namespace Netron
{
    public class Wall : TronBase
    {

        public new TronType GetTronType()
        {
            return TronType.Wall;
        }
        public new void Draw(Graphics g)
        {
            float[] coords = GetEquivalentLocation();
            g.DrawRectangle(new Pen(Color), coords[0], coords[1], coords[0] + 1, coords[1] + 1);
        }

        public new void Erase(Graphics g)
        {
            throw new NotImplementedException();
        }
        
        public new static Wall Deserialize(string str)
        {
            var strs = str.Split(',');
            Wall wl = new Wall
                          {
                              XPos = UInt32.Parse(strs[0]),
                              YPos = UInt32.Parse(strs[1]),
                              Direction = (DirectionType) UInt32.Parse(strs[2]),
                              Color = Color.FromArgb(Int32.Parse(strs[3]))
                          };
            return wl;
        }
        public new void Act()
        {

        }
    }
}

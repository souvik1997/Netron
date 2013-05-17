using System;
using System.Drawing;

namespace Netron
{
    public class Wall : TronBase
    {

        public override TronType GetTronType()
        {
            return TronType.Wall;
        }

        public override void Erase(Graphics g)
        {
            throw new NotImplementedException();
        }
        
        public static Wall Deserialize(string str)
        {
            var strs = str.Split(',');
            Wall wl = new Wall
                          {
                              XPos = Int32.Parse(strs[0]),
                              YPos = Int32.Parse(strs[1]),
                              Direction = (DirectionType) Int32.Parse(strs[2]),
                              Color = Color.FromArgb(Int32.Parse(strs[3]))
                          };
            return wl;
        }
        public override void Act()
        {

        }
    }
}

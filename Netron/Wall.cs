using System;
using System.Drawing;

namespace Netron
{
    public class Wall : TronBase
    {
        private static readonly Bitmap owallNS = Properties.Resources.WallNS;
        private static readonly Bitmap owallEW = Properties.Resources.WallEW;
        private static readonly Bitmap owallUR = Properties.Resources.WallUR;
        private static readonly Bitmap owallUL = Properties.Resources.WallUL;
        private static readonly Bitmap owallBL = Properties.Resources.WallBL;
        private static readonly Bitmap owallBR = Properties.Resources.WallBR;

        private Bitmap wallNS;
        private Bitmap wallEW;
        private Bitmap wallUR;
        private Bitmap wallUL;
        private Bitmap wallBL;
        private Bitmap wallBR;
        public Wall()
        {
            wallNS = owallNS;
            wallEW = owallEW;
            wallUR = owallUR;
            wallUL = owallUL;
            wallBL = owallBL;
            wallBR = owallBR;
        }
        
        public override TronType GetTronType()
        {
            return TronType.Wall;
        }

        public override Bitmap Image
        {
            get
            {
                Bitmap obj = null;
                switch (Direction)
                {
                    case DirectionType.East:
                    case DirectionType.West:
                        obj = wallEW;
                        break;
                    case DirectionType.North:
                    case DirectionType.South:
                        obj = wallNS;
                        break;
                    case DirectionType.Northwest:
                        obj = wallUL;
                        break;
                    case DirectionType.Northeast:
                        obj = wallUR;
                        break;
                    case DirectionType.Southeast:
                        obj = wallBR;
                        break;
                    case DirectionType.Southwest:
                        obj = wallBL;
                        break;
                        
                }
                return obj;
            }
            set
            {
                
            }
        }

        private Color _color;
        public override Color Color 
        { 
            get { return _color; }
            set
            {
                wallNS = TintBitmap(owallNS, value);
                wallEW = TintBitmap(owallEW, value);
                wallBL = TintBitmap(owallBL, value);
                wallBR = TintBitmap(owallBR, value);
                wallUL = TintBitmap(owallUL, value);
                wallUR = TintBitmap(owallUR, value);
                _color = value;
            } 
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

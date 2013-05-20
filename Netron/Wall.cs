using System;
using System.Drawing;
using System.Collections.Generic;
namespace Netron
{
    public class IconSet
    {
        public Bitmap wallNS;
        public Bitmap wallEW;
        public Bitmap wallUR;
        public Bitmap wallUL;
        public Bitmap wallBL;
        public Bitmap wallBR;
    }
    public class Wall : TronBase
    {
        public static List<Color> IconSetColors = new List<Color>();
        public static List<IconSet> IconSets = new List<IconSet>();

        private static readonly Bitmap owallNS = Properties.Resources.WallNS;
        private static readonly Bitmap owallEW = Properties.Resources.WallEW;
        private static readonly Bitmap owallUR = Properties.Resources.WallUR;
        private static readonly Bitmap owallUL = Properties.Resources.WallUL;
        private static readonly Bitmap owallBL = Properties.Resources.WallBL;
        private static readonly Bitmap owallBR = Properties.Resources.WallBR;
        IconSet ics;
        
        public Wall()
        {
            ics = new IconSet();
            ics.wallNS = owallNS;
            ics.wallEW = owallEW;
            ics.wallUR = owallUR;
            ics.wallUL = owallUL;
            ics.wallBL = owallBL;
            ics.wallBR = owallBR;            
            
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
                        obj = ics.wallEW;
                        break;
                    case DirectionType.North:
                    case DirectionType.South:
                        obj = ics.wallNS;
                        break;
                    case DirectionType.Northwest:
                        obj = ics.wallUL;
                        break;
                    case DirectionType.Northeast:
                        obj = ics.wallUR;
                        break;
                    case DirectionType.Southeast:
                        obj = ics.wallBR;
                        break;
                    case DirectionType.Southwest:
                        obj = ics.wallBL;
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
                
                if (IconSetColors.Contains(value))
                    ics = IconSets[IconSetColors.IndexOf(value)];
                else
                {
                    Console.WriteLine("*");
                    ics = new IconSet();
                    ics.wallNS = TintBitmap(owallNS, value);
                    ics.wallEW = TintBitmap(owallEW, value);
                    ics.wallBL = TintBitmap(owallBL, value);
                    ics.wallBR = TintBitmap(owallBR, value);
                    ics.wallUL = TintBitmap(owallUL, value);
                    ics.wallUR = TintBitmap(owallUR, value);
                    IconSetColors.Add(value);
                    IconSets.Add(ics);
                }
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

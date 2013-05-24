using System;
using System.Diagnostics;
using System.Drawing;
using System.Collections.Generic;
namespace Netron
{
    public class IconSet
    {
        public Bitmap WallNS;
        public Bitmap WallEW;
        public Bitmap WallUR;
        public Bitmap WallUL;
        public Bitmap WallBL;
        public Bitmap WallBR;
    }
    public class Wall : TronBase
    {
        public static Dictionary<Color, IconSet> IconSets = new Dictionary<Color, IconSet>();

        private static readonly Bitmap _owallNS = Properties.Resources.WallNS;
        private static readonly Bitmap _owallEW = Properties.Resources.WallEW;
        private static readonly Bitmap _owallUR = Properties.Resources.WallUR;
        private static readonly Bitmap _owallUL = Properties.Resources.WallUL;
        private static readonly Bitmap _owallBL = Properties.Resources.WallBL;
        private static readonly Bitmap _owallBR = Properties.Resources.WallBR;
        IconSet _ics;
        
        public Wall()
        {
            
            _ics = new IconSet
                    {
                        WallNS = _owallNS,
                        WallEW = _owallEW,
                        WallUR = _owallUR,
                        WallUL = _owallUL,
                        WallBL = _owallBL,
                        WallBR = _owallBR
                    };
        }
        public Wall(Color c):this()
        {
            Color = c; 
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
                        obj = _ics.WallEW;
                        break;
                    case DirectionType.North:
                    case DirectionType.South:
                        obj = _ics.WallNS;
                        break;
                    case DirectionType.Northwest:
                        obj = _ics.WallUL;
                        break;
                    case DirectionType.Northeast:
                        obj = _ics.WallUR;
                        break;
                    case DirectionType.Southeast:
                        obj = _ics.WallBR;
                        break;
                    case DirectionType.Southwest:
                        obj = _ics.WallBL;
                        break;
                        
                }
                return obj;
            }
            set
            {
                
            }
        }

        private Color _color;
        public override sealed Color Color 
        { 
            get { return _color; }
            set
            {
                
                if (IconSets.ContainsKey(value))
                    _ics = IconSets[value];
                else
                {
                    Debug.WriteLine("*");
                    _ics = new IconSet
                              {
                                  WallNS = TintBitmap(_owallNS, value),
                                  WallEW = TintBitmap(_owallEW, value),
                                  WallBL = TintBitmap(_owallBL, value),
                                  WallBR = TintBitmap(_owallBR, value),
                                  WallUL = TintBitmap(_owallUL, value),
                                  WallUR = TintBitmap(_owallUR, value)
                              };
                    IconSets.Add(value, _ics);
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

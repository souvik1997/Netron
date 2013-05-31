#region

using System;
using System.Collections.Generic;
using System.Drawing;
using Netron.Properties;

#endregion

namespace Netron
{
    public class IconSet
    {
        public Bitmap WallBL;
        public Bitmap WallBR;
        public Bitmap WallEW;
        public Bitmap WallNS;
        public Bitmap WallUL;
        public Bitmap WallUR;
    }

    public class Wall : TronBase
    {
        public static Dictionary<Color, IconSet> IconSets = new Dictionary<Color, IconSet>();

        private static Bitmap _owallNS = Resources.WallNS;
        private static Bitmap _owallEW = Resources.WallEW;
        private static Bitmap _owallUR = Resources.WallUR;
        private static Bitmap _owallUL = Resources.WallUL;
        private static Bitmap _owallBL = Resources.WallBL;
        private static Bitmap _owallBR = Resources.WallBR;
        private Color _color;
        private IconSet _ics;

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

        public Wall(Color c) : this()
        {
            Color = c;
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
                if (Grid == null || obj == null)
                {
                    return obj;
                }
                if (obj.Width != (int) Grid.CellWidth + 1 || obj.Height != (int) Grid.CellHeight + 1)
                {
                    obj = resize(obj, (int) Grid.CellWidth + 1, (int) Grid.CellHeight + 1);
                }
                return obj;
            }
            set { }
        }

        public override sealed Color Color
        {
            get { return _color; }
            set
            {
                if (IconSets.ContainsKey(value))
                    _ics = IconSets[value];
                else
                {
                    if (Grid != null)
                    {
                        if (_owallNS.Width != (int) Grid.CellWidth + 1 || _owallNS.Height != (int) Grid.CellHeight + 1)
                        {
                            _owallNS = resize(_owallNS, (int) Grid.CellWidth + 1, (int) Grid.CellHeight + 1);
                            _owallEW = resize(_owallEW, (int) Grid.CellWidth + 1, (int) Grid.CellHeight + 1);
                            _owallBL = resize(_owallBL, (int) Grid.CellWidth + 1, (int) Grid.CellHeight + 1);
                            _owallBR = resize(_owallBR, (int) Grid.CellWidth + 1, (int) Grid.CellHeight + 1);
                            _owallUL = resize(_owallUL, (int) Grid.CellWidth + 1, (int) Grid.CellHeight + 1);
                            _owallUR = resize(_owallUR, (int) Grid.CellWidth + 1, (int) Grid.CellHeight + 1);
                        }
                    }
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

        public override TronType GetTronType()
        {
            return TronType.Wall;
        }


        public static Wall Deserialize(string str)
        {
            var strs = str.Split(',');
            var wl = new Wall
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
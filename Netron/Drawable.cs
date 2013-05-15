using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
namespace Netron
{
    interface Drawable
    {
        public void Draw(Graphics g);
        public void Erase(Graphics g);
    }
}

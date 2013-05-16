using System.Drawing;
namespace Netron
{
    interface IDrawable
    {
        void Draw(Graphics g);
        void Erase(Graphics g);
    }
}

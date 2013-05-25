using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace Netron
{
    public enum TronType //Types of TronBases. Eliminates the need for reflection/other hacky things
    {
        Player, Wall
    }
    public abstract class TronBase  //Base class for things in the grid
    {
        
        public enum DirectionType
        {
            North = 45*0, Northeast = 45*1, East = 45*2, Southeast = 45*3, South = 45*4, Southwest = 45*5, West = 45*6, Northwest = 45*7, Null = -1
        }
        public abstract TronType GetTronType(); //Overrided to return the type of the object
        public abstract Bitmap Image //Property for image
        {
            get;
            set;
        }

        public virtual string Serialize() //Serializes this object into a string. Can be overrided (hence virtual)
        {
            StringBuilder sb = new StringBuilder(); //Create stringbuilder
            sb.Append(XPos); //Append info
            sb.Append(",");
            sb.Append(YPos);
            sb.Append(",");
            sb.Append((int) Direction);
            sb.Append(",");
            sb.Append(Color.ToArgb());
            return sb.ToString(); //return as string
        }

        public abstract Color Color { get; set; } //Property for color
        protected static Bitmap TintBitmap(Bitmap b, Color tintColor) //Helper method to tint a bitmap to a color
        {
            
            Bitmap b2 = new Bitmap(b.Width, b.Height); //Create a new bitmap
            for (int x = 0; x < b.Width; x++) //Go through each pixel
            {
                for (int y = 0; y < b.Height; y++)
                {
                    
                    Color src = b.GetPixel(x, y); //Get the color
                    
                    Color newColor = Color.FromArgb(src.A, (src.R + tintColor.R)/2, (src.G + tintColor.G)/2,
                                                    (src.B + tintColor.B)/2); //Average source and tint colors
                    
                    b2.SetPixel(x, y,
                                newColor); //Set pixel to new color
                    
                }
            }
            return b2; //return the new bitmap
        }
        public Grid Grid //Property for the grid (Like getGrid() in GridWorld)
        {
            get;
            private set; //Not accessible by everything
        }
        public bool IsInGrid //Property for if this is in a grid
        {
            get;
            set;
        }
        public int XPos //Property for x coordinate
        {
            get;
            set;
        }
        public int YPos //Property for y coordinate
        {
            get;
            set;
        }
        public DirectionType Direction { get; set; } //Property for direction

        public void PutSelfInGrid(Grid gr, int x, int y) //Puts this in the specified grid
        {
            Grid = gr; //Set property
            gr.Set(this, x, y); //Add this to grid
            XPos = x; //Update variables
            YPos = y;
            IsInGrid = true; 
        }
        public void RemoveFromGrid() //Removes this from the grid
        {
            Grid.Remove(XPos, YPos); //Remove
            IsInGrid = false; //Update
        }
        public void MoveTo(int newx, int newy) //Moves to a new location
        {
             
            Grid.Move(XPos, YPos, newx, newy); //Move
            XPos = newx; //Update
            YPos = newy;
        }
        public int[] GetAdjacentLocation(DirectionType dt, int howMuchToMove) //Gets the adjacent location in a direction and interval
        {
            int proposedx = XPos; //Variables for new coordinates
            int proposedy = YPos;
            if (dt == DirectionType.North ) 
            {
                proposedy -= howMuchToMove;
            }
            else if (dt == DirectionType.Northeast)
            {
                proposedy -= howMuchToMove;
                proposedx += howMuchToMove;
            }
            else if (dt == DirectionType.East)
            {
                proposedx += howMuchToMove;
            }
            else if (dt == DirectionType.Southeast)
            {
                proposedx += howMuchToMove;
                proposedy += howMuchToMove;
            }
            else if (dt == DirectionType.South)
            {
                proposedy += howMuchToMove;
            }
            else if (dt == DirectionType.Southwest)
            {
                proposedx -= howMuchToMove;
                proposedy += howMuchToMove;
            }
            else if (dt == DirectionType.West)
            {
                proposedx -= howMuchToMove;
            }
            else if (dt == DirectionType.Northwest)
            {
                proposedx += howMuchToMove;
                proposedy -= howMuchToMove;
            }
            return new[] {proposedx, proposedy}; //Return as an array

        }
#pragma warning disable 659 //I don't care about hash codes
        public override bool Equals(object obj) 
#pragma warning restore 659
        {
            TronBase tb = obj as TronBase; //Safely typecast and exit if error
            if (tb == null) return false;
            return tb.XPos == XPos && tb.YPos == YPos && tb.Color.ToArgb() == Color.ToArgb() &&
                   tb.Direction == Direction; //compare properties
        }
        public abstract void Act(); //Act method to be overrided
    }
    
}

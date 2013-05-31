#region

using System.Collections.Generic;

#endregion

namespace Netron
{
    public class Grid
    {
        public Grid(int width, int height) //Constructor
        {
            Map = new TronBase[height,width]; //Creates a new array
        }

        public TronBase[,] Map //2D array of TronBases
        { get; private set; }

        public int Width //Width of the map
        {
            get { return Map.GetLength(1); }
        }

        public int Height //Height of the map
        {
            get { return Map.GetLength(0); }
        }

        public float CellHeight { get; set; } //Cell height and width  
        public float CellWidth { get; set; }

        public void Set(TronBase tb, int x, int y) //Sets a location to a TronBase
        {
            if (Get(x, y) != null) //If there is already an object there
                Get(x, y).IsInGrid = false; //Update
            Map[y, x] = tb; //Put the object
        }

        public TronBase Remove(int x, int y) //Removes something from the array
        {
            var tb = Get(x, y); //Get the object there
            if (tb != null) //If there is already an object there
                tb.IsInGrid = false; //Update
            Set(null, x, y); //Set to null
            return tb; //return the object
        }

        public void Move(int x, int y, int newx, int newy) //Move an object from one coordinate to another
        {
            Set(Remove(x, y), newx, newy); //Set and remove the object
        }

        public void Move(TronBase tb, int newx, int newy) //Like the other move method
        {
            Move(tb.XPos, tb.YPos, newx, newy); //Call Move with coordinates
        }

        public TronBase Get(int x, int y) //Gets the tronbase at the coordinate
        {
            return Map[y, x];
        }

        public TronBase Get(int[] coords) //like the other get method
        {
            return Get(coords[0], coords[1]); //call Get with coords
        }

        public void Set(TronBase tb, int[] coords) //like the other set method
        {
            Set(tb, coords[0], coords[1]); //call set with coordinates
        }

        public void ActAll() //Make all the tronbases act
        {
            foreach (var tb in Map) //Foreach loop
            {
                if (tb != null)
                    tb.Act(); //Act if not null
            }
        }

        public List<TronBase> GetAllNeighboring(int xCoord, int yCoord) //Get all tronbases neighboring a location
        {
            var list = new List<TronBase>(); //Create list
            for (var x = xCoord - 1; x < xCoord + 1; x++) //go through a 3x3 grid around the coordinate
            {
                for (var y = yCoord - 1; y < yCoord + 1; y++)
                {
                    if (IsValidLocation(x, y)) //if location is valid
                    {
                        var tb = Get(x, y); //get the object
                        if (y != yCoord || x != xCoord && tb != null)
                            list.Add(tb); //Add the object
                    }
                }
            }
            return list; //return the list
        }

        public bool IsValidLocation(int x, int y) //if the coordinate is valid
        {
            return (x >= 0) && (y >= 0) && (x < Width) && (y < Height);
        }

        public bool IsValidLocation(int[] coords) //Overloaded method with array
        {
            return IsValidLocation(coords[0], coords[1]);
        }

        public void Exec(TronInstruction ti, int arg1, int arg2, TronBase tb) //Executes an instruction
        {
            if (!tb.IsInGrid) tb.PutSelfInGrid(this, arg1, arg2); //Put the object in the grid if it isn't already
            var p = tb as Player; //safely typecast
            switch (ti)
            {
                case TronInstruction.AddToGrid:
                    tb.PutSelfInGrid(this, arg1, arg2); //put it in the grid
                    return;
                case TronInstruction.MoveEntity:
                    tb.MoveTo(arg1, arg2); //move the object
                    return;
                case TronInstruction.RemoveFromGrid:
                    tb.RemoveFromGrid(); //remove the object
                    return;
            }
            if (p == null) return; //exit if it wasn't a Player
            switch (ti) //Turn based on the instruction and don't broadcast
            {
                case TronInstruction.TurnLeft:
                    p.AcceptUserInput(TronBase.DirectionType.West, false);
                    break;
                case TronInstruction.TurnRight:
                    p.AcceptUserInput(TronBase.DirectionType.East, false);
                    break;
                case TronInstruction.TurnDown:
                    p.AcceptUserInput(TronBase.DirectionType.South, false);
                    break;
                case TronInstruction.TurnUp:
                    p.AcceptUserInput(TronBase.DirectionType.North, false);
                    break;
                case TronInstruction.Kill:
                    p.Dead = true;
                    break;
            }
        }

        public void Clear()
        {
            Map = new TronBase[Height,Width]; //Creates a new array
        }
    }
}
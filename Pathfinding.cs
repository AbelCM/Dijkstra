using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts;
using System.Linq;

    
    //This class needs mainly two external additional classes: a GridGenerator, which handles the grid
    //we're going to use for the pathfinding and a Square class, which is simply a handy way to store data
    //about each square in the grid. We're going to need a getGrid(int, int) method which will tell us in what state
    //the specified position is in, a getSquare(Square) method which will tell us using a dictionary if the square is
    //occupied by another unit or not and a setSquare(Square, bool) to change it.
    //The Square class works like new Square(int positionX, int positionY, Square previousSquare, float cost)
public static class Pathfinding : MonoBehaviour {

    enum SquareState { Flat, Ramp, Entrance, Blocked }
    private float straightCost, diagonalCost;
    //This method will find all the valid movements in a grid for a given unit with a given amount
    //of movement points. It also works with multiple floors and ramps, but that part is still experimental.
    public static List<Square> findValidMovements(GameObject unit)
    {
        straightCost = 1;
        diagonalCost = 1.5f;
        //Destroys the already existing lights marking the spots where you can move
        if (GameObject.FindGameObjectsWithTag("wayLight") != null)
        {
            GameObject[] gameObjectArray = GameObject.FindGameObjectsWithTag("wayLight");
            foreach (GameObject go in gameObjectArray)
            {
                Destroy(go);
            }
        }

        UnitController unitController = unit.GetComponent<UnitController>();
        GridGenerator gridGenerator;

        //If the unit is standing on a plane, it'll fetch it to get the grid it is walking on.
        //The unit should always be standing on a plane, but I'll get a default plane in case
        //it doesn't for debugging purposes.
        if (unit.GetComponent<UnitController>().standingOn == null)
        {
            gridGenerator = GameObject.Find("Plane").GetComponent<GridGenerator>();
        }
        else
        {
            gridGenerator = unit.GetComponent<UnitController>().standingOn.GetComponent<GridGenerator>();
        }

        //Two new lists to store the valid, already visited and the pending of check squares
        List<Square> visited = new List<Square>();
        List<Square> pending = new List<Square>();

        //Creates the square we're standing on by getting the unit coordinates
        int actualx = (int)unit.transform.position.x - (int)gridGenerator.offset().x;
        int actualy = Mathf.Abs((int)unit.transform.position.z) - (int)gridGenerator.offset().y;
        Square actualSquare = new Square(actualx,actualy,null,0);

        //Check for the east square, first check is always to see if it isn't out of range of the grid
        //by being bigger than the size or smaller than 0
        if(actualx + 1 < gridGenerator.width())
            //The next conditional is always to check if the square is blocked or occupied
            if (gridGenerator.getGrid(actualx + 1, actualy) != SquareState.Blocked && !gridGenerator.getSquare(new Square(actualx + 1, actualy)))
            {
                //This checks if the square we're in is a ramp or a ramp entrance and we're trying to move into a ramp or ramp entrance.
                //It limits the movement so you just can move from ramp to ramp or ramp entrance or from ramp entrance to ramp or wherever else
                //otherwise you would just fly onto the ramp from a side instead of correctly walking through the entrance
                if ((gridGenerator.getGrid(actualx + 1, actualy) == SquareState.Ramp || gridGenerator.getGrid(actualx + 1, actualy) == SquareState.Entrance) && (gridGenerator.getGrid(actualx, actualy) == SquareState.Entrance || gridGenerator.getGrid(actualx, actualy) == SquareState.Ramp))
                    //Add the square we're in as a pending of check, possible movement
                    pending.Add(new Square(actualx + 1, actualy, actualSquare, straightCost));
                //If we're not on a ramp, or trying to get into one
                if(gridGenerator.getGrid(actualx + 1, actualy) != SquareState.Ramp && gridGenerator.getGrid(actualx, actualy) != SquareState.Ramp)
                    pending.Add(new Square(actualx + 1, actualy, actualSquare, straightCost));
            }
        //West square
        if (actualx - 1 >= 0)
            if (gridGenerator.getGrid(actualx - 1, actualy) != SquareState.Blocked && !gridGenerator.getSquare(new Square(actualx - 1, actualy)))
            {
                if((gridGenerator.getGrid(actualx - 1, actualy) == SquareState.Ramp || gridGenerator.getGrid(actualx - 1, actualy) == SquareState.Entrance) && (gridGenerator.getGrid(actualx, actualy) == SquareState.Entrance || gridGenerator.getGrid(actualx, actualy) == SquareState.Ramp))
                    pending.Add(new Square(actualx - 1, actualy, actualSquare, straightCost));
                if (gridGenerator.getGrid(actualx - 1, actualy) != SquareState.Ramp && gridGenerator.getGrid(actualx, actualy) != SquareState.Ramp)
                    pending.Add(new Square(actualx - 1, actualy, actualSquare, straightCost));
            }
        //South square
        if (actualy - 1 >= 0)
            if (gridGenerator.getGrid(actualx, actualy - 1) != SquareState.Blocked && !gridGenerator.getSquare(new Square(actualx, actualy - 1)))
            {
                if ((gridGenerator.getGrid(actualx, actualy - 1) == SquareState.Ramp || gridGenerator.getGrid(actualx, actualy-1) == SquareState.Entrance) && (gridGenerator.getGrid(actualx, actualy) == SquareState.Entrance || gridGenerator.getGrid(actualx, actualy) == SquareState.Ramp))
                    pending.Add(new Square(actualx, actualy - 1, actualSquare, straightCost));
                if (gridGenerator.getGrid(actualx, actualy - 1) != SquareState.Ramp && gridGenerator.getGrid(actualx, actualy) != SquareState.Ramp)
                    pending.Add(new Square(actualx, actualy - 1, actualSquare, straightCost));
            }
        //North square
        if (actualy + 1 < gridGenerator.height())
            if (gridGenerator.getGrid(actualx, actualy + 1) != SquareState.Blocked && !gridGenerator.getSquare(new Square(actualx, actualy + 1)))
            {
                if ((gridGenerator.getGrid(actualx, actualy + 1) == SquareState.Ramp || gridGenerator.getGrid(actualx, actualy + 1) == SquareState.Entrance) && (gridGenerator.getGrid(actualx, actualy) == SquareState.Entrance || gridGenerator.getGrid(actualx, actualy) == SquareState.Ramp))
                    pending.Add(new Square(actualx, actualy + 1, actualSquare, straightCost));
                if (gridGenerator.getGrid(actualx, actualy + 1) != SquareState.Ramp && gridGenerator.getGrid(actualx, actualy) != SquareState.Ramp)
                    pending.Add(new Square(actualx, actualy + 1, actualSquare, straightCost));
            }
        //Diagonal northeast
        if (actualy + 1 < gridGenerator.height() && actualx + 1 < gridGenerator.width())
            if (gridGenerator.getGrid(actualx + 1, actualy + 1) != SquareState.Blocked && !gridGenerator.getSquare(new Square(actualx + 1, actualy + 1)))
            {
                if ((gridGenerator.getGrid(actualx + 1, actualy + 1) == SquareState.Ramp || gridGenerator.getGrid(actualx + 1, actualy + 1) == SquareState.Entrance) && (gridGenerator.getGrid(actualx, actualy) == SquareState.Entrance || gridGenerator.getGrid(actualx, actualy) == SquareState.Ramp))
                    pending.Add(new Square(actualx + 1, actualy + 1, actualSquare, diagonalCost));
                if (gridGenerator.getGrid(actualx + 1, actualy + 1) != SquareState.Ramp && gridGenerator.getGrid(actualx, actualy) != SquareState.Ramp)
                    pending.Add(new Square(actualx + 1, actualy + 1, actualSquare, diagonalCost));
            }
        //Diagonal southwest
        if (actualy - 1 >= 0 && actualx - 1 >= 0)
            if (gridGenerator.getGrid(actualx - 1, actualy - 1) != SquareState.Blocked && !gridGenerator.getSquare(new Square(actualx - 1, actualy - 1)))
            {
                if ((gridGenerator.getGrid(actualx - 1, actualy - 1) == SquareState.Ramp || gridGenerator.getGrid(actualx - 1, actualy - 1) == SquareState.Entrance) && (gridGenerator.getGrid(actualx, actualy) == SquareState.Entrance || gridGenerator.getGrid(actualx, actualy) == SquareState.Ramp))
                    pending.Add(new Square(actualx - 1, actualy - 1, actualSquare, diagonalCost));
                if (gridGenerator.getGrid(actualx - 1, actualy - 1) != SquareState.Ramp && gridGenerator.getGrid(actualx, actualy) != SquareState.Ramp)
                    pending.Add(new Square(actualx - 1, actualy - 1, actualSquare, diagonalCost));
            }
        //Diagonal northwest
        if (actualy + 1 < gridGenerator.height() && actualx - 1 >= 0)
            if (gridGenerator.getGrid(actualx - 1, actualy + 1) != SquareState.Blocked && !gridGenerator.getSquare(new Square(actualx - 1, actualy + 1)))
            {
                if ((gridGenerator.getGrid(actualx - 1, actualy + 1) == SquareState.Ramp || gridGenerator.getGrid(actualx - 1, actualy + 1) == SquareState.Entrance) && (gridGenerator.getGrid(actualx, actualy) == SquareState.Entrance || gridGenerator.getGrid(actualx, actualy) == SquareState.Ramp))
                    pending.Add(new Square(actualx - 1, actualy + 1, actualSquare, diagonalCost));
                if (gridGenerator.getGrid(actualx - 1, actualy + 1) != SquareState.Ramp && gridGenerator.getGrid(actualx, actualy) != SquareState.Ramp)
                    pending.Add(new Square(actualx - 1, actualy + 1, actualSquare, diagonalCost));
            }
        //Diagonal southeast
        if (actualy - 1 >= 0  && actualx + 1 < gridGenerator.width())
            if (gridGenerator.getGrid(actualx + 1, actualy - 1) != SquareState.Blocked && !gridGenerator.getSquare(new Square(actualx + 1, actualy - 1)))
            {
                if ((gridGenerator.getGrid(actualx + 1, actualy - 1) == SquareState.Ramp || gridGenerator.getGrid(actualx + 1, actualy - 1) == SquareState.Entrance) && (gridGenerator.getGrid(actualx, actualy) == SquareState.Entrance || gridGenerator.getGrid(actualx, actualy) == SquareState.Ramp))
                    pending.Add(new Square(actualx + 1, actualy - 1, actualSquare, diagonalCost));
                if (gridGenerator.getGrid(actualx + 1, actualy - 1) != SquareState.Ramp && gridGenerator.getGrid(actualx, actualy) != SquareState.Ramp)
                    pending.Add(new Square(actualx + 1, actualy - 1, actualSquare, diagonalCost));
            }
        //Adds the square we started at to the already visited ones list
        visited.Add(actualSquare);

        //Checks every square in the possible movements list
        while(pending.Count>0)
        {
            //Pick the first one, then check for the less expensive one in the list and pick it,
            //this will allow us to find the cheapest/fastest route to the point in question.
            //This is kinda pointless in this implementation since we're not looking for a specific path
            //to a specific point, but rather all the possible movements within a range
            Square square = pending[0];
            foreach (Square sq in pending)
            {
                if (sq.cost < square.cost)
                {
                    square = sq;
                }
            }
            //East square
            if (square.x + 1 < gridGenerator.width())
                //If it is not blocked, already visited, already pending or occupied
                if (gridGenerator.getGrid(square.x + 1, square.y) != SquareState.Blocked && !visited.Exists(x => x.x == square.x + 1 && x.y == square.y) && !pending.Exists(x => x.x == square.x + 1 && x.y == square.y) && !gridGenerator.getSquare(new Square(square.x + 1, square.y)))
                {
                    //If the cost of moving to that square doesn't surpass the speed of the unit in question
                    if (unitController.Speed >= square.cost + straightCost)
                    {
                        //Same ramp checks as above
                        if ((gridGenerator.getGrid(square.x + 1, square.y) == SquareState.Ramp || gridGenerator.getGrid(square.x + 1, square.y) == SquareState.Entrance) && (gridGenerator.getGrid(square.x, square.y) == SquareState.Entrance || gridGenerator.getGrid(square.x, (square.y)) == SquareState.Ramp))
                            pending.Add(new Square(square.x + 1, square.y, square, square.cost + straightCost));
                        if (gridGenerator.getGrid(square.x + 1, square.y) != SquareState.Ramp && gridGenerator.getGrid(square.x, square.y) != SquareState.Ramp)
                            pending.Add(new Square(square.x + 1, square.y, square, square.cost + straightCost));
                    }     
                }
            //West Square
            if (square.x - 1 >= 0)
                if (gridGenerator.getGrid(square.x - 1, square.y) != SquareState.Blocked && !visited.Exists(x => x.x == square.x - 1 && x.y == square.y) && !pending.Exists(x => x.x == square.x - 1 && x.y == square.y) && !gridGenerator.getSquare(new Square(square.x - 1, square.y)))
                {
                    if (unitController.Speed >= square.cost + straightCost)
                    {
                        if ((gridGenerator.getGrid(square.x - 1, square.y) == SquareState.Ramp || gridGenerator.getGrid(square.x - 1, square.y) == SquareState.Entrance) && (gridGenerator.getGrid(square.x, square.y) == SquareState.Entrance || gridGenerator.getGrid(square.x, (square.y)) == SquareState.Ramp))
                            pending.Add(new Square(square.x - 1, square.y, square, square.cost + straightCost));
                        if (gridGenerator.getGrid(square.x - 1, square.y) != SquareState.Ramp && gridGenerator.getGrid(square.x, square.y) != SquareState.Ramp)
                            pending.Add(new Square(square.x - 1, square.y, square, square.cost + straightCost));
                    }   
                }
            //South square
            if (square.y - 1 >= 0)
                if (gridGenerator.getGrid(square.x, (square.y - 1)) != SquareState.Blocked && !visited.Exists(x => x.x == square.x && x.y == square.y - 1) && !pending.Exists(x => x.x == square.x && x.y == square.y - 1) && !gridGenerator.getSquare(new Square(square.x, square.y - 1)))
                {
                    if (unitController.Speed >= square.cost + straightCost)
                    {
                        if ((gridGenerator.getGrid(square.x, square.y - 1) == SquareState.Ramp || gridGenerator.getGrid(square.x, square.y - 1) == SquareState.Entrance) && (gridGenerator.getGrid(square.x, (square.y)) == SquareState.Entrance || gridGenerator.getGrid(square.x, (square.y)) == SquareState.Ramp))
                        {
                            RaycastHit hit;
                            Physics.Raycast(new Vector3(square.x, unitController.standingOn.transform.position.y, -square.y), Vector3.up, out hit);
                            print("Rampa " + hit.transform.gameObject.name);
                            pending.Add(new Square(square.x, square.y - 1, square, square.cost + straightCost));
                        }
                        if (gridGenerator.getGrid(square.x, (square.y - 1)) != SquareState.Ramp && gridGenerator.getGrid(square.x, square.y) != SquareState.Ramp)
                            pending.Add(new Square(square.x, square.y - 1, square, square.cost + straightCost));
                    } 
                }
            //North square
            if (square.y + 1 < gridGenerator.height())
                if (gridGenerator.getGrid(square.x, (square.y + 1)) != SquareState.Blocked && !visited.Exists(x => x.x == square.x && x.y == square.y + 1) && !pending.Exists(x => x.x == square.x && x.y == square.y + 1) && !gridGenerator.getSquare(new Square(square.x, square.y + 1)))
                {
                    if (unitController.Speed >= square.cost + straightCost)
                    {
                        if ((gridGenerator.getGrid(square.x, square.y + 1) == SquareState.Ramp || gridGenerator.getGrid(square.x, square.y + 1) == SquareState.Entrance) && (gridGenerator.getGrid(square.x, (square.y)) == SquareState.Entrance || gridGenerator.getGrid(square.x, (square.y)) == SquareState.Ramp))
                            pending.Add(new Square(square.x, square.y + 1, square, square.cost + straightCost));
                        if (gridGenerator.getGrid(square.x, square.y + 1) != SquareState.Ramp && gridGenerator.getGrid(square.x, square.y) != SquareState.Ramp)
                            pending.Add(new Square(square.x, square.y + 1, square, square.cost + straightCost));
                    }
                }
            //Diagonal northeast square
            if (square.y + 1 < gridGenerator.height() && square.x + 1 < gridGenerator.width())
                if (gridGenerator.getGrid(square.x + 1, (square.y + 1)) != SquareState.Blocked && !visited.Exists(x => x.x == square.x + 1 && x.y == square.y + 1) && !pending.Exists(x => x.x == square.x + 1 && x.y == square.y + 1) && !gridGenerator.getSquare(new Square(square.x + 1, square.y + 1)))
                {
                    if (unitController.Speed >= square.cost + diagonalCost)
                    {
                        if ((gridGenerator.getGrid(square.x + 1, square.y + 1) == SquareState.Ramp || gridGenerator.getGrid(square.x + 1, square.y + 1) == SquareState.Entrance) && (gridGenerator.getGrid(square.x, (square.y)) == SquareState.Entrance || gridGenerator.getGrid(square.x, (square.y)) == SquareState.Ramp))
                            pending.Add(new Square(square.x + 1, square.y + 1, square, square.cost + diagonalCost));
                        if (gridGenerator.getGrid(square.x + 1, square.y + 1) != SquareState.Ramp && gridGenerator.getGrid(square.x, square.y) != SquareState.Ramp)
                            pending.Add(new Square(square.x + 1, square.y + 1, square, square.cost + diagonalCost));
                    }  
                }
            //Diagonal southwest square
            if (square.y - 1 >= 0 && square.x - 1 >= 0)
                if (gridGenerator.getGrid(square.x - 1, (square.y - 1)) != SquareState.Blocked && !visited.Exists(x => x.x == square.x - 1 && x.y == square.y - 1) && !pending.Exists(x => x.x == square.x - 1 && x.y == square.y - 1) && !gridGenerator.getSquare(new Square(square.x - 1, square.y - 1)))
                {
                    if (unitController.Speed >= square.cost + diagonalCost)
                    {
                        if ((gridGenerator.getGrid(square.x - 1, square.y - 1) == SquareState.Ramp || gridGenerator.getGrid(square.x - 1, square.y - 1) == SquareState.Entrance) && (gridGenerator.getGrid(square.x, (square.y)) == SquareState.Entrance || gridGenerator.getGrid(square.x, (square.y)) == SquareState.Ramp))
                            pending.Add(new Square(square.x - 1, square.y - 1, square, square.cost + diagonalCost));
                        if (gridGenerator.getGrid(square.x - 1, square.y - 1) != SquareState.Ramp && gridGenerator.getGrid(square.x, square.y) != SquareState.Ramp)
                            pending.Add(new Square(square.x - 1, square.y - 1, square, square.cost + diagonalCost));
                    }
                }
            //Diagonal northwest square
            if (square.y + 1 < gridGenerator.height() && square.x - 1 >= 0)
                if (gridGenerator.getGrid(square.x - 1, (square.y + 1)) != SquareState.Blocked && !visited.Exists(x => x.x == square.x - 1 && x.y == square.y + 1) && !pending.Exists(x => x.x == square.x - 1 && x.y == square.y + 1) && !gridGenerator.getSquare(new Square(square.x - 1, square.y + 1)))
                {
                    if (unitController.Speed >= square.cost + diagonalCost)
                    {
                        if ((gridGenerator.getGrid(square.x - 1, square.y + 1) == SquareState.Ramp || gridGenerator.getGrid(square.x - 1, square.y + 1) == SquareState.Entrance) && (gridGenerator.getGrid(square.x, (square.y)) == SquareState.Entrance || gridGenerator.getGrid(square.x, (square.y)) == SquareState.Ramp))
                            pending.Add(new Square(square.x - 1, square.y + 1, square, square.cost + diagonalCost));
                        if (gridGenerator.getGrid(square.x - 1, square.y + 1) != SquareState.Ramp && gridGenerator.getGrid(square.x, square.y) != SquareState.Ramp)
                            pending.Add(new Square(square.x - 1, square.y + 1, square, square.cost + diagonalCost));
                    } 
                }
            //Diagonal southeast square
            if (square.y - 1 >= 0 && square.x + 1 < gridGenerator.width())
                if (gridGenerator.getGrid(square.x + 1, (square.y - 1)) != SquareState.Blocked && !visited.Exists(x => x.x == square.x + 1 && x.y == square.y - 1) && !pending.Exists(x => x.x == square.x + 1 && x.y == square.y - 1) && !gridGenerator.getSquare(new Square(square.x + 1, square.y - 1)))
                {
                    if (unitController.Speed >= square.cost + diagonalCost)
                    {
                        if ((gridGenerator.getGrid(square.x + 1, square.y - 1) == SquareState.Ramp || gridGenerator.getGrid(square.x + 1, square.y - 1) == SquareState.Entrance) && (gridGenerator.getGrid(square.x, (square.y)) == SquareState.Entrance || gridGenerator.getGrid(square.x, (square.y)) == SquareState.Ramp))
                            pending.Add(new Square(square.x + 1, square.y - 1, square, square.cost + diagonalCost));
                        if (gridGenerator.getGrid(square.x + 1, square.y - 1) != SquareState.Ramp && gridGenerator.getGrid(square.x, square.y) != SquareState.Ramp)
                            pending.Add(new Square(square.x + 1, square.y - 1, square, square.cost + diagonalCost));
                    }  
                }
            //Add the square to the valid, visited list and remove it from the pending one
            visited.Add(square);
            pending.Remove(square);
        }
        //Put a light in each valid, visited square so we know which ones are ingame
        foreach (Square square in visited)
        {
            GameObject marker = Instantiate(Resources.Load("AreaLight")) as GameObject;
            marker.transform.position = new Vector3(square.x + 0.5f, 0, -1 * square.y - 0.5f);
        }
        //Return the valid, visited list in case we need it for movement
        return visited;
    }
}

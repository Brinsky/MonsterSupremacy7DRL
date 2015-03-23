using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using UnityEngine;
using Tiles;

using Random = UnityEngine.Random;

public class Level
{
    // public static int seed = 0; // Eventually use this for debugging if needed
    public static System.Random rand = new System.Random();

    private int floor;
    private int numCols;
    private int numRows;
    public Point Size { get { return new Point(numCols, numRows); } }
    Point startCell;
    public Point Upstairs { get; private set; }
    public Point Downstairs { get; private set; }

    // Level content data
    Tile[,] map;
    Organism[,] organisms;
    Stack<Item>[,] itemsOnGround;

    // Helpful transforms to parent other GameObjects to
    Transform anchor; 
    Transform organismAnchor;
    Transform itemAnchor;
    Transform terrainAnchor;

    public List<Organism> turnOrder = new List<Organism>();

    public Level(int floor = 25, int numCols = 80, int numRows = 30)
    {
        // Set up anchors to help separate GameObjects in the editor
        anchor = new GameObject("Level").transform;
        organismAnchor = new GameObject("Organisms").transform;
        organismAnchor.SetParent(anchor);
        itemAnchor = new GameObject("Items").transform;
        itemAnchor.SetParent(anchor);
        terrainAnchor = new GameObject("Terrain").transform;
        terrainAnchor.SetParent(anchor);

        this.floor = floor;
        this.numCols = numCols;
        this.numRows = numRows;

        map = new Tile[numCols, numRows];
        organisms = new Organism[numCols, numRows];
        
        itemsOnGround = new Stack<Item>[numCols, numRows];

        // Place the entry point randomly
        startCell = new Point(Random.Range(1, numCols - 1), Random.Range(1, numRows - 1));

        GenerateLevel();
        BuildLevel();

        Player player = Place<Player>(startCell.x, startCell.y);
        player.power = new BasicMelee(1f);
    }

    // Create/instantiate actual GameObjects based on the level
    private void BuildLevel()
    {
        List<Point> openTiles = new List<Point>();

        for (int x = 0; x < numCols; ++x)
        {
            for (int y = 0; y < numRows; ++y)
            {
                PlaceTile(GetTile(x, y), x, y);

                // Add open tiles to a list (used for monster placement)
                if (GetTile(x, y).Walkable())
                    if (x != Downstairs.x && y != Downstairs.y)
                        if (x != Upstairs.x && y != Upstairs.y)
                            openTiles.Add(new Point(x, y));

                // Initialize each stack within the stack array
                itemsOnGround[x, y] = new Stack<Item>();
            }
        }

        // Number of monsters spawned is roughly 1/100 of the total open squares
        int monsterCount = (int)(openTiles.Count * (Random.value / 5 + 0.9f) / 90f);

        SpawnNPCs(openTiles, monsterCount);
    }

    private void SpawnNPCs(List<Point> openTiles, int count)
    {
        for(int i = 0; i < count; i++)
        {
            // Choose a random tile
            Point t = openTiles[rand.Next(openTiles.Count)];

            // Place a monster
            EnemyMonster monster = Place<EnemyMonster>(t.x, t.y);
            monster.stats = new Stats(monster, (GameManager.Manager.FLOOR_START - floor) + 1);

            switch (Random.Range(0, 4))
            {
                case 0:
                    monster.power = new Poison(0.5f, 5);
                    break;

                case 1:
                    monster.power = new BasicRanged(1, 3);
                    break;

                case 2:
                    monster.power = new BasicMelee(1);
                    break;

                case 3:
                    monster.power = new BounceBack(1);
                    break;
            }

            openTiles.Remove(t);
        }


    }


    #region Level generation
    private void GenerateLevel()
    {
        int roomType = 1; // rand.Next(3);

        switch (roomType)
        {
            case 0:
                #region Attempt to make randomly sized rooms and corridors to connect them.
                SetAllTiles(Tile.Wall);
                GenerateIrregularLevel(new Point(0, 0), new Point(numCols - 1, numRows - 1));
                break;
                #endregion

            case 1:
                #region Maze
                SetAllTiles(Tile.Ground);
                SetEdgeTiles(Tile.Wall);
                GenerateMazeLevel(new Point(0, 0), new Point(numCols - 1, numRows - 1));
                break;
                #endregion

            case 2: // This is untested, Best to avoid it for now.
                #region Half maze, half regular
                SetAllTiles(Tile.Ground, new Point(0, 0), new Point(numCols / 2, numRows - 1));
                SetEdgeTiles(Tile.Ground, new Point(0, 0), new Point(numCols / 2, numRows - 1));
                GenerateMazeLevel(new Point(0, 0), new Point(numCols / 2 - 1, numRows - 1));
                GenerateIrregularLevel(new Point(numCols / 2 + 1, 0), new Point(numCols - 1, numRows - 1));

                // Pick a point in the dividing wall and punch through it to provide access
                int dividingCol = rand.Next(1, numRows);
                map[numCols / 2, dividingCol] = Tile.Ground;
                for (int col = numCols / 2 - 1; !map[col, dividingCol].Walkable() && col >= 0; col--)
                    map[col, dividingCol] = Tile.Ground; // Be careful - this may write over tiles that it shouldn't..
                for (int col = numCols / 2 + 1; !map[col, dividingCol].Walkable() && col < numCols; col++)
                    map[col, dividingCol] = Tile.Ground; // Be careful - this may write over tiles that it shouldn't..
                break;
                #endregion

            // Add cases for other levels. Perhaps specific patterns, or preset levels
        }

        // Randomly choose an upstairs tile. Eventually change this so it puts it in a more reasonable spot (further from the entrance)
        int x = 0;
        int y = 0;
        while (!map[x, y].Walkable() || (x == startCell.x && y == startCell.y) || startCell.Distance(new Point(x,y)) < Math.Min(numCols, numRows) / 2 )
        {
            x = rand.Next(0, numCols);
            y = rand.Next(0, numRows);
        }
        map[x, y] = Tile.Upstairs;
        map[startCell.x, startCell.y] = (floor == GameManager.Manager.FLOOR_START) ? Tile.Ground : Tile.Downstairs;
        Upstairs = new Point(x, y);
        Downstairs = new Point(startCell.x, startCell.y);
    }
   
    private void GenerateMazeLevel(Point topLeft, Point bottomRight)
    {
        // http://en.wikipedia.org/wiki/Maze_generation_algorithm#Recursive_division_method

        // Base Case
        int squares = (bottomRight.x - topLeft.x - 1) * (bottomRight.y - topLeft.y - 1);
        if (squares <= 9)
            return;

        // Divide the map using one vertical and one horizontal line. Make each have one space in them on each side of the intersection
        Point intersect = new Point(rand.Next(Math.Min(topLeft.x + 2, bottomRight.x - 1), bottomRight.x), rand.Next(Math.Min(topLeft.y + 2, bottomRight.y - 1), bottomRight.y));
        Point[] openings = new Point[4];
        openings[0] = new Point(intersect.x, rand.Next(topLeft.y + 1, intersect.y));
        openings[1] = new Point(intersect.x, rand.Next(intersect.y + 1, bottomRight.y));
        openings[2] = new Point(rand.Next(topLeft.x + 1, intersect.x), intersect.y);
        openings[3] = new Point(rand.Next(intersect.x + 1, bottomRight.x), intersect.y);

        for (int y = topLeft.y; y < bottomRight.y; y++)
            if (y != openings[0].y && y != openings[1].y)
                map[intersect.x, y] = Tile.Wall;

        for (int x = topLeft.x; x < bottomRight.x; x++)
            if (x != openings[2].x && x != openings[3].x)
                map[x, intersect.y] = Tile.Wall;

        // Recursive calls on each of the new four rooms
        GenerateMazeLevel(new Point(topLeft.x + 1, topLeft.y + 1), new Point(intersect.x - 1, intersect.y - 1)); // top left
        GenerateMazeLevel(new Point(topLeft.x + 1, intersect.y + 1), new Point(intersect.x - 1, bottomRight.y - 1)); // bottom left
        GenerateMazeLevel(new Point(intersect.x + 1, topLeft.y + 1), new Point(bottomRight.x - 1, intersect.y + 1)); // top right
        GenerateMazeLevel(new Point(intersect.x + 1, intersect.y + 1), new Point(bottomRight.x - 1, bottomRight.y - 1)); // bottom right

        Console.WriteLine(this.ToString());
        Console.WriteLine("------------------");
    }

    private void GenerateIrregularLevel(Point topLeft, Point bottomRight)
    {
        // Create rooms
        int failCount = 0;
        int maxFailCount = rand.Next(100, 500); // The higher this is, the more open space there will be in the dungeon

        while (failCount < maxFailCount)
        {
            if (!TryAddRandomRoom(topLeft, bottomRight))
                failCount++;
        }

        // Make sure all rooms are connected by flood fill.

        int[,] filledTiles = new int[numCols, numRows];
        for (int i = 0; i < numCols; i++)
            for (int j = 0; j < numRows; j++)
                if (!map[i, j].Walkable())
                    filledTiles[i, j] = 2;
        filledTiles[startCell.x, startCell.y] = 1;

        bool[,] visited = new bool[numCols, numRows];


        //Console.WriteLine(ToString());
        //Console.WriteLine("-----");
        floodFill(filledTiles, visited, startCell);

        // Now that the int array is "flooded", all walls have value 2. All tiles connected in some way to the starting stairs have value 1. Others have value 0.
        // We want to find any unconnected tile and connect it. Then flood that new area and repeat.
        while (!allConnected(filledTiles, topLeft, bottomRight))
        {
            // Find a random unconnected tile
            List<Point> unconnectedTiles = new List<Point>();
            for (int i = topLeft.x; i < bottomRight.x; i++)
                for (int j = topLeft.y; j < bottomRight.y; j++)
                    if (filledTiles[i, j] == 0)
                        unconnectedTiles.Add(new Point(i, j));

            if (unconnectedTiles.Count == 0) // This should never happen
                break;

            Point p = unconnectedTiles[rand.Next(unconnectedTiles.Count)];

            #region Find the closest connected point to p by looking in a spiral outwards
            // Spirals work by: 1 up, 1 right, 2 down, 2 left, 3 up, 3 right, etc... it's two of the same distance one after another
            Point q = new Point(p.x, p.y);
            int dir = 0;
            int step = 1; // How many steps to take in one direction
            int baseStep = 1;
            while (true)
            {
                // Take a step in the set direction
                switch (dir)
                {
                    case 0:
                        q.y--;
                        break;
                    case 1:
                        q.x++;
                        break;
                    case 2:
                        q.y++;
                        break;
                    case 3:
                        q.x--;
                        break;
                }
                step--;

                if (step == 0)
                {
                    if (dir == 1 || dir == 3)
                        baseStep++;
                    step = baseStep;
                    dir = (dir + 1) % 4;
                }

                // If the point is in bounds
                if ((q.x >= topLeft.x && q.x <= bottomRight.x) && (q.y >= topLeft.y && q.y <= bottomRight.y))
                    if (filledTiles[q.x, q.y] == 1) // If it's a connected tile
                        break;
            }
            #endregion

            //Console.WriteLine("Connecting Points:" + p + " " + q);
            #region Next connect points p and q by making ground tiles between them.
            if (p.x < q.x)
            {
                for (int i = p.x + 1; i <= q.x; i++) // go across from p
                    if (!map[i, p.y].Walkable())
                    {
                        map[i, p.y] = Tile.Ground;
                        visited[i, p.y] = false; // set visited to false so the new area can be floodFilled
                    }

                // Then go up/down to reach q
                if (p.y < q.y)
                {
                    for (int j = q.y - 1; j > p.y; j--)
                        if (!map[q.x, j].Walkable())
                        {
                            map[q.x, j] = Tile.Ground;
                            visited[q.x, j] = false;
                        }
                }
                else
                {
                    for (int j = q.y + 1; j < p.y; j++)
                        if (!map[q.x, j].Walkable())
                        {
                            map[q.x, j] = Tile.Ground;
                            visited[q.x, j] = false;
                        }
                }

            }
            else
            {
                for (int i = q.x + 1; i <= p.x; i++) // go across from q if it's further to the left
                    if (!map[i, q.y].Walkable())
                    {
                        map[i, q.y] = Tile.Ground;
                        visited[i, q.y] = false;
                    }

                if (p.y < q.y)
                {
                    for (int j = p.y + 1; j < q.y; j++)
                        if (!map[p.x, j].Walkable())
                        {
                            map[p.x, j] = Tile.Ground;
                            visited[p.x, j] = false;
                        }
                }
                else
                {
                    for (int j = p.y - 1; j >= q.y; j--)
                        if (!map[p.x, j].Walkable())
                        {
                            map[p.x, j] = Tile.Ground;
                            visited[p.x, j] = false;
                        }
                }
            }
            #endregion

            /*
            Console.WriteLine(ToString());
            for (int j = 0; j < numRows; j++)
            {
                for (int i = 0; i < numCols; i++)
                {
                    if (visited[i, j])
                        Console.Write('X');
                    else
                        Console.Write(map[i, j].c);
                }
                Console.WriteLine();
            }

            Console.WriteLine("----------");
            */

            visited[p.x, p.y] = false;
            visited[q.x, q.y] = false;
            floodFill(filledTiles, visited, q);
        }

        Console.WriteLine(this.ToString());
        Console.WriteLine("------------------");
    }

    private bool allConnected(int[,] filled, Point topLeft, Point bottomRight)
    {
        for (int i = topLeft.x; i < bottomRight.x; i++)
            for (int j = topLeft.y; j < bottomRight.y; j++)
                if (filled[i, j] == 0)
                    return false;
        return true;
    }

    // Helper method for making sure all walkable tiles in a level can be reached
    private void floodFill(int[,] filled, bool[,] visited, Point startPoint)
    {
        // In the int array, 0 means unchecked. 1 means accessible. When the flood is done, anything left as 0 is inaccessible.

        if (visited[startPoint.x, startPoint.y])
            return;
        visited[startPoint.x, startPoint.y] = true;

        // If the given point is not able to be walked on, then can't flood that way
        if (!map[startPoint.x, startPoint.y].Walkable())
            return;

        filled[startPoint.x, startPoint.y] = 1;

        // Flood in each direction
        if (startPoint.x + 1 < numCols) // right
            floodFill(filled, visited, new Point(startPoint.x + 1, startPoint.y));
        if (startPoint.x - 1 >= 0) // left
            floodFill(filled, visited, new Point(startPoint.x - 1, startPoint.y));
        if (startPoint.y + 1 < numRows) // down
            floodFill(filled, visited, new Point(startPoint.x, startPoint.y + 1));
        if (startPoint.y - 1 >= 0) // up
            floodFill(filled, visited, new Point(startPoint.x, startPoint.y - 1));

    }

    private bool TryAddRandomRoom(Point topLeft, Point bottomRight)
    {
        // Walls must have at least one space in between
        if (bottomRight.x - topLeft.x < 2 || bottomRight.y - topLeft.y < 2)
            return false;

        Point roomTopLeft = new Point(rand.Next(topLeft.x + 1, bottomRight.x), rand.Next(topLeft.y + 1, bottomRight.y));
        int roomWidth = Math.Max(3, rand.Next(0, (bottomRight.x - (roomTopLeft.x + 2)) / 3)); // Width and height are limited to avoid massive rooms
        int roomHeight = Math.Max(3, rand.Next(0, (bottomRight.y - (roomTopLeft.y + 2)) / 3));
        Point roomBottomRight = new Point(roomTopLeft.x + roomWidth, roomTopLeft.y + roomHeight);

        if (roomBottomRight.x > bottomRight.x)
            roomBottomRight.x = bottomRight.x;
        if (roomBottomRight.y > bottomRight.y)
            roomBottomRight.y = bottomRight.y;


        // Make sure all tiles in the new room are ground. If they aren't then it is invalid.
        for (int col = roomTopLeft.x; col < roomBottomRight.x; col++)
            for (int row = roomTopLeft.y; row < roomBottomRight.y; row++)
                if (!map[col, row].Equals(Tile.Wall))
                    return false;

        SetAllTiles(Tile.Ground, roomTopLeft, roomBottomRight);
        return true;
    }

    private bool IsAllWalkable(Point topLeft, Point bottomRight)
    {
        for (int x = topLeft.x; x < bottomRight.x; x++)
            for (int y = topLeft.y; y < bottomRight.y; y++)
                if (!map[x, y].Walkable())
                    return false;
        return true;
    }

    private void SetAllTiles(Tile tile)
    {
        SetAllTiles(tile, new Point(0, 0), new Point(numCols, numRows));
    }

    private void SetAllTiles(Tile tile, Point topLeft, Point bottomRight)
    {
        for (int x = topLeft.x; x < bottomRight.x; x++)
            for (int y = topLeft.y; y < bottomRight.y; y++)
                map[x, y] = tile;
    }

    private void SetEdgeTiles(Tile tile)
    {
        SetEdgeTiles(tile, new Point(0, 0), new Point(numCols - 1, numRows - 1));
    }

    private void SetEdgeTiles(Tile tile, Point topLeft, Point bottomRight)
    {
        for (int x = topLeft.x; x <= bottomRight.x; x++)
        {
            map[x, topLeft.y] = tile;
            map[x, bottomRight.y] = tile;
        }

        for (int y = topLeft.y; y <= bottomRight.y; y++)
        {
            map[topLeft.x, y] = tile;
            map[bottomRight.x, y] = tile;
        }
    }

    #endregion

    #region Object placement

    // Adds a GameObject with appropriate sprite (but no script) to the world
    private void PlaceTile(Tile tile, int x, int y)
    {
        if (tile == Tile.None) // Tile.None should not be represented as a GameObject
            return;

        GameObject tileInstance = new GameObject(tile.ToString());
        tileInstance.transform.position = new Vector2(x, y);
        tileInstance.transform.SetParent(terrainAnchor);

        SpriteRenderer renderer = tileInstance.AddComponent<SpriteRenderer>();
        renderer.sprite = tile.Sprite();

        // If the tile is intended to block line of sight, set up colliders
        if (tile.Blocking())
        {
            BoxCollider2D colliderA = tileInstance.AddComponent<BoxCollider2D>();
            colliderA.size = new Vector2(0.85f, 1);

            BoxCollider2D colliderB = tileInstance.AddComponent<BoxCollider2D>();
            colliderB.size = new Vector2(1, 0.85f);
        }
    }

    // My greatest acheivement - an easy way to get the main scripts of new GameObjects
    public Script Place<Script>(int x, int y) where Script : Entity
    {
        // Create an instance of an empty GameObject, name it after the script
        GameObject instance = new GameObject(typeof(Script).ToString());
        instance.transform.position = new Vector2(x, y);

        // Attach a new instance of that script to the GameObject as a component
        Script script = instance.AddComponent<Script>();

        if (script is Organism)
        {
            instance.transform.SetParent(organismAnchor);
            AddOrganism(script as Organism, x, y);

            if (script is Player)
            {
                // Make sure the player comes first in the turn order
                turnOrder.Insert(0, script as Organism);
                instance.tag = "Player";
            }
            else // Add the organism on the end of the turn list
            {
                turnOrder.Add(script as Organism);
                //script.gameObject.layer = LayerMask.NameToLayer("Attackables");
            }
        }
        else if (script is Item)
        {
            instance.transform.SetParent(itemAnchor);
            AddItem(script as Item, x, y);
        }
        else
        {
            instance.transform.SetParent(anchor);
        }

        return script; // Give back the script
    }

    // Places an item on top of the given stack
    public void AddItem(Item item, int x, int y)
    {
        if (!InBounds(x, y))
            throw new System.InvalidOperationException("Cannot place item out of bounds!");

        // Stop rendering the item that was previously on top of the stack
        if (itemsOnGround[x, y].Count > 0)
            itemsOnGround[x, y].Peek().renderer.enabled = false;

        // Add the new item to the stack
        itemsOnGround[x, y].Push(item);
    }

    // Removes the item on top of the given stack and returns it. The item is not destroyed!
    public Item RemoveItem(int x, int y)
    {
        if (!InBounds(x, y))
            throw new System.InvalidOperationException("Cannot remove item out of bounds!");

        if (itemsOnGround[x, y].Count == 0)
            throw new System.InvalidOperationException("Cannot remove item from empty stack!");

        // Remove the item on top
        Item top = itemsOnGround[x, y].Pop();

        // Start rendering the item that is now on top of the stack
        if (itemsOnGround[x, y].Count > 0)
            itemsOnGround[x, y].Peek().renderer.enabled = true;

        return top;
    }

    public void AddOrganism(Organism organism, int x, int y)
    {
        if (!InBounds(x, y))
            throw new System.InvalidOperationException("Cannot place organism out of bounds!");

        if (organisms[x, y] != null)
            throw new System.InvalidOperationException("Cannot place one organism on top of another!");

        organisms[x, y] = organism;
        organism.position = new Point(x, y);
        organism.transform.position = new Vector2(x, y);
    }

    public Organism RemoveOrganism(int x, int y)
    {
        if (!InBounds(x, y))
            throw new System.InvalidOperationException("Cannot remove an organism out of bounds!");

        if (organisms[x, y] == null)
            throw new System.InvalidOperationException("No organism to remove!");

        Organism removed = organisms[x, y];
        organisms[x, y] = null;
        return removed;
    }

    #endregion

    #region Useful interfaces

    // Determines whether or not a given tile is walkable and unoccupied
    public bool IsOpen(int x, int y)
    {
        return GetTile(x, y).Walkable() && organisms[x, y] == null;
    }

    // Determines whether a given position is within the valid bounds of the level
    public bool InBounds(int x, int y)
    {
        return (x >= 0 && x < numCols && y >= 0 && y < numRows);
    }

    // If a position is in bounds, the tile at that position is returned (otherwise null)
    public Tile GetTile(int x, int y)
    {
        if (InBounds(x, y))
            return map[x, y];

        return Tile.None;
    }

    public Organism GetOrganism(int x, int y)
    {
        if (InBounds(x, y))
            return organisms[x, y];

        return null;
    }

    public Item GetItem(int x, int y)
    {
        if (InBounds(x, y))
            if (itemsOnGround[x,y].Count > 0)
                return itemsOnGround[x, y].Peek();

        return null;
    }

    #endregion

    public override string ToString()
    {
        String s = "";
        for (int y = 0; y < numRows; y++)
        {
            for (int x = 0; x < numCols; x++)
            {
                // If there is an item there, then overlay that
                // Eventually would also show monsters and player, but since we're doing a graphical output eventually it isn't worth the bother connecting the classes for this
                s += map[x, y].Char().ToString();
            }
            s += '\n';
        }
        return s;
    }
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Tiles
{
    public enum Tile
    {
        Ground,         // '.'
        Wall,           // '#'
        Upstairs,       // '<'
        Downstairs,     // '>'
        None            // 'X' (shouldn't actually be loaded into levels)
    }

    // Holds extensions methods for the Tile enum
    public static class TileHandler
    {
        private static Dictionary<Tile, char> tileChars;
        private static Dictionary<Tile, Sprite> tileSprites;
        private static Dictionary<Tile, bool> tileWalkability;

        public static void Initialize()
        {
            tileChars = new Dictionary<Tile, char>();
            tileSprites = new Dictionary<Tile, Sprite>();
            tileWalkability = new Dictionary<Tile, bool>();

            tileChars.Add(Tile.Ground, '.');
            tileChars.Add(Tile.Wall, '#');
            tileChars.Add(Tile.Upstairs, '<');
            tileChars.Add(Tile.Downstairs, '>');

            tileSprites.Add(Tile.Ground, GameManager.Sprites.ground);
            tileSprites.Add(Tile.Wall, GameManager.Sprites.wall);
            tileSprites.Add(Tile.Upstairs, GameManager.Sprites.upstairs);
            tileSprites.Add(Tile.Downstairs, GameManager.Sprites.downstairs);

            tileWalkability.Add(Tile.Ground, true);
            tileWalkability.Add(Tile.Wall, false);
            tileWalkability.Add(Tile.Upstairs, true);
            tileWalkability.Add(Tile.Downstairs, true);
        }

        public static bool Walkable(this Tile tile)
        {
            return tileWalkability[tile];
        }

        public static Sprite Sprite(this Tile tile)
        {
            return tileSprites[tile];
        }

        public static char Char(this Tile tile)
        {
            return tileChars[tile];
        }

        public static bool Blocking(this Tile tile)
        {
            if (tile == Tile.Wall)
                return true;

            return false;
        }
    }
}
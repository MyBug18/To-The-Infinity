using System;
using System.Collections.Generic;

namespace Core
{
    /// <summary>
    /// Clockwise tile direction
    /// </summary>
    public enum TileDirection
    {
        Right = 0,      // (+1,  0)
        UpRight = 1,    // ( 0, +1)
        UpLeft = 2,     // (-1, +1)
        Left = 3,       // (-1,  0)
        DownLeft = 4,   // ( 0, -1)
        DownRight = 5,  // (+1, -1)
    }

    public readonly struct HexTileCoord : IEquatable<HexTileCoord>
    {
        public int Q { get; }
        public int R { get; }

        public HexTileCoord(int q, int r)
        {
            Q = q;
            R = r;
        }

        public readonly static HexTileCoord Right = new HexTileCoord(1, 0);
        public readonly static HexTileCoord UpRight = new HexTileCoord(0, 1);
        public readonly static HexTileCoord UpLeft = new HexTileCoord(-1, 1);
        public readonly static HexTileCoord Left = new HexTileCoord(-1, 0);
        public readonly static HexTileCoord DownLeft = new HexTileCoord(0, -1);
        public readonly static HexTileCoord DownRight = new HexTileCoord(1, -1);

        public readonly static HashSet<HexTileCoord> AllDirectionSet = new HashSet<HexTileCoord>
        {
            Right,
            UpRight,
            UpLeft,
            Left,
            DownLeft,
            DownRight,
        };

        public override string ToString() => $"({Q}, {R})";

        public bool Equals(HexTileCoord c) => c.Q == Q && c.R == R;

        public override bool Equals(object obj) => obj is HexTileCoord c && Equals(c);

        public override int GetHashCode() => base.GetHashCode();

        public static HexTileCoord operator +(HexTileCoord coord) => coord;

        public static HexTileCoord operator -(HexTileCoord coord) => new HexTileCoord(-coord.Q, -coord.R);

        public static HexTileCoord operator +(HexTileCoord coord1, HexTileCoord coord2) =>
            new HexTileCoord(coord1.Q + coord2.Q, coord1.R + coord2.R);

        public static HexTileCoord operator -(HexTileCoord coord1, HexTileCoord coord2) => coord1 + (-coord2);

        public static bool operator ==(HexTileCoord coord1, HexTileCoord coord2) => coord1.Equals(coord2);

        public static bool operator !=(HexTileCoord coord1, HexTileCoord coord2) => !coord1.Equals(coord2);

        public bool IsAdjacent(HexTileCoord coord, bool includeCenter) =>
            this == coord ? includeCenter : AllDirectionSet.Contains(this - coord);


        public HexTileCoord AddDirection(TileDirection dir)
        {
            return dir switch
            {
                TileDirection.Right => this + Right,
                TileDirection.UpRight => this + UpRight,
                TileDirection.UpLeft => this + UpLeft,
                TileDirection.Left => this + Left,
                TileDirection.DownLeft => this + DownLeft,
                TileDirection.DownRight => this + DownRight,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
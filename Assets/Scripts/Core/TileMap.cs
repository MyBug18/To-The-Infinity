using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public class TileMap : IEnumerable<HexTile>
    {
        private readonly HexTile[][] _tileMap;

        private readonly Dictionary<HexTileCoord, List<IOnHexTileObject>> _onTileMapObjects =
            new Dictionary<HexTileCoord, List<IOnHexTileObject>>();

        public readonly int Radius;

        public TileMap(int radius)
        {
            Radius = radius;

            _tileMap = new HexTile[radius * 2 + 1][];
            ConstructTileMap();
        }

        private void ConstructTileMap()
        {
            for (var r = 0; r < Radius; r++)
            {
                _tileMap[r] = new HexTile[Radius + r + 1];
                for (var q = Radius - r; q <= 2 * Radius; q++)
                {
                    var qIdx = q - Radius + r;
                    _tileMap[r][qIdx] = new HexTile(new HexTileCoord(q, r));
                }
            }

            for (var r = Radius; r <= 2 * Radius; r++)
            {
                _tileMap[r] = new HexTile[3 * Radius - r + 1];
                for (var q = 0; q <= 3 * Radius - r; q++)
                {
                    _tileMap[r][q] = new HexTile(new HexTileCoord(q, r));
                }
            }
        }

        /// <summary>
        /// Is it valid coordinate on this tile map?
        /// </summary>
        public bool IsValidCoord(HexTileCoord coord)
        {
            return coord.Q + coord.R >= Radius && coord.Q + coord.R <= 3 * Radius;
        }

        public bool IsValidCoord(int q, int r)
        {
            return q + r >= Radius && q + r <= 3 * Radius;
        }

        public HexTile GetHexTile(HexTileCoord coord)
        {
            if (!IsValidCoord(coord))
                return null;

            var q = coord.Q;
            var r = coord.R;

            if (r < Radius)
                q = q - Radius + r;

            return _tileMap[r][q];
        }

        public List<HexTileCoord> GetRing(int radius, HexTileCoord? center = null)
        {
            if (radius < 1)
                throw new InvalidOperationException("Radius of a ring must be bigger than 0!");

            var current = center ?? new HexTileCoord(Radius, Radius);

            var resultList = new List<HexTileCoord>();

            // To the start point
            for (var i = 0; i < radius; i++)
                current = current.AddDirection(TileDirection.Right);

            for (var i = 2; i < 8; i++)
            {
                var walkDir = (TileDirection)(i % 6);
                for (var j = 0; j < radius; j++)
                {
                    // Add only valid coordinates
                    if (IsValidCoord(current))
                        resultList.Add(current);
                    current = current.AddDirection(walkDir);
                }
            }
            return resultList;
        }

        public HexTileCoord GetRandomCoordFromRing(int radius, HexTileCoord? center = null)
        {
            var ring = GetRing(radius, center);
            var count = ring.Count;
            var decider = UnityEngine.Random.value;
            for (var i = 0; i < count; i++)
            {
                var chance = 1.0f / (count - i);
                if (decider < chance)
                    return ring[i];
            }

            throw new InvalidOperationException("Chance generator has broken!");
        }

        public IReadOnlyList<IOnHexTileObject> GetAllTileObjects(HexTileCoord coord) =>
            !_onTileMapObjects.TryGetValue(coord, out var result) ? null : result;

        /// <summary>
        /// Gets collection of OnHexTileObject with given type.
        /// </summary>
        /// <returns>Returns null if given type is not in the dict.</returns>
        public IReadOnlyList<T> GetTileObjectList<T>() where T : IOnHexTileObject =>
            (from objLists in _onTileMapObjects.Values
                from obj in objLists
                where obj.TypeName == nameof(T)
                select (T) obj).ToList();

        public IEnumerator<HexTile> GetEnumerator() =>
            _tileMap.SelectMany(tileArray => tileArray).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
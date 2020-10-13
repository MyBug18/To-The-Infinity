﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public sealed class TileMap : IEnumerable<HexTile>
    {
        public ITileMapHolder Holder { get; }

        private readonly HexTile[][] _tileMap;

        private readonly Dictionary<HexTileCoord, Dictionary<string, IOnHexTileObject>> _onTileMapObjects =
            new Dictionary<HexTileCoord, Dictionary<string, IOnHexTileObject>>();

        public int Radius { get; }

        public int Seed { get; }

        public TileMap(ITileMapHolder holder, HexTile[][] tileMap, int radius, int seed)
        {
            Holder = holder;
            _tileMap = tileMap;
            Radius = radius;
            Seed = seed;
        }

        public void StartNewTurn(int month)
        {
            foreach (var obj in _onTileMapObjects.Values.SelectMany(objs => objs.Values))
            {
                obj.StartNewTurn(month);
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

        public int GetDistanceOfTwoTile(HexTileCoord c1, HexTileCoord c2)
        {
            if (!IsValidCoord(c1) || !IsValidCoord(c2)) return -1;

            return (Math.Abs(c1.Q - c2.Q) + Math.Abs(c1.Q + c1.R - c2.Q - c2.R) + Math.Abs(c1.R - c2.R)) / 2;
        }

        public IReadOnlyList<IOnHexTileObject> GetAllTileObjects(HexTileCoord coord) =>
            !_onTileMapObjects.TryGetValue(coord, out var result) ? new List<IOnHexTileObject>() : result.Values.ToList();

        /// <summary>
        /// Gets collection of OnHexTileObject with given type.
        /// </summary>
        /// <returns>Returns null if given type is not in the dict.</returns>
        public IReadOnlyList<T> GetTileObjectList<T>() where T : IOnHexTileObject =>
            (from objDict in _onTileMapObjects.Values
             from obj in objDict.Values
             where obj.TypeName == nameof(T)
             select (T)obj).ToList();

        public void AddTileObject(string typeName, string name, HexTileCoord coord)
        {
            var gameData = GameDataStorage.Instance.GetGameData(typeName);

            if (gameData == null)
                return;

            switch (gameData)
            {
                default:
                    return;
            }
        }

        public void AddTileObject(IOnHexTileObject obj, HexTileCoord coord)
        {
            if (!_onTileMapObjects.ContainsKey(coord))
                _onTileMapObjects.Add(coord, new Dictionary<string, IOnHexTileObject>());

            var objDict = _onTileMapObjects[coord];
            if (objDict.ContainsKey(obj.TypeName)) return;

            objDict[obj.TypeName] = obj;
        }

        public void RemoveTileObject(string typeName, HexTileCoord coord)
        {
            if (!IsValidCoord(coord)) return;

            if (!_onTileMapObjects.TryGetValue(coord, out var objs)) return;

            if (!objs.ContainsKey(typeName)) return;

            objs.Remove(typeName);

            // Remove dictionary if there is no object left
            if (objs.Count == 0)
                _onTileMapObjects.Remove(coord);
        }

        public void ApplyModifierChangeToTileObjects(Modifier m, bool isRemoving)
        {
            foreach (var objs in _onTileMapObjects
                // Should not apply effect to tile out of it's effect range
                .Where(objDict => m.IsInEffectRange(objDict.Key))
                .SelectMany(objDict => objDict.Value)
                .Select(x => x.Value))
                // Tile limit should not remain after apply
                objs.ApplyModifierChangeToDownward(m.WithoutTileLimit(), isRemoving);
        }

        public bool IsTileObjectExists(string typeName, HexTileCoord coord)
            => _onTileMapObjects.TryGetValue(coord, out var objDict) && objDict.ContainsKey(typeName);

        public IEnumerator<HexTile> GetEnumerator() =>
            _tileMap.SelectMany(tileArray => tileArray).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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
    }
}

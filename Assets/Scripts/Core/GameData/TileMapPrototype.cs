using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Core.GameData
{
    public sealed class TileMapPrototype : ILuaHolder
    {
        private ScriptFunctionDelegate<Dictionary<string, object>> _tileInfoMaker;

        public TileMapPrototype(string filePath) => FilePath = filePath;

        public string IdentifierName { get; private set; }

        public string TypeName => "TileMap";

        public string FilePath { get; }

        public bool Load(Script luaScript)
        {
            IdentifierName = luaScript.Globals.Get("Name").String;
            _tileInfoMaker = luaScript.Globals.Get("MakeTile").Function.GetDelegate<Dictionary<string, object>>();

            return true;
        }

        public TileMap Create(ITileMapHolder holder, int radius, int? seed = null)
        {
            var tileMapSeed = seed ?? Random.Range(int.MinValue, int.MaxValue);
            var sharedStorage = new LuaDictWrapper(new Dictionary<string, object>
                {{"random", new System.Random(tileMapSeed)}});

            var size = 2 * radius + 1;

            var noiseMap = Noise2d.GenerateNoiseMap(size, size, 2, seed);

            var tileMap = new HexTile[size][];

            var lineCount = new int[size];

            for (var i = 0; i < radius; i++)
            {
                tileMap[i] = new HexTile[radius + i + 1];
                lineCount[i] = radius + i + 1;
            }

            for (var i = radius; i < 2 * radius + 1; i++)
            {
                tileMap[i] = new HexTile[3 * radius - i + 1];
                lineCount[i] = 3 * radius - i + 1;
            }

            var result = new TileMap(holder, tileMap, radius, tileMapSeed);

            for (var i = 0; i < lineCount.Sum(); i++)
            {
                var (x, y) = GetTileMapIndexFromInt(i);

                var q = x > radius ? y : y + (radius - x);
                var r = x;

                var coord = new HexTileCoord(q, r);
                var noise = noiseMap[coord.Q, coord.R];

                tileMap[x][y] = GenerateTile(result, coord, noise, sharedStorage);
            }

            return result;

            (int x, int y) GetTileMapIndexFromInt(int n)
            {
                var x = 0;
                var y = 0;

                var left = n;

                for (var i = 0; i < lineCount.Length; i++)
                {
                    if (left < lineCount[i])
                    {
                        x = i;
                        y = left;
                        break;
                    }

                    left -= lineCount[i];
                }

                return (x, y);
            }
        }

        private HexTile GenerateTile(TileMap tileMap, HexTileCoord coord, float noise, LuaDictWrapper sharedStorage)
        {
            var dict = _tileInfoMaker.Invoke(coord, noise, sharedStorage);

            var name = (string)dict["Name"];
            var res = (int)(double)dict["ResDecider"];

            var tileProto = GameDataStorage.Instance.GetGameData<HexTileData>().GetPrototype(name);
            var tile = tileProto.Create(tileMap, coord, res);

            return tile;
        }
    }
}

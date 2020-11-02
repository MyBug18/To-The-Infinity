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

            var size = 2 * radius + 1;

            var noiseMap = Noise2d.GenerateNoiseMap(size, size, 2, seed);
            var randomMap = MakeRandomMap(size, tileMapSeed);

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

                var r = x;
                var q = r > radius ? y : y + (radius - r);

                var coord = new HexTileCoord(q, r);
                var noise = noiseMap[coord.Q, coord.R];
                var rnd = randomMap[coord.Q, coord.R];

                tileMap[x][y] = GenerateTile(result, coord, noise, rnd);
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

            static float[,] MakeRandomMap(int size, int seed)
            {
                var r = new System.Random(seed);

                var result = new float[size, size];

                for (var i = 0; i < size; i++)
                for (var j = 0; j < size; j++)
                    result[i, j] = (float) r.NextDouble();

                return result;
            }
        }

        private HexTile GenerateTile(TileMap tileMap, HexTileCoord coord, float noise, float rnd)
        {
            var dict = _tileInfoMaker.Invoke(coord, noise, rnd);

            var name = (string) dict["Name"];
            // var res = (int)dict["ResDecider"];

            var tileProto = GameDataStorage.Instance.GetGameData<HexTileData>().GetPrototype(name);
            var tile = tileProto.Create(tileMap, coord, 0);

            return tile;
        }
    }
}

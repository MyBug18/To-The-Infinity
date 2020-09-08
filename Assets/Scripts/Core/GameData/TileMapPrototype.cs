using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MoonSharp.Interpreter;

namespace Core.GameData
{
    public class TileMapPrototype : ILuaHolder
    {
        public string Name { get; private set; }

        public string TypeName => "TileMap";

        public string FilePath { get; }

        private ScriptFunctionDelegate<Dictionary<string, object>> _tileInfoMaker;

        public TileMapPrototype(string filePath)
        {
            FilePath = filePath;
        }

        public bool Load(Script luaScript)
        {
            Name = luaScript.Globals.Get("Name").String;
            _tileInfoMaker = luaScript.Globals.Get("MakeTile").Function.GetDelegate<Dictionary<string, object>>();

            return true;
        }

        public TileMap Create(int radius)
        {
            var size = 2 * radius + 1;

            var noiseTask = new Task<float[,]>(() => Noise2d.GenerateNoiseMap(size, size, 2));
            var randomTask = new Task<float[,]>(() => MakeRandomMap(size));

            var tileMap = new HexTile[radius * 2 + 1][];

            var lineCount = new int[radius * 2 + 1];

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

            var noiseMap = noiseTask.Result;
            var randomMap = randomTask.Result;

            Parallel.For(0, lineCount.Sum(), i =>
            {
                var (x, y) = GetTileMapIndexFromInt(i);
                var coord = new HexTileCoord(y + (radius - x), x);
                var noise = noiseMap[coord.Q, coord.R];
                var rnd = randomMap[coord.Q, coord.R];

                tileMap[x][y] = GenerateTileThreadSafe(coord, noise, rnd);
            });

            return new TileMap(tileMap, radius);

            (int x, int y) GetTileMapIndexFromInt(int n)
            {
                var x = 0;
                var y = 0;

                var left = n;

                for (var i = 0; i < lineCount.Length; i++)
                {
                    if (left <= lineCount[i])
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

        private HexTile GenerateTileThreadSafe(HexTileCoord coord, float noise, float rnd)
        {
            var dict = _tileInfoMaker.Invoke(coord, noise, rnd);

            var name = (string) dict["Name"];
            var res = (int) dict["ResDecider"];

            var tileProto = GameDataStorage.Instance.GetGameData<HexTileData>().GetPrototype(name);
            var tile = tileProto.Create(coord, res);

            return tile;
        }

        private float[,] MakeRandomMap(int size)
        {
            var r = new Random();

            var result = new float[size, size];

            for (var i = 0; i < size; i++)
            {
                for (var j = 0; j < size; j++)
                {
                    result[i, j] = (float) r.NextDouble();
                }
            }

            return result;
        }
    }
}
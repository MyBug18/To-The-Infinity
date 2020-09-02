using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MoonSharp.Interpreter;

namespace Core.GameData
{
    public class TileMapPrototype : ILuaHolder
    {
        public string Name { get; }

        public string TypeName => "TileMap";

        public string FilePath { get; }

        public bool Load(Script luaScript)
        {
            throw new System.NotImplementedException();
        }

        public TileMap ConstructTileMap(int radius)
        {
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

            var noiseMap = Noise2d.GenerateNoiseMap(2 * radius + 1, 2 * radius + 1, 2);

            Parallel.For(0, lineCount.Sum(), i =>
            {
                var (x, y) = GetTileMapIndexFromInt(i);
                var coord = new HexTileCoord(y + (radius - x), x);
                var noise = noiseMap[coord.Q, coord.R];

                tileMap[x][y] = GenerateTileThreadSafe(coord, noise);
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

        private HexTile GenerateTileThreadSafe(HexTileCoord coord, float noise)
        {
            return new HexTile(coord);
        }
    }
}
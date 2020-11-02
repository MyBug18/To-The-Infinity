using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;

namespace Core.GameData
{
    public sealed class HexTilePrototype : ILuaHolder
    {
        public HexTilePrototype(string filePath) => FilePath = filePath;

        public IReadOnlyList<(int value, string resName)> ResChanceMap { get; private set; }
        public string IdentifierName { get; private set; }
        public string TypeName => "HexTile";

        public string FilePath { get; }

        public bool Load(Script luaScript)
        {
            IdentifierName = luaScript.Globals.Get("Name").String;

            var resChanceMap =
                luaScript.Globals.Get("ResChanceMap").Table.Pairs
                    .Select(kv => ((int) kv.Key.Number, kv.Value.String)).ToList();

            resChanceMap.Sort((x, y) => y.Item1.CompareTo(x.Item1));

            ResChanceMap = resChanceMap;

            return true;
        }

        public HexTile Create(TileMap tileMap, HexTileCoord coord, int resDecider)
        {
            var specialResourceName = "";

            foreach (var (value, resName) in ResChanceMap)
            {
                if (resDecider <= value)
                {
                    specialResourceName = resName;
                    continue;
                }

                if (resDecider > value)
                    break;
            }

            if (string.IsNullOrEmpty(specialResourceName))
                return new HexTile(tileMap, coord, IdentifierName, null);

            var specialResource = GameDataStorage.Instance.GetGameData<TileSpecialResourceTypeData>()
                .GetDirectly(specialResourceName);

            return new HexTile(tileMap, coord, IdentifierName, specialResource);
        }
    }
}

using MoonSharp.Interpreter;
using System.Collections.Generic;
using System.Linq;

namespace Core.GameData
{
    public class HexTilePrototype : ILuaHolder
    {
        public string Name { get; private set; }
        public string TypeName => "HexTile";

        public string FilePath { get; }

        public int MoveCost { get; private set; }

        public IReadOnlyList<(int value, string resName)> ResChanceMap { get; private set; }

        public HexTilePrototype(string filePath)
        {
            FilePath = filePath;
        }

        public bool Load(Script luaScript)
        {
            Name = luaScript.Globals.Get("Name").String;
            MoveCost = (int)luaScript.Globals.Get("MoveCost").Number;

            var resChanceMap =
                luaScript.Globals.Get("ResChanceMap").Table.Pairs
                    .Select(kv => ((int)kv.Key.Number, kv.Value.String)).ToList();

            resChanceMap.Sort((x, y) => y.Item1.CompareTo(x.Item1));

            ResChanceMap = resChanceMap;

            return true;
        }

        public HexTile Create(HexTileCoord coord, int resDecider)
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
                return new HexTile(coord, Name, null);

            var specialResource = GameDataStorage.Instance.GetGameData<TileSpecialResourceTypeData>()
                .GetPrototype(specialResourceName);

            return new HexTile(coord, Name, specialResource.Create());
        }
    }
}

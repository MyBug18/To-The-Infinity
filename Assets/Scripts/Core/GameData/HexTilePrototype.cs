using System.Collections.Generic;
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
            var t = luaScript.Globals;

            if (!t.TryGetString("Name", out var name,
                MoonSharpUtil.LoadingError("Name", FilePath)))
                return false;

            IdentifierName = name;

            if (!t.TryGetTable("ResChanceMap", out var table,
                MoonSharpUtil.LoadingError("ResChanceMap", FilePath)))
                return false;

            var resChanceMap = new List<(int value, string resName)>();

            foreach (var kv in table.Pairs)
            {
                if (!kv.Key.TryGetInt(out var k, MoonSharpUtil.LoadingError("ResChanceMap.Key", FilePath)))
                    return false;

                if (!kv.Value.TryGetString(out var v, MoonSharpUtil.LoadingError("ResChanceMap.Value", FilePath)))
                    return false;

                resChanceMap.Add((k, v));
            }

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

            var specialResource = TileSpecialResourceTypeData.Instance.GetDirectly(specialResourceName);

            return new HexTile(tileMap, coord, IdentifierName, specialResource);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;

namespace Core.GameData
{
    public sealed class SpecialActionPrototype : ILuaHolder
    {
        private SpecialActionCore _cache;

        public SpecialActionPrototype(string filePath) => FilePath = filePath;

        public string IdentifierName { get; private set; }

        public string TypeName { get; }

        public string FilePath { get; }

        public bool Load(Script luaScript)
        {
            IdentifierName = luaScript.Globals.Get("Name").String;

            var needCoordinate = luaScript.Globals.Get("NeedCoordinate").Boolean;

            var isAvailable = luaScript.Globals.Get("IsAvailable").Function.GetDelegate<bool>();
            var getAvailableTiles =
                luaScript.Globals.Get("GetAvailableTiles").Function.GetDelegate<List<HexTileCoord>>();
            var previewEffectRange =
                luaScript.Globals.Get("PreviewEffectRange").Function.GetDelegate<List<HexTileCoord>>();
            var getCost = luaScript.Globals.Get("GetCost").Function.GetDelegate<Dictionary<string, int>>();
            var doAction = luaScript.Globals.Get("DoAction").Function.GetDelegate<bool>();

            _cache = new SpecialActionCore(IdentifierName, needCoordinate,
                owner => isAvailable.Invoke(owner),
                owner => getAvailableTiles.Invoke(owner),
                (owner, coord) => previewEffectRange.Invoke(owner, coord),
                ProcessCost,
                (owner, coord) => doAction.Invoke(owner, coord));

            return true;

            Dictionary<string, int> ProcessCost(ISpecialActionHolder owner, HexTileCoord coord)
            {
                return getCost.Invoke(owner, coord).ToDictionary(kv => kv.Key, kv => kv.Value);
            }
        }

        public SpecialAction Create(ISpecialActionHolder owner) => new SpecialAction(_cache, owner);
    }
}

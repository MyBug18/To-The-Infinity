using MoonSharp.Interpreter;
using System.Collections.Generic;
using System.Linq;

namespace Core.GameData
{
    public sealed class SpecialActionPrototype : ILuaHolder
    {
        public string Name { get; private set; }

        public string TypeName { get; }

        public string FilePath { get; }

        private SpecialActionCore _cache;

        public SpecialActionPrototype(string filePath)
        {
            FilePath = filePath;
        }

        public bool Load(Script luaScript)
        {
            Name = luaScript.Globals.Get("Name").String;

            var needCoordinate = luaScript.Globals.Get("NeedCoordinate").Boolean;

            var data = GameDataStorage.Instance.GetGameData<ResourceData>();

            var isAvailable = luaScript.Globals.Get("IsAvailable").Function.GetDelegate<bool>();
            var getAvailableTiles =
                luaScript.Globals.Get("GetAvailableTiles").Function.GetDelegate<List<HexTileCoord>>();
            var previewEffectRange =
                luaScript.Globals.Get("PreviewEffectRange").Function.GetDelegate<List<HexTileCoord>>();
            var getCost = luaScript.Globals.Get("GetCost").Function.GetDelegate<Dictionary<string, int>>();
            var doAction = luaScript.Globals.Get("DoAction").Function.GetDelegate();

            _cache = new SpecialActionCore(Name, needCoordinate,
                owner => isAvailable.Invoke(owner),
                owner => getAvailableTiles.Invoke(owner),
                (owner, coord) => previewEffectRange.Invoke(owner, coord),
                ProcessCost,
                (owner, coord) => doAction.Invoke(owner, coord));

            return true;

            Dictionary<ResourceInfoHolder, int> ProcessCost(ISpecialActionHolder owner, HexTileCoord coord)
                => getCost.Invoke(owner, coord).ToDictionary(kv => data.GetResourceDirectly(kv.Key), kv => kv.Value);
        }

        public SpecialAction Create(ISpecialActionHolder owner) => new SpecialAction(_cache, owner);
    }
}

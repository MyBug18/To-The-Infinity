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
            var cost = luaScript.Globals.Get("Cost").Table.Pairs
                .ToDictionary(kv => data.GetResourceDirectly(kv.Key.String), kv => (int)kv.Value.Number);

            var isVisible = luaScript.Globals.Get("IsVisible").Function.GetDelegate<bool>();
            var isAvailable = luaScript.Globals.Get("IsAvailable").Function.GetDelegate<bool>();
            var getAvailableTiles =
                luaScript.Globals.Get("GetAvailableTiles").Function.GetDelegate<List<HexTileCoord>>();
            var doAction = luaScript.Globals.Get("DoAction").Function.GetDelegate();
            _cache = new SpecialActionCore(Name, needCoordinate, cost,
                owner => isVisible.Invoke(owner),
                owner => isAvailable.Invoke(owner),
                owner => getAvailableTiles.Invoke(owner),
                (owner, coord) => doAction.Invoke(owner, coord));

            return true;
        }

        public SpecialAction Create(ISpecialActionHolder owner) => new SpecialAction(_cache, owner);
    }
}

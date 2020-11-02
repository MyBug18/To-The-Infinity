using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace Core.GameData
{
    public sealed class PopSlotPrototype : ILuaHolder
    {
        private readonly Script _script = new Script();

        public PopSlotPrototype(string filePath) => FilePath = filePath;

        public string Group { get; private set; }

        public IReadOnlyDictionary<string, float> BaseYield { get; private set; }

        public IReadOnlyDictionary<string, float> BaseUpkeep { get; private set; }
        public string TypeName => "PopSlot";

        public string FilePath { get; }

        public string IdentifierName { get; private set; }

        public bool Load(Script luaScript)
        {
            IdentifierName = luaScript.Globals.Get("Name").String;
            Group = luaScript.Globals.Get("Group").String;

            var yield = new Dictionary<string, float>();
            foreach (var kv in luaScript.Globals.Get("Yield").Table.Pairs)
                yield[kv.Key.String] = (float) kv.Value.Number;
            BaseYield = yield;

            var upkeep = new Dictionary<string, float>();
            foreach (var kv in luaScript.Globals.Get("Upkeep").Table.Pairs)
                upkeep[kv.Key.String] = (float) kv.Value.Number;
            BaseUpkeep = upkeep;

            return true;
        }
    }
}

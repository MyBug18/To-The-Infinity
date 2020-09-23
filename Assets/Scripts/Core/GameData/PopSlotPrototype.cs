using MoonSharp.Interpreter;
using System.Collections.Generic;

namespace Core.GameData
{
    public sealed class PopSlotPrototype : ILuaHolder
    {
        public string TypeName => "PopSlot";

        public string FilePath { get; }

        public string Name { get; private set; }

        public string Group { get; private set; }

        public IReadOnlyDictionary<string, float> BaseYield { get; private set; }

        public IReadOnlyDictionary<string, float> BaseUpkeep { get; private set; }

        private readonly Script _script = new Script();

        public PopSlotPrototype(string filePath) => FilePath = filePath;

        public bool Load(Script luaScript)
        {
            Name = luaScript.Globals.Get("Name").String;
            Group = luaScript.Globals.Get("Group").String;

            var yield = new Dictionary<string, float>();
            foreach (var kv in luaScript.Globals.Get("Yield").Table.Pairs)
            {
                yield[kv.Key.String] = (float)kv.Value.Number;
            }
            BaseYield = yield;

            var upkeep = new Dictionary<string, float>();
            foreach (var kv in luaScript.Globals.Get("Upkeep").Table.Pairs)
            {
                upkeep[kv.Key.String] = (float)kv.Value.Number;
            }
            BaseUpkeep = upkeep;

            return true;
        }
    }
}

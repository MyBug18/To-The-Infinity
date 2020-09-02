using System.Collections.Generic;
using System.IO;
using MoonSharp.Interpreter;

namespace Core.GameData
{
    public class PopSlotPrototype : ILuaHolder
    {
        public string TypeName => "PopSlot";

        public string FilePath { get; }

        public string Name { get; private set; }

        public string Group { get; private set; }

        public IReadOnlyDictionary<string, float> BaseYield { get; private set; }

        public IReadOnlyDictionary<string, float> BaseUpkeep { get; private set; }

        private readonly Script _script = new Script();

        public PopSlotPrototype(string filePath) => FilePath = filePath;

        public bool Load(Script luaCode)
        {
            Name = luaCode.Globals.Get("Name").String;
            Group = luaCode.Globals.Get("Group").String;

            var yield = new Dictionary<string, float>();
            foreach (var kv in luaCode.Globals.Get("Yield").Table.Pairs)
            {
                yield[kv.Key.String] = (float) kv.Value.Number;
            }
            BaseYield = yield;

            var upkeep = new Dictionary<string, float>();
            foreach (var kv in luaCode.Globals.Get("Upkeep").Table.Pairs)
            {
                upkeep[kv.Key.String] = (float) kv.Value.Number;
            }
            BaseUpkeep = upkeep;

            return true;
        }
    }
}

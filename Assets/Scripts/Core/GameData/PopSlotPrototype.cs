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
            var t = luaScript.Globals;

            if (!t.TryGetString("Name", out var identifierName,
                MoonSharpUtil.LoadingError("Name", FilePath)))
                return false;

            IdentifierName = identifierName;

            if (!t.TryGetString("Group", out var group,
                MoonSharpUtil.LoadingError("Group", FilePath)))
                return false;

            Group = group;

            var yield = new Dictionary<string, float>();

            if (t.TryGetTable("Yield", out var yieldTable, MoonSharpUtil.AllowNotDefined("Yield", FilePath)))
            {
                foreach (var kv in yieldTable.Pairs)
                {
                    if (!kv.Key.TryGetString(out var resName, MoonSharpUtil.LoadingError("Yield.Key", FilePath)))
                        return false;

                    if (!kv.Value.TryGetFloat(out var resAmount, MoonSharpUtil.LoadingError("Yield.Value", FilePath)))
                        return false;

                    if (resAmount < 0)
                    {
                        Logger.Log(LogType.Warning, "Field Yield.Value of " + FilePath,
                            "Yield should not be smaller than 0, so it will be ignored.");
                        continue;
                    }

                    if (yield.ContainsKey(resName))
                    {
                        Logger.Log(LogType.Warning, $"Field Yield.Key ({resName}) of " + FilePath,
                            "Same yield name detected, so it will be automatically merged");

                        yield[resName] += resAmount;
                        continue;
                    }

                    yield[resName] = resAmount;
                }
            }

            BaseYield = yield;

            var upkeep = new Dictionary<string, float>();

            if (t.TryGetTable("Upkeep", out var upKeepTable, MoonSharpUtil.AllowNotDefined("Upkeep", FilePath)))
            {
                foreach (var kv in upKeepTable.Pairs)
                {
                    if (!kv.Key.TryGetString(out var resName, MoonSharpUtil.LoadingError("Upkeep.Key", FilePath)))
                        return false;

                    if (!kv.Value.TryGetFloat(out var resAmount, MoonSharpUtil.LoadingError("Upkeep.Value", FilePath)))
                        return false;

                    if (resAmount < 0)
                    {
                        Logger.Log(LogType.Warning, "Field Upkeep.Value of " + FilePath,
                            "Upkeep should not be smaller than 0, so it will be ignored.");
                        continue;
                    }

                    if (upkeep.ContainsKey(resName))
                    {
                        Logger.Log(LogType.Warning, $"Field Upkeep.Key ({resName}) of " + FilePath,
                            "Same upkeep name detected, so it will be automatically merged");

                        upkeep[resName] += resAmount;
                        continue;
                    }

                    upkeep[resName] = resAmount;
                }
            }

            BaseUpkeep = upkeep;

            return true;
        }
    }
}

using MoonSharp.Interpreter;
using System.Collections.Generic;
using System.Linq;

namespace Core.GameData
{
    public class ModifierPrototype : ILuaHolder
    {
        public string Name { get; private set; }

        public string TypeName => "Modifier";

        private ModifierCore _cache;

        public string FilePath { get; }

        public ModifierPrototype(string filePath)
        {
            FilePath = filePath;
        }

        public bool Load(Script luaScript)
        {
            Name = luaScript.Globals.Get("Name").String;

            var additionalInfo = luaScript.Globals.Get("Name").String;

            var holderType = luaScript.Globals.Get("TargetType").String;

            var conditionChecker = luaScript.Globals.Get("CheckCondition").Function.GetDelegate<bool>();

            var effectDictGetter = luaScript.Globals.Get("GetEffect").Function.GetDelegate<Dictionary<string, object>>();

            _cache = new ModifierCore(Name, holderType, additionalInfo, ProcessEffect,
                t => conditionChecker.Invoke(t));

            return true;

            List<ModifierEffect> ProcessEffect(IModifierHolder target)
            {
                var dict = effectDictGetter.Invoke(target);
                var data = GameDataStorage.Instance.GetGameData<ResourceData>();

                return (from kv in dict
                    select new ModifierEffect(data.GetResourceDirectly(kv.Key), (int) kv.Value)).ToList();
            }
        }

        public ModifierCore Create() => _cache;
    }
}

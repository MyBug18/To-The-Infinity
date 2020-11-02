using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;

namespace Core.GameData
{
    public sealed class ModifierPrototype : ILuaHolder
    {
        private ModifierCore _cache;

        public ModifierPrototype(string filePath) => FilePath = filePath;

        public string IdentifierName { get; private set; }

        public string TypeName => "Modifier";

        public string FilePath { get; }

        public bool Load(Script luaScript)
        {
            var t = luaScript.Globals;

            IdentifierName = t.Get("Name").String;

            var isTileLimited = t.Get("IsTileLimited").Boolean;

            var additionalDesc = t.Get("AdditionalDesc").String;

            var holderType = t.Get("TargetType").String;

            var scopeDict = new Dictionary<string, ModifierScope>();

            foreach (var nameTable in t.Get("Scope").Table.Pairs)
            {
                var name = nameTable.Key.String;
                var scopeTable = nameTable.Value.Table;
                var getEffect = scopeTable.Get("GetEffect").Function.GetDelegate<Dictionary<string, object>>();
                var checkCondition = scopeTable.Get("CheckCondition").Function.GetDelegate<bool>();
                var triggerEvent = scopeTable.Get("TriggerEvent").Table.Pairs
                    .ToDictionary(kv => kv.Key.String,
                        kv => kv.Value.Function.GetDelegate());

                var scope = new ModifierScope(name, ProcessEffect,
                    (target, adderGuid) => checkCondition.Invoke(target, adderGuid),
                    triggerEvent);

                scopeDict.Add(name, scope);

                List<ModifierEffect> ProcessEffect(IModifierHolder target, string adderGuid)
                {
                    var result = new List<ModifierEffect>();

                    var dict = getEffect.Invoke(target, adderGuid);
                    foreach (var kv in dict)
                    {
                        var additionalInfo = new List<string>();
                        var tokens = kv.Key.Split('_');
                        for (var i = 1; i < tokens.Length; i++)
                            additionalInfo.Add(tokens[i]);

                        result.Add(new ModifierEffect(tokens[0], additionalInfo, (int)kv.Value));
                    }

                    return result;
                }
            }

            _cache = new ModifierCore(IdentifierName, holderType, isTileLimited, additionalDesc, scopeDict);

            return true;
        }

        public ModifierCore Create() => _cache;
    }
}

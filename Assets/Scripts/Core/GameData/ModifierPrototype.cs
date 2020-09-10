using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;

namespace Core.GameData
{
    public class ModifierPrototype : ILuaHolder
    {
        public string Name { get; private set; }

        public string TypeName => "Modifier";

        private string _holderType;

        private string _additionalInfo;

        private IReadOnlyList<(string name, float amount)> _resourceInfo;

        private ScriptFunctionDelegate<bool> _conditionChecker;

        private IReadOnlyList<ModifierInfoHolder> _effect;

        public string FilePath { get; }

        public ModifierPrototype(string filePath)
        {
            FilePath = filePath;
        }

        public bool Load(Script luaScript)
        {
            Name = luaScript.Globals.Get("Name").String;

            _additionalInfo = luaScript.Globals.Get("Name").String;

            _holderType = luaScript.Globals.Get("HolderType").String;

            _conditionChecker = luaScript.Globals.Get("CheckCondition").Function.GetDelegate<bool>();

            var table = luaScript.Globals.Get("Effect").Table;

            var data = GameDataStorage.Instance.GetGameData<ResourceData>();

            var effect = new List<ModifierInfoHolder>();

            foreach (var kv in table.Pairs)
            {
                var name = kv.Key.String;
                var info = data.GetResourceDirectly(name);
                effect.Add(new ModifierInfoHolder(info, (int)kv.Value.Number));
            }

            _effect = effect;

            return true;
        }

        public Modifier Create(object target)
        {
            Func<bool> conditionChecker = () => _conditionChecker.Invoke(target);

            return new Modifier(Name, _holderType, _additionalInfo, _effect, conditionChecker);
        }
    }
}

using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.GameData
{
    public class ModifierPrototype : ILuaHolder
    {
        public string Name { get; private set; }

        public string TypeName => "Modifier";

        private string _holderType;

        private string _additionalInfo;

        private ScriptFunctionDelegate<bool> _conditionChecker;

        private ScriptFunctionDelegate<Dictionary<string, object>> _effectDictGetter;

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

            _effectDictGetter = luaScript.Globals.Get("GetEffect").Function.GetDelegate<Dictionary<string, object>>();

            return true;
        }

        public Modifier Create(object target)
        {
            if (Name != "Default" && target.GetType().ToString() != _holderType)
                throw new InvalidOperationException(
                    $"Trying to instantiate modifier with holder type {_holderType} to target type {target.GetType()}!");

            return new Modifier(Name, _holderType, _additionalInfo, () => ProcessEffect(target),
                () => _conditionChecker.Invoke(target));
        }

        private List<ModifierInfoHolder> ProcessEffect(object target)
        {
            var dict = _effectDictGetter.Invoke(target);
            var data = GameDataStorage.Instance.GetGameData<ResourceData>();

            return (from kv in dict
                select new ModifierInfoHolder(data.GetResourceDirectly(kv.Key), (int) kv.Value)).ToList();
        }
    }
}

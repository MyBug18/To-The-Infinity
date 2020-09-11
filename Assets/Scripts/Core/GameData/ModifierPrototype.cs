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

            var effect = (from kv in table.Pairs
                let name = kv.Key.String
                let info = data.GetResourceDirectly(name)
                select new ModifierInfoHolder(info, (int) kv.Value.Number)).ToList();

            _effect = effect;

            return true;
        }

        public Modifier Create(object target)
        {
            if (Name != "Default" && target.GetType().ToString() != _holderType)
                throw new InvalidOperationException(
                    $"Trying to instantiate modifier with holder type {_holderType} to target type {target.GetType()}!");

            return new Modifier(Name, _holderType, _additionalInfo, _effect, () => _conditionChecker.Invoke(target));
        }

    }
}

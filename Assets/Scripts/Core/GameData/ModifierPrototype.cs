using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace Core.GameData
{
    public class ModifierPrototype : ILuaHolder
    {
        public string Name { get; private set; }

        public string TypeName => "Modifier";

        private string _holderType;

        private IReadOnlyList<(string name, float amount)> _resourceInfo;

        private ScriptFunctionDelegate<bool> _conditionChecker;

        public string FilePath { get; }

        public ModifierPrototype(string filePath)
        {
            FilePath = filePath;
        }

        public bool Load(Script luaScript)
        {
            Name = luaScript.Globals.Get("Name").String;

            _holderType = luaScript.Globals.Get("HolderType").String;

            _conditionChecker = luaScript.Globals.Get("CheckCondition").Function.GetDelegate<bool>();

            return true;
        }
    }
}
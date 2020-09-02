using System;
using MoonSharp.Interpreter;

namespace Core.GameData
{
    public class SpecialActionPrototype : ILuaHolder
    {
        public string Name { get; }

        public string TypeName { get; }

        public bool IsValid { get; }

        public string FilePath { get; }

        public string OwnerTypeName { get; }

        private ScriptFunctionDelegate<bool> _dynamicIsVisible;

        private ScriptFunctionDelegate<bool> _dynamicIsAvailable;

        private ScriptFunctionDelegate<bool> _dynamicDoSpecialAction;

        public bool Load(Script luaScript)
        {
            throw new NotImplementedException();
        }

        public SpecialAction CreateSpecialAction(IOnHexTileObject owner)
        {
            if (OwnerTypeName != owner.TypeName) return null;


            return null;
        }
    }
}
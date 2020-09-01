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

        public string OwnerType { get; }

        private Closure _dynamicIsVisible;

        private Closure _dynamicIsAvailable;

        private Closure _dynamicDoSpecialAction;

        public bool Load(Script luaScript)
        {
            throw new NotImplementedException();
        }

        public SpecialAction CreateSpecialAction(IOnHexTileObject owner)
        {
            if (OwnerType != owner.Type.ToString()) return null;


            return null;
        }
    }
}
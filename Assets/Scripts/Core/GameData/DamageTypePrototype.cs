using MoonSharp.Interpreter;

namespace Core.GameData
{
    public sealed class DamageTypePrototype : ILuaHolder
    {
        public DamageTypePrototype(string filePath) => FilePath = filePath;

        public string IdentifierName { get; private set; }
        public string TypeName => "DamageType";

        public string FilePath { get; }

        public string Color { get; private set; }

        public bool Load(Script luaScript)
        {
            var t = luaScript.Globals;

            if (!t.TryGetString("Name", out var name,
                MoonSharpUtil.LoadingError("Name", FilePath)))
                return false;

            IdentifierName = name;

            if (!t.TryGetString("Color", out var color,
                MoonSharpUtil.LoadingError("Color", FilePath)))
                return false;

            Color = color;

            return true;
        }
    }
}

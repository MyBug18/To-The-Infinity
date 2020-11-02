using MoonSharp.Interpreter;

namespace Core.GameData
{
    public sealed class TileSpecialResourceTypePrototype : ILuaHolder
    {
        private TileSpecialResourceType _cache;

        public TileSpecialResourceTypePrototype(string filePath) => FilePath = filePath;

        public string IdentifierName { get; private set; }

        public string TypeName => "TileSpecialResourceType";

        public string FilePath { get; }

        public bool Load(Script luaScript)
        {
            IdentifierName = luaScript.Globals.Get("Name").String;

            _cache = new TileSpecialResourceType(IdentifierName);

            return true;
        }

        public TileSpecialResourceType Create() => _cache;
    }
}

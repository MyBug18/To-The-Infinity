using MoonSharp.Interpreter;

namespace Core.GameData
{
    public sealed class TileSpecialResourceTypePrototype : ILuaHolder
    {
        public string Name { get; private set; }

        public string TypeName => "TileSpecialResourceType";

        public string FilePath { get; }

        private TileSpecialResourceType _cache;

        public TileSpecialResourceTypePrototype(string filePath)
        {
            FilePath = filePath;
        }

        public bool Load(Script luaScript)
        {
            Name = luaScript.Globals.Get("Name").String;

            _cache = new TileSpecialResourceType(Name);

            return true;
        }

        public TileSpecialResourceType Create() => _cache;
    }
}

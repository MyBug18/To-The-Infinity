using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace Core.GameData
{
    public class TileSpecialResourceTypePrototype : ILuaHolder
    {
        public string Name { get; private set; }

        public int MoveCost { get; private set; }

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

            MoveCost = (int) luaScript.Globals.Get("MoveCost").Number;

            _cache = new TileSpecialResourceType(Name, MoveCost);
            return true;
        }

        public TileSpecialResourceType Create() => _cache;
    }
}
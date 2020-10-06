using MoonSharp.Interpreter;
using System.Collections.Generic;

namespace Core.GameData
{
    public sealed class ResourcePrototype : ILuaHolder
    {
        private static readonly Dictionary<string, ResourceType> StringTypeMap = new Dictionary<string, ResourceType>
        {
            {"Planetary", ResourceType.PlanetaryResource},
            {"Global", ResourceType.GlobalResource},
            {"Research", ResourceType.Research},
        };

        public string Name { get; private set; }

        public string TypeName => "Resource";

        public ResourceType ResourceType { get; private set; }

        public bool IsBasic { get; private set; }

        public int MaxAmount { get; private set; }

        public string FilePath { get; }

        public ResourcePrototype(string filePath)
        {
            FilePath = filePath;
        }

        public bool Load(Script luaScript)
        {
            Name = luaScript.Globals.Get("Name").String;

            if (!StringTypeMap.TryGetValue(luaScript.Globals.Get("ResourceType").String, out var value))
                return false;

            ResourceType = value;

            IsBasic = luaScript.Globals.Get("IsBasic").Boolean;

            MaxAmount = (int)luaScript.Globals.Get("MaxAmount").Number;

            return true;
        }
    }
}

using System.Collections.Generic;
using MoonSharp.Interpreter;

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

        public ResourcePrototype(string filePath) => FilePath = filePath;

        public ResourceType ResourceType { get; private set; }

        public bool IsBasic { get; private set; }

        public int MaxAmount { get; private set; }

        public string IdentifierName { get; private set; }

        public string TypeName => "Resource";

        public string FilePath { get; }

        public bool Load(Script luaScript)
        {
            var t = luaScript.Globals;

            if (!t.TryGetString("Name", out var identifierName,
                MoonSharpUtil.LoadingError("Name", FilePath)))
                return false;

            IdentifierName = identifierName;

            if (!t.TryGetString("ResourceType", out var resourceType,
                MoonSharpUtil.LoadingError("ResourceType", FilePath)))
                return false;

            if (!StringTypeMap.TryGetValue(resourceType, out var value))
            {
                Logger.Log(LogType.Error, FilePath, "ResourceType must be one of \"Planetary\", \"Global\", \"Research\"!");
                return false;
            }

            ResourceType = value;

            if (!t.TryGetBool("IsBasic", out var isBasic,
                MoonSharpUtil.LoadingError("IsBasic", FilePath)))
                return false;

            IsBasic = isBasic;

            if (!t.TryGetInt("MaxAmount", out var maxAmount,
                MoonSharpUtil.LoadingError("MaxAmount", FilePath)))
                return false;

            MaxAmount = maxAmount;

            return true;
        }
    }
}

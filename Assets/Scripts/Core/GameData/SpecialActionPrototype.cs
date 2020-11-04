using System.Collections.Generic;
using System.Linq;
using MoonSharp.Interpreter;

namespace Core.GameData
{
    public sealed class SpecialActionPrototype : ILuaHolder
    {
        private SpecialActionCore _cache;

        public SpecialActionPrototype(string filePath) => FilePath = filePath;

        public string IdentifierName { get; private set; }

        public string TypeName => "SpecialAction";

        public string FilePath { get; }

        public bool Load(Script luaScript)
        {
            var t = luaScript.Globals;

            if (!t.TryGetString("Name", out var identifierName,
                MoonSharpUtil.LoadingError("Name", FilePath)))
                return false;

            IdentifierName = identifierName;

            if (!t.TryGetString("TargetType", out var targetType,
                MoonSharpUtil.LoadingError("TargetType", FilePath)))
                return false;

            var needCoordinate = luaScript.Globals.Get("NeedCoordinate").Boolean;

            if (!t.TryGetLuaFunc<bool>("IsAvailable", out var isAvailable,
                MoonSharpUtil.LoadingError("IsAvailable", FilePath)))
                return false;

            if (!t.TryGetLuaFunc<HashSet<HexTileCoord>>("GetAvailableTiles", out var getAvailableTiles,
                MoonSharpUtil.LoadingError("GetAvailableTiles", FilePath)))
                return false;

            if (!t.TryGetLuaFunc<HashSet<HexTileCoord>>("PreviewEffectRange", out var previewEffectRange,
                MoonSharpUtil.LoadingError("PreviewEffectRange", FilePath)))
                return false;

            if (!t.TryGetLuaFunc<Dictionary<string, int>>("GetCost", out var getCost,
                MoonSharpUtil.LoadingError("GetCost", FilePath)))
                return false;

            if (!t.TryGetLuaFunc<bool>("DoAction", out var doAction,
                MoonSharpUtil.LoadingError("DoAction", FilePath)))
                return false;

            _cache = new SpecialActionCore(IdentifierName, targetType, needCoordinate, isAvailable, getAvailableTiles,
                previewEffectRange, getCost, doAction);

            return true;
        }

        public SpecialAction Create(ISpecialActionHolder owner) => new SpecialAction(_cache, owner);
    }
}

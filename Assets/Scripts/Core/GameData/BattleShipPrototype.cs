using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace Core.GameData
{
    public sealed class BattleShipPrototype : ILuaHolder
    {
        public BattleShipPrototype(string filePath) => FilePath = filePath;

        public IReadOnlyCollection<string> Properties { get; private set; }

        public string AttackDamageType { get; private set; }

        public int BaseAttackPower { get; private set; }

        public int BaseMaxHp { get; private set; }

        public int BaseMaxMovePoint { get; private set; }

        public IReadOnlyDictionary<string, int> BaseResourceStorage { get; private set; }

        public IReadOnlyCollection<string> BasicModifiers { get; private set; }

        public IReadOnlyCollection<string> BasicSpecialActions { get; private set; }

        public string IdentifierName { get; private set; }
        public string TypeName => "BattleShip";

        public string FilePath { get; }

        public bool Load(Script luaScript)
        {
            var t = luaScript.Globals;

            if (!t.TryGetString("Name", out var name,
                MoonSharpUtil.LoadingError("Name", FilePath)))
                return false;

            IdentifierName = name;

            if (!t.TryGetTable("Properties", out var propertiesTable,
                MoonSharpUtil.LoadingError("Properties", FilePath)))
                return false;

            var properties = new HashSet<string>();

            foreach (var v in propertiesTable.Values)
            {
                if (!v.TryGetString(out var property,
                    MoonSharpUtil.LoadingError("Properties.Value", FilePath)))
                    return false;

                if (properties.Contains(property))
                {
                    Logger.Log(LogType.Warning, FilePath,
                        $"There is a duplicate value in Properties.Value ({property}), so it will be ignored");
                    continue;
                }

                properties.Add(property);
            }

            Properties = properties;

            if (!t.TryGetString("AttackDamageType", out var attackDamageType,
                MoonSharpUtil.LoadingError("AttackDamageType", FilePath)))
                return false;

            AttackDamageType = attackDamageType;

            if (!t.TryGetInt("BaseAttackPower", out var baseAttackPower,
                MoonSharpUtil.LoadingError("BaseAttackPower", FilePath)))
                return false;

            BaseAttackPower = baseAttackPower;

            if (!t.TryGetInt("BaseMaxHp", out var baseMaxHp,
                MoonSharpUtil.LoadingError("BaseMaxHp", FilePath)))
                return false;

            BaseMaxHp = baseMaxHp;

            if (!t.TryGetInt("BaseMaxMovePoint", out var baseMaxMovePoint,
                MoonSharpUtil.LoadingError("BaseMaxMovePoint", FilePath)))
                return false;

            BaseMaxMovePoint = baseMaxMovePoint;

            var modifiers = new HashSet<string>();

            if (t.TryGetTable("BasicModifiers", out var modifiersTable,
                MoonSharpUtil.AllowNotDefined("BasicModifiers", FilePath)))
                foreach (var v in modifiersTable.Values)
                {
                    if (!v.TryGetString(out var modifier,
                        MoonSharpUtil.LoadingError("BasicModifiers.Value", FilePath)))
                        return false;

                    if (modifiers.Contains(modifier))
                    {
                        Logger.Log(LogType.Warning, FilePath,
                            $"There is a duplicate value in BasicModifiers.Value ({modifier}), so it will be ignored");
                        continue;
                    }

                    modifiers.Add(modifier);
                }

            BasicModifiers = modifiers;

            var specialActions = new HashSet<string>();

            if (t.TryGetTable("BasicSpecialActions", out var specialActionsTable,
                MoonSharpUtil.AllowNotDefined("BasicSpecialActions", FilePath)))
                foreach (var v in specialActionsTable.Values)
                {
                    if (!v.TryGetString(out var specialAction,
                        MoonSharpUtil.LoadingError("BasicSpecialActions.Value", FilePath)))
                        return false;

                    if (specialActions.Contains(specialAction))
                    {
                        Logger.Log(LogType.Warning, FilePath,
                            $"There is a duplicate value in BasicSpecialActions.Value ({specialAction}), so it will be ignored");
                        continue;
                    }

                    specialActions.Add(specialAction);
                }

            BasicSpecialActions = specialActions;

            var baseResourceStorage = new Dictionary<string, int>();

            if (t.TryGetTable("ResourceStorage", out var resourceStorageTable,
                MoonSharpUtil.AllowNotDefined("ResourceStorage", FilePath)))
                foreach (var kv in resourceStorageTable.Pairs)
                {
                    if (!kv.Key.TryGetString(out var resourceName,
                        MoonSharpUtil.LoadingError("ResourceStorage.Key", FilePath)))
                        return false;

                    if (baseResourceStorage.ContainsKey(resourceName))
                    {
                        Logger.Log(LogType.Warning, FilePath,
                            $"There is a duplicate value in ResourceStorage.Key ({properties}), so it will be ignored");
                        continue;
                    }

                    if (!kv.Value.TryGetInt(out var resourceAmount,
                        MoonSharpUtil.LoadingError("ResourceStorage.Value", FilePath)))
                        return false;

                    if (resourceAmount <= 0) continue;

                    baseResourceStorage[resourceName] = resourceAmount;
                }

            BaseResourceStorage = baseResourceStorage;

            return true;
        }
    }
}

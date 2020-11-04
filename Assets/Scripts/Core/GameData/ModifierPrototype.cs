﻿using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace Core.GameData
{
    public sealed class ModifierPrototype : ILuaHolder
    {
        private ModifierCore _cache;

        public ModifierPrototype(string filePath) => FilePath = filePath;

        public string IdentifierName { get; private set; }

        public string TypeName => "Modifier";

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

            // Set false if not defined
            if (!t.TryGetBool("IsTileLimited", out var isTileLimited,
                MoonSharpUtil.AllowNotDefined("IsTileLimited", FilePath)))
                isTileLimited = false;

            // Set empty if not defined
            if (!t.TryGetString("AdditionalDesc", out var additionalDesc,
                MoonSharpUtil.AllowNotDefined("AdditionalDesc", FilePath)))
                additionalDesc = string.Empty;

            var scopeDict = new Dictionary<string, ModifierScope>();

            if (!t.TryGetTable("Scope", out var scopes,
                MoonSharpUtil.LoadingError("Scope", FilePath)))
                return false;

            foreach (var nameTable in scopes.Pairs)
            {
                if (!nameTable.Key.TryGetString(out var typeName,
                    MoonSharpUtil.LoadingError("Scope.Key", FilePath)))
                    return false;

                if (!nameTable.Value.TryGetTable(out var scopeTable,
                    MoonSharpUtil.LoadingError("Scope.Value", FilePath)))
                    return false;

                // Returns empty effect list when not defined
                if (!scopeTable.TryGetLuaFunc<List<ModifierEffect>>("GetEffect", out var getEffect,
                    MoonSharpUtil.AllowNotDefined($"Scope.{typeName}.GetEffect", FilePath)))
                    getEffect = null;

                // Returns true when not defined
                if (!scopeTable.TryGetLuaFunc<bool>("CheckCondition", out var checkCondition,
                    MoonSharpUtil.AllowNotDefined($"Scope.{typeName}.CheckCondition", FilePath)))
                    checkCondition = null;

                // Set empty dictionary when not defined
                var triggerEvent = new Dictionary<string, ScriptFunctionDelegate>();

                if (scopeTable.TryGetTable("TriggerEvent", out var rawTriggerEvent,
                    MoonSharpUtil.AllowNotDefined($"Scope.{typeName}.TriggerEvent", FilePath)))
                {
                    foreach (var kv in rawTriggerEvent.Pairs)
                    {
                        if (!kv.Key.TryGetString(out var eventName,
                            MoonSharpUtil.LoadingError($"Scope.{typeName}.TriggerEvent.Key", FilePath)))
                            continue;

                        if (!kv.Value.TryGetLuaAction(out var eventFunc,
                            MoonSharpUtil.LoadingError($"Scope.{typeName}.TriggerEvent.Key", FilePath)))
                            continue;

                        if (triggerEvent.ContainsKey(eventName))
                        {
                            Logger.Log(LogType.Warning, $"Field Scope.{typeName}.TriggerEvent of" + FilePath,
                                $"Event type \"{eventName}\" has already defined, so it will be ignored.", true);
                            continue;
                        }

                        triggerEvent[eventName] = eventFunc;
                    }
                }

                var scope = new ModifierScope(typeName, getEffect, checkCondition, triggerEvent);

                scopeDict.Add(typeName, scope);
            }

            _cache = new ModifierCore(IdentifierName, targetType, isTileLimited, additionalDesc, scopeDict);

            return true;
        }

        public ModifierCore Create() => _cache;
    }
}

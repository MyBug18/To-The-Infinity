﻿using System;
using MoonSharp.Interpreter;
using System.Collections.Generic;
using System.Linq;

namespace Core.GameData
{
    public sealed class ModifierPrototype : ILuaHolder
    {
        public string Name { get; private set; }

        public string TypeName => "Modifier";

        private ModifierCore _cache;

        public string FilePath { get; }

        public ModifierPrototype(string filePath)
        {
            FilePath = filePath;
        }

        public bool Load(Script luaScript)
        {
            var t = luaScript.Globals;

            Name = t.Get("Name").String;

            var additionalDesc = t.Get("AdditionalDesc").String;

            var holderType = t.Get("TargetType").String;

            var scopeDict = new Dictionary<string, ModifierScope>();

            foreach (var nameTable in t.Get("Scope").Table.Pairs)
            {
                var name = nameTable.Key.String;
                var scopeTable = nameTable.Value.Table;

                var getEffect = scopeTable.Get("GetEffect").Function.GetDelegate<Dictionary<string, object>>();
                var checkCondition = scopeTable.Get("CheckCondition").Function.GetDelegate<bool>();
                var onAdded = scopeTable.Get("OnAdded").Function.GetDelegate();
                var onRemoved = scopeTable.Get("OnRemoved").Function.GetDelegate();

                var scope = new ModifierScope(name, ProcessEffect,
                    target => checkCondition.Invoke(target),
                    target => onAdded.Invoke(target),
                    target => onRemoved.Invoke(target));

                scopeDict.Add(name, scope);

                List<ModifierEffect> ProcessEffect(IModifierHolder target)
                {
                    var dict = getEffect.Invoke(target);
                    var data = GameDataStorage.Instance.GetGameData<ResourceData>();

                    return (from kv in dict
                        select new ModifierEffect(data.GetResourceDirectly(kv.Key), (int)kv.Value)).ToList();
                }
            }

            _cache = new ModifierCore(Name, holderType, additionalDesc, scopeDict);

            return true;
        }

        public ModifierCore Create() => _cache;
    }
}

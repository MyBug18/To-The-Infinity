﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core.GameData;
using MoonSharp.Interpreter;
using UnityEngine;
using Random = System.Random;

namespace Core
{
    public sealed class GameDataStorage
    {
        private static GameDataStorage _instance;

        private static readonly IReadOnlyDictionary<string, Func<string, ILuaHolder>> LuaHolderMaker =
            new Dictionary<string, Func<string, ILuaHolder>>
            {
                {"BattleShip", path => new BattleShipPrototype(path)},
                {"DamageType", path => new DamageTypePrototype(path)},
                {"HexTile", path => new HexTilePrototype(path)},
                {"Modifier", path => new ModifierPrototype(path)},
                {"PopSlot", path => new PopSlotPrototype(path)},
                {"Resource", path => new ResourcePrototype(path)},
                {"SpecialAction", path => new SpecialActionPrototype(path)},
                {"TileMap", path => new TileMapPrototype(path)},
                {"TileSpecialResourceType", path => new TileSpecialResourceTypePrototype(path)},
            };

        private readonly IReadOnlyDictionary<string, IGameData> _allData =
            new Dictionary<string, IGameData>
            {
                {"BattleShip", new BattleShipData()},
                {"DamageType", new DamageTypeData()},
                {"HexTile", new HexTileData()},
                {"Modifier", new ModifierData()},
                {"PopSlot", new PopSlotData()},
                {"Resource", new ResourceData()},
                {"SpecialAction", new SpecialActionData()},
                {"TileMap", new TileMapData()},
                {"TileSpecialResourceType", new TileSpecialResourceTypeData()},
            };

        public static GameDataStorage Instance => _instance ??= new GameDataStorage();

        public void Initialize()
        {
            InitializeMoonSharp();
            Logger.Instance.Initialize();

            var pathList = Directory.GetFiles(Path.Combine(Application.streamingAssetsPath, "CoreData"),
                "*.lua", SearchOption.AllDirectories);

            var luaHolderList = new ILuaHolder[pathList.Length];

            Parallel.For(0, luaHolderList.Length, i =>
            {
                var path = pathList[i];

                var script = new Script();

                script.Globals["Logger"] = Logger.Instance;

                try
                {
                    script.DoString(File.ReadAllText(path));
                }
                catch (Exception e)
                {
                    Logger.Log(LogType.Error, $"While loading {path}",
                        "Critical Lua error happened while loading, so it will be ignored! Error message: " +
                        e.Message);
                    luaHolderList[i] = null;
                    return;
                }

                if (!script.Globals.TryGetString("Type", out var typeName,
                    MoonSharpUtil.LoadingError("Type", path)))
                    return;

                if (!LuaHolderMaker.TryGetValue(typeName, out var maker)) return;

                var luaHolder = maker(path);

                if (!luaHolder.Load(script))
                {
                    Logger.Log(LogType.Error, path, "There was error on loading, so it will not be loaded!");

                    luaHolderList[i] = null;
                    return;
                }

                luaHolderList[i] = luaHolder;
            });

            foreach (var luaHolder in luaHolderList)
            {
                // null means there were errors when loading lua script
                if (luaHolder == null) continue;

                _allData[luaHolder.TypeName].AddNewData(luaHolder);
            }

            foreach (var kv in _allData)
            {
                if (!kv.Value.HasDefaultValue)
                    Logger.Log(LogType.Warning, "", $"{kv.Key} has no default value. It may result a serious problem!");
            }
        }

        private static void InitializeMoonSharp()
        {
#if UNITY_EDITOR
            UserData.RegisterType<Random>();
            UserData.RegisterAssembly();
#endif

            // HardWireType.Initialize();

            SetCustomConverters();
        }

        private static void SetCustomConverters()
        {
            var customConverters = Script.GlobalOptions.CustomConverters;

            customConverters.SetScriptToClrCustomConversion(
                DataType.Table, typeof(HexTileCoord), v =>
                    new HexTileCoord((int)(double)v.Table[1], (int)(double)v.Table[2]));

            customConverters.SetScriptToClrCustomConversion(
                DataType.Table, typeof(HashSet<HexTileCoord>), v =>
                {
                    var result = new HashSet<HexTileCoord>();

                    foreach (var kv in v.Table.Pairs)
                    {
                        var coord = new HexTileCoord((int)(double)kv.Value.Table[1], (int)(double)kv.Value.Table[2]);
                        result.Add(coord);
                    }

                    return result;
                });

            customConverters.SetScriptToClrCustomConversion(
                DataType.Table, typeof(List<ModifierEffect>), v =>
                {
                    var result = new List<ModifierEffect>();

                    foreach (var kv in v.Table.Pairs)
                    {
                        var additionalInfo = new List<string>();
                        var tokens = kv.Key.String.Split('_');
                        if (!Enum.TryParse(tokens[0], out ModifierEffectType type))
                        {
                            Logger.Log(LogType.Warning, "",
                                $"{tokens[0]} is not a valid {nameof(ModifierEffectType)}, so it will be ignored!");

                            type = ModifierEffectType.Default;
                        }

                        for (var i = 1; i < tokens.Length; i++)
                            additionalInfo.Add(tokens[i]);

                        result.Add(new ModifierEffect(type, additionalInfo, (int)kv.Value.Number));
                    }

                    return result;
                });
        }
    }
}

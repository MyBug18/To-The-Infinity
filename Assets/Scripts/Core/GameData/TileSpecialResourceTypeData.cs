using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace Core.GameData
{
    public class TileSpecialResourceType
    {
        public readonly string Name;

        public readonly int MoveCost;

        public TileSpecialResourceType(string name, int moveCost)
        {
            Name = name;
            MoveCost = moveCost;
        }
    }

    public class TileSpecialResourceTypeData : IGameData
    {
        private TileSpecialResourceTypeHolder _holder;

        public bool HasDefaultValue => _holder.Default != null;

        public void AddNewData(ILuaHolder luaHolder)
        {
            if (!(luaHolder is TileSpecialResourceTypeHolder tsrth)) return;

            _holder = tsrth;
        }

        public void OnGameInitialized(Game game)
        {
            throw new System.NotImplementedException();
        }
    }

    public class TileSpecialResourceTypeHolder : ILuaHolder
    {
        public string Name => "TileSpecialResourceTypeHolder";

        public string TypeName => "TileSpecialResourceType";

        public string FilePath { get; }

        public TileSpecialResourceType Default { get; private set; }

        public IReadOnlyDictionary<string, TileSpecialResourceType> Data { get; private set; }

        public TileSpecialResourceTypeHolder(string filePath)
        {
            FilePath = filePath;
        }

        public bool Load(Script luaScript)
        {
            var data = new Dictionary<string, TileSpecialResourceType>();

            foreach (var kv in luaScript.Globals.Pairs)
            {
                if (kv.Key.String == "Type") continue;

                var name = kv.Key.String;

                var infos = kv.Value.Table;

                var value = new TileSpecialResourceType(name, (int) infos.Get("MoveCost").Number);

                if (name == "Default")
                {
                    Default = value;
                    continue;
                }

                if (data.ContainsKey(name)) continue;
                data[name] = value;
            }

            Data = data;
            return true;
        }
    }
}
using System.Collections.Generic;

namespace Core.GameData
{
    public sealed class PopSlotData : IGameData
    {
        private readonly Dictionary<string, PopSlotPrototype> _data = new Dictionary<string, PopSlotPrototype>();

        private PopSlotPrototype _default;

        public bool HasDefaultValue => _default != null;

        public HashSet<string> AllJobName { get; } = new HashSet<string>();

        public Dictionary<string, List<string>> AllGroupInfo { get; } = new Dictionary<string, List<string>>();

        public void AddNewData(ILuaHolder luaHolder)
        {
            if (!(luaHolder is PopSlotPrototype psp)) return;

            if (_data.ContainsKey(psp.IdentifierName)) return;

            if (psp.IdentifierName == "Default")
            {
                _default = psp;
                return;
            }

            _data[psp.IdentifierName] = psp;
            AllJobName.Add(psp.IdentifierName);

            if (!AllGroupInfo.ContainsKey(psp.Group))
                AllGroupInfo.Add(psp.Group, new List<string>());

            AllGroupInfo[psp.Group].Add(psp.IdentifierName);
        }

        public void OnGameInitialized(Game _)
        {
        }
    }
}

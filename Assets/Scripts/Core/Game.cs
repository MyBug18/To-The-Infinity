using System.Collections.Generic;

namespace Core
{
    public sealed class Game : IInfinityObject
    {
        private readonly Dictionary<string, IInfinityObject> _guidObjectMap = new Dictionary<string, IInfinityObject>();

        private readonly Dictionary<string, Player> _playerMap = new Dictionary<string, Player>();

        public Game(int gameSpeed)
        {
            Instance = this;

            var guid = System.Guid.NewGuid().ToString();

            Guid = guid;
            _guidObjectMap[guid] = this;

            GameSpeed = gameSpeed;
        }

        public static Game Instance { get; private set; }

        public Player OwnPlayer { get; }

        /// <summary>
        ///     The controller player of the turn.
        ///     Should not be null when the Game instance is alive.
        /// </summary>
        public Player PlayerCurrentInControl { get; private set; }

        /// <summary>
        ///     Player of this Game instance
        /// </summary>
        public Player Me { get; }

        public int GameSpeed { get; }

        public IReadOnlyList<StarSystem> StarSystems { get; }

        public string TypeName => nameof(Game);

        public string Guid { get; }

        public LuaDictWrapper Storage { get; } = new LuaDictWrapper(new Dictionary<string, object>());

        public void StartNewTurn()
        {
            foreach (var s in StarSystems) s.StartNewTurn(GameSpeed);
        }

        public bool IsObjectExists(string guid) => _guidObjectMap.ContainsKey(guid);

        public IInfinityObject GetObject(string guid)
        {
            if (_guidObjectMap.TryGetValue(guid, out var result)) return result;

            Logger.Log(LogType.Warning, $"{nameof(Game)}.{nameof(GetObject)}",
                "Trying to get object which does not exist, so it will return nil!");
            return null;
        }

        public Player GetPlayer(string playerName)
        {
            if (_playerMap.TryGetValue(playerName, out var result)) return result;

            Logger.Log(LogType.Warning, $"{nameof(Game)}.{nameof(GetPlayer)}",
                "Trying to get player who does not exist, so it will return nil!");
            return null;
        }
    }
}

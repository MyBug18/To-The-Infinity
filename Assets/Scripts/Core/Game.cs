using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MoonSharp.Interpreter;

namespace Core
{
    public sealed class Game : IInfinityObject
    {
        private readonly Dictionary<string, IPlayer> _playerMap = new Dictionary<string, IPlayer>();

        public Game(int gameSpeed, int? gameId)
        {
            Instance = this;
            RegisterInfinityObject(this, true, gameId);
            TileMapNoiseMaker.InitializeGradSeed(gameId);

            GameSpeed = gameSpeed;
        }

        public static Game Instance { get; private set; }

        /// <summary>
        ///     The controller player of the turn.
        ///     Should not be null when the Game instance is alive.
        /// </summary>
        public IPlayer PlayerCurrentInControl { get; private set; }

        /// <summary>
        ///     Player of this Game instance
        ///     Would be different on different computer
        /// </summary>
        public IPlayer Me { get; }

        public int GameSpeed { get; }

        public IReadOnlyList<StarSystem> StarSystems { get; }

        // No one can own Game
        public IPlayer OwnPlayer => NoPlayer.Instance;

        public string TypeName => nameof(Game);

        public int Id { get; set; }

        public LuaDictWrapper Storage { get; } = new LuaDictWrapper(new Dictionary<string, object>());

        public void StartNewTurn(int week)
        {
            foreach (var s in StarSystems)
                s.StartNewTurn(week);
        }

        [MoonSharpHidden]
        public InfinityObjectData Save()
        {
            var result = new Dictionary<string, object>
            {
                ["Id"] = Id,
                ["Storage"] = Storage.Data,
                ["StarSystems"] = StarSystems.Select(x => x.Id).ToList(),
            };

            return new InfinityObjectData(TypeName, result);
        }

        public IPlayer GetPlayer(string playerName)
        {
            if (_playerMap.TryGetValue(playerName, out var result)) return result;

            Logger.Log(LogType.Warning, $"{nameof(Game)}.{nameof(GetPlayer)}",
                "Trying to get player who does not exist, so it will return nil!");
            return null;
        }

        #region IdHandling

        private readonly Dictionary<int, IInfinityObject> _guidObjectMap = new Dictionary<int, IInfinityObject>();

        private static readonly object Mutex = new object();

        private readonly Random _r = new Random();

        [MoonSharpHidden]
        public void RegisterInfinityObject(IInfinityObject obj, bool isRegistering, int? id = null)
        {
            lock (Mutex)
            {
                if (isRegistering)
                {
                    int newId;

                    if (id == null)
                    {
                        newId = _r.Next();

                        while (_guidObjectMap.ContainsKey(newId))
                            newId = _r.Next();
                    }
                    else
                    {
                        newId = id.Value;

                        // Should check duplicate key when the id is explicit
                        if (_guidObjectMap.ContainsKey(newId)) return;
                    }

                    obj.Id = newId;
                    _guidObjectMap[newId] = obj;
                }
                else
                {
                    _guidObjectMap.Remove(obj.Id);
                }
            }
        }

        [MoonSharpHidden]
        public IInfinityObject GetObject(int id) => _guidObjectMap.TryGetValue(id, out var result) ? result : null;

        #endregion

        #region ModifierEffectCaching

        private class ModifierCacheLock : IDisposable
        {
            private readonly Action _freeLock;

            public ModifierCacheLock(Action freeLock) => _freeLock = freeLock;

            public void Dispose()
            {
                _freeLock();
            }
        }

        private int _lockCount;

        public IDisposable GetCacheLock()
        {
            Interlocked.Add(ref _lockCount, 1);
            return new ModifierCacheLock(FreeLock);
        }

        private void StartCachingModifierEffects()
        {
            if (_lockCount > 0) return;

            foreach (var s in StarSystems)
                s.StartCachingModifierEffect();
        }

        private void FreeLock()
        {
            if (_lockCount == 0)
                throw new InvalidOperationException("Trying to free lock which doesn't exist anymore!");

            Interlocked.Add(ref _lockCount, -1);

            if (_lockCount == 0)
                StartCachingModifierEffects();
        }

        #endregion
    }
}

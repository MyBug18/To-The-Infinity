using System.Collections.Generic;

namespace Core
{
    public sealed class Game
    {
        public static Game Instance { get; private set; }

        public int GameSpeed { get; }

        public IReadOnlyList<StarSystem> StarSystems { get; }

        private HashSet<string> _enemyOwners = new HashSet<string>();

        public Game(int gameSpeed)
        {
            Instance = this;

            GameSpeed = gameSpeed;
        }

        public void StartNewTurn()
        {
            foreach (var s in StarSystems)
            {
                s.StartNewTurn(GameSpeed);
            }
        }

        public bool IsEnemy(string owner1, string owner2)
        {
            if (owner1 == owner2) return false;

            // MainEnemy can not be friendly with any other owner
            if (owner1 == "MainEnemy" || owner2 == "MainEnemy") return true;

            if (owner1 == "Me")
                return _enemyOwners.Contains(owner2);

            if (owner2 == "Me")
                return _enemyOwners.Contains(owner1);

            return false;
        }
    }
}

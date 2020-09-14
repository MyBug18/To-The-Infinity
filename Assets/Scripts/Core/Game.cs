using System.Collections.Generic;

namespace Core
{
    public class Game
    {
        public int GameSpeed { get; }

        public IReadOnlyList<StarSystem> StarSystems { get; }

        public Game(int gameSpeed)
        {
            GameSpeed = gameSpeed;
        }

        public void StartNewTurn()
        {
            foreach (var s in StarSystems)
            {
                s.StartNewTurn(GameSpeed);
            }
        }
    }
}

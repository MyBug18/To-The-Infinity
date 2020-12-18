using System;

namespace Core
{
    public sealed class RandomManager
    {
        private static RandomManager _instance;

        public static RandomManager Instance => _instance ??= new RandomManager();

        private Random _r;

        public void Initialize(int? seed)
        {
            _r = new Random(seed ?? new Random().Next());
        }

        public bool CheckByChance(double chance)
            => _r.NextDouble() < chance;
    }
}

using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Core
{
    public static class Noise2d
    {
        private static Random _random;
        private static int[] _permutation;

        private static Vector2[] _gradients;

        public static int Seed { get; private set; }

        public static void InitializeGradSeed(int? seed)
        {
            Seed = seed ?? UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            _random = new Random(Seed);
            CalculateGradients();
        }

        /// <summary>
        /// Seed must be same with the seed of tileMap
        /// </summary>
        /// <returns></returns>
        public static float[,] GenerateNoiseMap(int width, int height, int octaves, int? seed)
        {
            var result = new float[width, height];

            var min = float.MaxValue;
            var max = float.MinValue;

            CalculatePermutation(seed);

            var frequency = 5f;
            var amplitude = 1f;

            for (var octave = 0; octave < octaves; octave++)
            {
                var f = frequency;
                var a = amplitude;
                Parallel.For(0
                    , width * height
                    , offset =>
                    {
                        var i = offset % width;
                        var j = offset / width;
                        var noise = Noise(i * f * 1f / width, j * f * 1f / height);
                        noise = result[i, j] += noise * a;

                        min = Math.Min(min, noise);
                        max = Math.Max(max, noise);
                    }
                );

                frequency *= 2;
                amplitude /= 2;
            }

            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    result[i, j] = (result[i, j] - min) / (max - min);
                }
            }

            return result;
        }

        private static void CalculatePermutation(int? seed)
        {
            var r = seed == null ? new Random() : new Random(seed.Value);

            _permutation = Enumerable.Range(0, 256).ToArray();

            // shuffle the array
            for (var i = 0; i < _permutation.Length; i++)
            {
                var source = r.Next(_permutation.Length);

                var t = _permutation[i];
                _permutation[i] = _permutation[source];
                _permutation[source] = t;
            }
        }

        private static void CalculateGradients()
        {
            _gradients = new Vector2[256];

            for (var i = 0; i < _gradients.Length; i++)
            {
                Vector2 gradient;

                do
                {
                    gradient = new Vector2((float)(_random.NextDouble() * 2 - 1),
                        (float)(_random.NextDouble() * 2 - 1));
                } while (gradient.LengthSquared() >= 1);

                gradient = Vector2.Normalize(gradient);

                _gradients[i] = gradient;
            }
        }

        private static float Drop(float t)
        {
            t = Math.Abs(t);
            return 1f - t * t * t * (t * (t * 6 - 15) + 10);
        }

        private static float Q(float u, float v)
        {
            return Drop(u) * Drop(v);
        }

        private static float Noise(float x, float y)
        {
            var cell = new Vector2((float)Math.Floor(x), (float)Math.Floor(y));

            var total = 0f;

            var corners = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0), new Vector2(1, 1) };

            foreach (var n in corners)
            {
                var ij = cell + n;
                var uv = new Vector2(x - ij.X, y - ij.Y);

                var index = _permutation[(int)ij.X % _permutation.Length];
                index = _permutation[(index + (int)ij.Y) % _permutation.Length];

                var grad = _gradients[index % _gradients.Length];

                total += Q(uv.X, uv.Y) * Vector2.Dot(grad, uv);
            }

            return Math.Max(Math.Min(total, 1f), -1f);
        }
    }
}

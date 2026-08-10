using System;

namespace PowerMath.Gameplay.Combat
{
    public interface IRandomSource
    {
        int NextInclusive(int minimum, int maximum);

        double NextUnit();
    }

    public sealed class SeededRandomSource : IRandomSource
    {
        private readonly Random _random;

        public SeededRandomSource(int seed)
        {
            _random = new Random(seed);
        }

        public int NextInclusive(int minimum, int maximum)
        {
            if (maximum < minimum)
            {
                throw new ArgumentOutOfRangeException(nameof(maximum));
            }

            return _random.Next(minimum, maximum + 1);
        }

        public double NextUnit()
        {
            return _random.NextDouble();
        }
    }
}

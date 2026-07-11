using System;

namespace NeighborhoodManager.Utilities
{
    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random random;

        public SystemRandomSource(int? seed = null)
        {
            random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public int Range(int minimumInclusive, int maximumExclusive)
        {
            return random.Next(minimumInclusive, maximumExclusive);
        }

        public float Range(float minimumInclusive, float maximumInclusive)
        {
            return minimumInclusive + ((float)random.NextDouble() * (maximumInclusive - minimumInclusive));
        }
    }
}

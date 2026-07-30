namespace FlyingFox.Core
{
    /// <summary>
    /// SplitMix64 — public-domain algorithm (Steele, Lea, Flood).
    /// Design KD14: Daily and seeded Classic use this only.
    /// </summary>
    public sealed class SplitMix64Rng : IRng
    {
        private ulong _state;

        public SplitMix64Rng() => Reseed(0);

        public SplitMix64Rng(int seed) => Reseed(seed);

        public void Reseed(int seed)
        {
            // Design: sign-extend via unchecked((ulong)(uint)seed) then mix once
            _state = Mix(unchecked((ulong)(uint)seed));
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive)
                throw new System.ArgumentException("min must be < max");
            uint range = (uint)(maxExclusive - minInclusive);
            // Rejection sampling for uniformity
            ulong x = NextULong();
            uint multi = (uint)(((x >> 32) * range) >> 32);
            return minInclusive + (int)multi;
        }

        public float NextFloat()
        {
            // [0, 1)
            return (NextULong() >> 40) * (1f / (1UL << 24));
        }

        public ulong NextULong()
        {
            _state += 0x9E3779B97F4A7C15UL;
            return Mix(_state);
        }

        private static ulong Mix(ulong z)
        {
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}

namespace FlyingFox.Core
{
    /// <summary>Deterministic RNG for Classic seeds and Daily. Never use System.Random for Daily.</summary>
    public interface IRng
    {
        void Reseed(int seed);
        int Next(int minInclusive, int maxExclusive);
        float NextFloat();
    }
}

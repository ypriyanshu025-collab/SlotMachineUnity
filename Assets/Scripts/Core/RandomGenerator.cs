using System;

namespace SlotMachine.Core
{
    /// <summary>
    /// Thin wrapper around System.Random used as the single source of
    /// randomness for every RNG-driven decision in the game (reel results,
    /// gamble coin-flips). Centralising it here — rather than sprinkling
    /// UnityEngine.Random calls throughout the codebase — makes the RNG
    /// swappable and, more importantly, seedable: passing an explicit seed
    /// produces a fully reproducible sequence of spins, which is valuable
    /// both for automated testing and for verifying payout fairness over a
    /// large number of simulated spins.
    /// </summary>
    public class RandomGenerator
    {
        private readonly Random _random;

        public RandomGenerator()
        {
            // Time-based seed mixed with a Guid for extra entropy so two
            // instances created in the same tick still diverge.
            int seed = Environment.TickCount ^ Guid.NewGuid().GetHashCode();
            _random = new Random(seed);
        }

        public RandomGenerator(int seed)
        {
            _random = new Random(seed);
        }

        /// <summary>Returns an integer in [minInclusive, maxExclusive).</summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            return _random.Next(minInclusive, maxExclusive);
        }

        /// <summary>Returns a float in [0, 1).</summary>
        public float NextFloat01()
        {
            return (float)_random.NextDouble();
        }

        /// <summary>Returns true with the given probability (0..1).</summary>
        public bool NextBool(float probability = 0.5f)
        {
            return NextFloat01() < probability;
        }
    }
}

using System.Collections.Generic;
using SlotMachine.Core;
using UnityEngine;

namespace SlotMachine.Data
{
    /// <summary>
    /// Holds the full set of symbols in play and provides weighted-random
    /// selection. Centralising this in one ScriptableObject means the RNG
    /// fairness logic (see SlotMachine.Core.RandomGenerator) only has to be
    /// written once and is trivially unit-testable in isolation.
    /// </summary>
    [CreateAssetMenu(fileName = "SymbolDatabase", menuName = "Slot Machine/Symbol Database", order = 0)]
    public class SymbolDatabase : ScriptableObject
    {
        public List<SymbolData> symbols = new List<SymbolData>();

        private int _totalWeight = -1;

        /// <summary>Sum of every symbol's weight. Cached after first access.</summary>
        public int TotalWeight
        {
            get
            {
                if (_totalWeight < 0)
                {
                    _totalWeight = 0;
                    foreach (var s in symbols)
                    {
                        _totalWeight += Mathf.Max(0, s.weight);
                    }
                }
                return _totalWeight;
            }
        }

        public SymbolData GetByType(SlotSymbolType type)
        {
            foreach (var s in symbols)
            {
                if (s.symbolType == type) return s;
            }
            return null;
        }

        /// <summary>
        /// Picks a random symbol using each symbol's configured weight, via
        /// the supplied RNG. Kept independent of Unity's Random class so the
        /// result is fully deterministic when a seeded RandomGenerator is
        /// used (useful for automated testing / replaying a spin).
        /// </summary>
        public SymbolData PickWeighted(RandomGenerator rng)
        {
            if (symbols == null || symbols.Count == 0) return null;

            int roll = rng.NextInt(0, TotalWeight);
            int cumulative = 0;
            foreach (var s in symbols)
            {
                cumulative += Mathf.Max(0, s.weight);
                if (roll < cumulative)
                {
                    return s;
                }
            }
            return symbols[symbols.Count - 1];
        }
    }
}

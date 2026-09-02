using System;
using SlotMachine.Core;

namespace SlotMachine.Bonus
{
    /// <summary>
    /// Classic "Double or Nothing" gamble feature: after a win, the player
    /// may risk their winnings on a coin-flip for a chance to double them.
    /// This is a common bonus round in real-world slot machines and is
    /// offered here as the creative bonus feature called out in the
    /// assignment brief. Pure logic class — the popup UI (Yes/No prompt)
    /// is built and driven by SlotMachine.UI.UIManager, which calls into
    /// this class rather than owning any of its rules.
    /// </summary>
    public class GambleManager
    {
        private readonly RandomGenerator _rng;
        private readonly float _winChance;
        private readonly int _maxRounds;

        public int RoundsPlayed { get; private set; }
        public bool RoundsExhausted => RoundsPlayed >= _maxRounds;

        public GambleManager(RandomGenerator rng, float winChance, int maxRounds)
        {
            _rng = rng;
            _winChance = winChance;
            _maxRounds = maxRounds;
        }

        public void ResetRounds()
        {
            RoundsPlayed = 0;
        }

        /// <summary>
        /// Resolves a single gamble attempt. Returns true if the player
        /// won (their stake should be doubled), false if they lost it.
        /// </summary>
        public bool PlayRound()
        {
            RoundsPlayed++;
            return _rng.NextBool(_winChance);
        }
    }
}

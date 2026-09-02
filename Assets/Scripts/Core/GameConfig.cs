using System;
using UnityEngine;

namespace SlotMachine.Core
{
    /// <summary>
    /// Every designer-tunable number in one place. Marked [Serializable] so
    /// it shows up as an editable block in the Inspector on whichever
    /// MonoBehaviour holds it (SlotMachineController), letting a reviewer
    /// tweak balance live in Play Mode without touching code.
    /// </summary>
    [Serializable]
    public class GameConfig
    {
        [Header("Economy")]
        public float startingCredits = 1000f;
        public float defaultBet = 10f;
        public float minBet = 10f;
        public float maxBet = 100f;
        public float betStep = 10f;

        [Header("Paylines")]
        [Tooltip("1 = middle row only (strict literal 'all slots match'), 3 = top+middle+bottom evaluated independently.")]
        public int paylineCount = 3;
        public bool enableNearMissBonus = true;

        [Header("Bonus Features")]
        public bool enableGambleFeature = true;
        [Tooltip("Odds of winning a single gamble attempt (0..1). 0.5 = a fair coin flip.")]
        public float gambleWinChance = 0.5f;
        public int maxGambleRounds = 3;

        [Header("Reel Timing")]
        public float staggerDelayPerReel = 0.15f;
    }
}

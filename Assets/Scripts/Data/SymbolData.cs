using SlotMachine.Core;
using UnityEngine;

namespace SlotMachine.Data
{
    /// <summary>
    /// Data-driven definition of a single slot symbol: its type, its sprite,
    /// how likely it is to land (RNG weight) and how much it pays out.
    /// Using a ScriptableObject keeps game-balance tuning entirely in data
    /// (designers/reviewers can create/edit these in the Editor Inspector
    /// without touching code) rather than hard-coding numbers in scripts.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSymbolData", menuName = "Slot Machine/Symbol Data", order = 1)]
    public class SymbolData : ScriptableObject
    {
        [Tooltip("Which logical symbol this asset represents.")]
        public SlotSymbolType symbolType;

        [Tooltip("The sprite shown on the reel for this symbol.")]
        public Sprite sprite;

        [Tooltip("Relative RNG weight. Higher = lands more often. Weights do not need to sum to any particular total; they are normalised at runtime.")]
        [Min(1)]
        public int weight = 10;

        [Tooltip("Payout multiplier applied to the current bet when 3 of this symbol land on a payline.")]
        [Min(0f)]
        public float payoutMultiplier = 1f;

        [Tooltip("If true, this symbol can substitute for any other symbol to help complete a winning payline (classic 'Wild' behaviour).")]
        public bool isWild = false;

        [Tooltip("Friendly name shown in the on-screen paytable legend.")]
        public string displayName = "Symbol";
    }
}

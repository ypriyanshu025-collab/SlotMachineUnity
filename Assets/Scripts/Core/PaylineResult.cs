using SlotMachine.Data;

namespace SlotMachine.Core
{
    /// <summary>
    /// Result of evaluating a single payline (one horizontal row across the
    /// three reels). Plain data holder returned by PayoutManager so the UI
    /// layer can both total up credits and highlight exactly which rows won.
    /// </summary>
    public struct PaylineResult
    {
        public int rowIndex;
        public bool isWin;
        public bool isNearMiss;
        public float payoutAmount;
        public SymbolData matchedSymbol;

        public static PaylineResult None(int rowIndex)
        {
            return new PaylineResult
            {
                rowIndex = rowIndex,
                isWin = false,
                isNearMiss = false,
                payoutAmount = 0f,
                matchedSymbol = null
            };
        }
    }
}

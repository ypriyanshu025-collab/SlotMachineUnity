using System.Collections.Generic;
using SlotMachine.Data;

namespace SlotMachine.Core
{
    /// <summary>
    /// Pure game-logic class (no MonoBehaviour, no Unity engine calls other
    /// than the SymbolData assets it is handed) responsible for deciding
    /// whether a spin's result is a win and how much it pays. Kept separate
    /// from SlotMachineController so the win rules can be unit tested and
    /// reasoned about independently of animation/UI concerns.
    ///
    /// Core rule (per the assignment spec): a payline wins when all of its
    /// slots show the same symbol. This project treats each horizontal row
    /// across the three reels as one payline (the classic slot-machine
    /// reading of "reel row = a set of slots"); the middle row is the
    /// primary line and, with <see cref="paylineCount"/> set to 3, the top
    /// and bottom rows are additionally evaluated as a bonus feature so a
    /// single spin can pay out on more than one line at once.
    /// </summary>
    public class PayoutManager
    {
        /// <summary>How many horizontal rows to evaluate: 1 = middle row only (strict literal reading), 3 = top+middle+bottom.</summary>
        public int paylineCount = 3;

        /// <summary>
        /// Bonus feature: pay a small consolation when exactly two of the
        /// three slots on a line match (no third match, no wild present).
        /// </summary>
        public bool enableNearMissBonus = true;
        public float nearMissPayoutFactor = 0.4f;

        /// <summary>
        /// Evaluates every active payline for the given 3x3 grid of landed
        /// symbols. grid[reelIndex][rowIndex] holds the symbol currently
        /// shown by that reel in that row (rowIndex 0 = top, 2 = bottom).
        /// </summary>
        public List<PaylineResult> EvaluateSpin(SymbolData[][] grid, float betAmount)
        {
            var results = new List<PaylineResult>();

            // Row 1 (index 1) is the middle/primary payline and is always
            // evaluated. Rows 0 and 2 are only evaluated when paylineCount == 3.
            int[] rowsToCheck = paylineCount >= 3
                ? new[] { 0, 1, 2 }
                : new[] { 1 };

            foreach (int row in rowsToCheck)
            {
                results.Add(EvaluateRow(grid, row, betAmount));
            }

            return results;
        }

        private PaylineResult EvaluateRow(SymbolData[][] grid, int row, float betAmount)
        {
            SymbolData a = grid[0][row];
            SymbolData b = grid[1][row];
            SymbolData c = grid[2][row];

            int wildCount = 0;
            var nonWild = new List<SymbolData>();
            foreach (var s in new[] { a, b, c })
            {
                if (s != null && s.isWild) wildCount++;
                else if (s != null) nonWild.Add(s);
            }

            // 3 wilds together = the jackpot, paid at the wild symbol's own rate.
            if (wildCount == 3)
            {
                return WinResult(row, a, betAmount, 1f);
            }

            // 2 wilds + 1 other symbol: wilds substitute, line pays as if it
            // were 3-of-that-symbol.
            if (wildCount == 2 && nonWild.Count == 1)
            {
                return WinResult(row, nonWild[0], betAmount, 1f);
            }

            // 1 wild + 2 matching others: wild substitutes for the missing third.
            if (wildCount == 1 && nonWild.Count == 2 && SameSymbol(nonWild[0], nonWild[1]))
            {
                return WinResult(row, nonWild[0], betAmount, 1f);
            }

            // No wilds: all three slots must be the literal same symbol.
            if (wildCount == 0 && SameSymbol(a, b) && SameSymbol(b, c))
            {
                return WinResult(row, a, betAmount, 1f);
            }

            // Bonus: near-miss consolation payout when exactly two of the
            // three (non-wild) slots match.
            if (enableNearMissBonus)
            {
                SymbolData pairMatch = FindPairMatch(a, b, c);
                if (pairMatch != null)
                {
                    var near = WinResult(row, pairMatch, betAmount, nearMissPayoutFactor);
                    near.isNearMiss = true;
                    return near;
                }
            }

            return PaylineResult.None(row);
        }

        private static SymbolData FindPairMatch(SymbolData a, SymbolData b, SymbolData c)
        {
            if (SameSymbol(a, b) && !SameSymbol(b, c)) return a;
            if (SameSymbol(b, c) && !SameSymbol(a, b)) return b;
            if (SameSymbol(a, c) && !SameSymbol(a, b)) return a;
            return null;
        }

        private static bool SameSymbol(SymbolData x, SymbolData y)
        {
            if (x == null || y == null) return false;
            return x.symbolType == y.symbolType;
        }

        private static PaylineResult WinResult(int row, SymbolData symbol, float betAmount, float factor)
        {
            return new PaylineResult
            {
                rowIndex = row,
                isWin = true,
                isNearMiss = false,
                payoutAmount = betAmount * symbol.payoutMultiplier * factor,
                matchedSymbol = symbol
            };
        }
    }
}

namespace SlotMachine.Core
{
    /// <summary>
    /// The four symbols supplied in the art pack. Kept as a simple enum so
    /// win-checking code can compare symbols cheaply without string/asset
    /// lookups; the visual + payout data for each value lives in a
    /// corresponding SymbolData asset (see SlotMachine.Data).
    /// </summary>
    public enum SlotSymbolType
    {
        Bar = 0,
        Cherry = 1,
        Bell = 2,
        Seven = 3 // Highest paying symbol; also acts as the Wild symbol.
    }
}

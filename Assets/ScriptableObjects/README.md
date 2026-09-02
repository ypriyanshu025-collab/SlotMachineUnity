# ScriptableObjects

By default, `GameBootstrapper` builds a `SymbolDatabase` (see
`Assets/Scripts/Data/SymbolDatabase.cs` and `SymbolData.cs`) entirely in code
at runtime, so the game works out of the box with no assets required here.

To tune symbol weights/payouts from the Inspector instead of code: right
click in this folder → `Create > Slot Machine > Symbol Database`, add four
`Create > Slot Machine > Symbol Data` assets to it (one per
`SlotSymbolType`), then drag the database onto the `Symbol Database` field of
the `GameBootstrapper` component on the `GameBootstrap` object in
`MainScene` — it overrides the built-in default.

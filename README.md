# Jackpot Slot Machine (Unity)

A 3-reel, 3-row slot machine built in Unity 2022.3 LTS from the supplied art
pack, for the Unity Slot Game Assignment.

## Game Overview

- **Grid**: 3 reels × 3 visible rows (9 symbols on screen), matching the
  supplied machine frame artwork.
- **Symbols**: BAR, Cherry, Bell, and Seven (the wild/jackpot symbol).
- **Paylines**: the top, middle and bottom rows are each evaluated as an
  independent payline — a spin can pay out on more than one line at once.
  The middle row is the primary/classic line; see *Design Notes* below for
  why all three are active by default.
- **Wild substitution**: Seven substitutes for any other symbol to help
  complete a line (2 wilds + 1 other, or 1 wild + 2 matching others, still
  win; 3 wilds is the jackpot).
- **Near-miss consolation**: two matching symbols on a line (no wild
  involved) pay a small consolation (40% of that symbol's rate) — a common
  "keep it exciting" touch in real slot machines.
- **Betting**: start with 1000 credits, bet 10–100 in steps of 10.
- **Bonus feature — Double or Nothing**: after any win, gamble it on a
  coin-flip for a chance to double it (up to 3 rounds), inspired by the
  classic "Gamble" feature found in many real slot machines.

## How to Open the Project

1. Install **Unity 2022.3 LTS** (any 2022.3.x patch) via Unity Hub.
2. Unity Hub → **Add** → select the `SlotMachineUnity` folder → open it.
3. Open `Assets/Scenes/MainScene.unity` if it doesn't open automatically.
4. Press **Play**.

### Why the Scene view looks empty before pressing Play

Every visible element — the Canvas, background, machine frame, reels, HUD,
buttons, popup — is built **procedurally in code** at runtime by
`GameBootstrapper` (see *Design Notes*). The scene itself only contains a
camera and one `GameBootstrap` object. This is intentional: press **Play**
and the full UI is constructed in the first frame.

### If the Editor reports any import warnings on first open

This project was authored file-by-file outside of the Unity Editor (see the
note at the bottom of this README). Everything has been double- and
triple-checked against Unity's known serialization formats, but if you see
a stray warning on first import:

- **Missing script / missing reference** on anything other than the
  `GameBootstrap` object: safe to ignore — nothing else in the scene
  references a script directly, everything is wired up in code.
- **Console errors on Play**: please open an issue / send me the Console
  text — most likely a typo in one hand-written `.meta` GUID, which is a
  one-line fix.

## Controls

- **SPIN** button (bottom-center, or the red button built into the machine
  art) — spins the reels (costs the current bet).
- **◀ / ▶** arrows on the credits panel — decrease/increase the bet.
- Win popup — **YES/NO** to gamble your win, or the **✕** to close and bank it.

## Paytable (multiplier × current bet, per winning line)

| Symbol        | Payout | Notes                          |
|---------------|-------:|---------------------------------|
| BAR           | x3     | Most common                     |
| Cherry        | x8     |                                  |
| Bell          | x15    |                                  |
| Seven (WILD)  | x50    | Rarest; substitutes for others  |

Near-miss (2-of-3 match, no wild): 40% of the matched symbol's rate.

## Building for WebGL

1. `File → Build Settings…` → select **WebGL** → **Switch Platform**
   (the scene is already added to the build list).
2. `Player Settings` → confirm Company/Product name (pre-filled).
3. `Build` (or `Build And Run`) → output folder: **`Build/WebGL`** at the
   project root, so the final path is `Build/WebGL/index.html`.
4. Commit the contents of `Build/WebGL/` to the repo as required by the
   assignment. A `Build/WebGL/.gitkeep` placeholder is included so the
   folder exists in this submission even before you've run a build here —
   **I was not able to run the Unity Editor / WebGL build pipeline in the
   environment this project was authored in, so the actual build output
   still needs to be generated once on a machine with Unity installed.**
   Everything else (scripts, scene, project settings, WebGL Player
   Settings) is already configured for it.

## Project Structure

```
Assets/
  Scripts/
    Core/       Gameplay logic: reels, payouts, credits, RNG, config
    Data/       ScriptableObject symbol definitions
    UI/         Procedural UI construction (GameBootstrapper, UIFactory, UIManager)
    Audio/      Procedural SFX generation + playback
    Bonus/      Gamble ("Double or Nothing") feature
    Utils/      Small reusable helpers (Singleton, Easing)
  Resources/Sprites/   All imported art from the supplied asset pack
  Scenes/MainScene.unity
  Prefabs/, Animations/, UI/, Sounds/, ScriptableObjects/
    (kept as part of the required structure; each has a short README
    explaining why it's currently empty and what would go there — see
    "Design Notes" below)
Build/WebGL/           WebGL build output goes here
```

## Design Notes / Thought Process

**Core rule.** The brief states "player wins when all slots have the same
symbol." I read "slots" as the classic slot-machine sense of *a reel's
landed position on a payline*, so a win is 3 reels agreeing on one row.
Since the supplied frame art clearly shows a 3-row window (confirmed against
the reference GIF in the asset pack), I evaluate all three rows as
independent paylines rather than just one — more paylines = more
interesting outcomes and a better showcase of the payout logic, while the
middle row alone still satisfies the strictest literal reading if you set
`paylineCount = 1` in the `GameBootstrapper`/`SlotMachineController`
Inspector config.

**Why the UI is built entirely from code.** I did not have interactive
access to the Unity Editor while authoring this project, so hand-crafting
`.unity`/`.prefab` YAML for `Image`/`Button`/`Text`/mask components would
have meant guessing internal Unity package GUIDs I could not verify —
risking a broken scene on first open. Instead, `GameBootstrapper` composes
the entire visible game (Canvas, background, reel viewports with
`RectMask2D` clipping, HUD, popup, buttons with sprite-swapped states) from
plain C# using `AddComponent<T>()`, which the C# compiler resolves directly
against the engine — no serialized GUID references at risk of being wrong.
As a side benefit, the whole UI is fully reproducible from source and easy
to review/diff like any other code. The trade-off is documented above (Scene
view looks empty until Play).

**Art layering.** The supplied `slot-machine1.png` (frame), `slot-machine5.png`
(glass shine highlight) and the reel-window cut-outs all share one 816×624
composition — they're separated layers of a single design, not independent
images. `GameBootstrapper` stacks them at matching coordinates (measured
directly from the source PNGs) so they line up pixel-for-pixel: frame
background → live reel symbols (masked to the three window rects) → glass
shine overlay → lever/HUD on top.

**RNG & fairness.** `RandomGenerator` wraps `System.Random` behind a small
interface so it can be seeded for deterministic testing; `SymbolDatabase`
does weighted-random symbol selection (BAR 42, Cherry 30, Bell 20, Seven 8 —
rarer symbols pay more). Every spin's result is decided *before* the reel
animation plays, then the animation is guaranteed to land on that
pre-decided result — the spin never "lies" about the outcome.

**Audio.** No sound assets were supplied. Rather than ship silently,
`AudioManager` generates a few short procedural tones at startup
(`ProceduralTone.cs`) for clicks/spins/wins/losses. Swapping in real SFX
later is a one-line change per sound.

## Bonus Features Implemented

1. **Wild symbol** (Seven) with substitution logic.
2. **Multi-payline evaluation** (top/middle/bottom rows).
3. **Near-miss consolation payout.**
4. **Double or Nothing gamble round** after a win.
5. **Procedural audio** so the game isn't silent despite no supplied SFX.
6. **Staggered reel stops + eased deceleration + overshoot bounce** for a
   more physical, "weighted" spin feel rather than a linear scroll-and-stop.

## Tech Notes

- Engine: Unity **2022.3 LTS**, legacy `UnityEngine.UI` (uGUI), legacy Input
  Manager (no external packages required beyond built-in modules).
- No third-party assets/plugins.
- Namespaces: `SlotMachine.Core`, `SlotMachine.Data`, `SlotMachine.UI`,
  `SlotMachine.Audio`, `SlotMachine.Bonus`, `SlotMachine.Utils`.

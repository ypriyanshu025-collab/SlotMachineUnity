# Sounds

The supplied art pack did not include any audio assets, so
`Assets/Scripts/Audio/AudioManager.cs` generates a handful of small
procedural "beep" AudioClips at runtime (see `ProceduralTone.cs`) for click,
spin, reel-stop, win and lose feedback, rather than shipping the game silent.

Drop real `.wav`/`.mp3` SFX in this folder and assign them in
`AudioManager.Awake()` (replace the `ProceduralTone.Generate(...)` calls with
`Resources.Load<AudioClip>(...)` or serialized fields) to swap in proper
sound design — no other code needs to change, since every caller only ever
asks for `PlayClick()`, `PlayWin()`, etc.

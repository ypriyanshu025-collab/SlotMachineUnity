# Prefabs

This project builds its entire UI (Canvas, reels, HUD, popup, buttons) from
code at runtime — see `Assets/Scripts/UI/GameBootstrapper.cs` and
`UIFactory.cs`. That removes the need for hand-authored `.prefab` files for
UI elements (they're effectively "prefabbed" by the factory methods instead).

This folder is kept as part of the required project structure and is the
natural place to add real `.prefab` assets if the project grows to need
reusable, Inspector-editable objects (e.g. a VFX burst prefab for big wins).

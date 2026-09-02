# UI

All uGUI elements (Canvas, machine frame, reels, HUD, popup, buttons) are
constructed procedurally at runtime by `Assets/Scripts/UI/GameBootstrapper.cs`
and `UIFactory.cs`, using the sprites in `Assets/Resources/Sprites/`. This
folder is kept as part of the required project structure and is a natural
home for any hand-designed `.uxml`/`.uss` (UI Toolkit) or additional Sprite
assets if the presentation layer grows beyond what's built here.

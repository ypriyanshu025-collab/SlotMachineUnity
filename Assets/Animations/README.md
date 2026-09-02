# Animations

Reel spin/deceleration/bounce and button press feedback are driven by small
hand-written easing coroutines (see `Assets/Scripts/Utils/Easing.cs` and
`Assets/Scripts/Core/SlotReel.cs`) rather than Animator Controllers or
AnimationClips, since the motion is fully procedural (scroll distance and
timing depend on runtime RNG results). This folder is kept as part of the
required project structure and is where `.anim`/`.controller` assets would
live if timeline-based animation (e.g. a jackpot celebration sequence) is
added later.

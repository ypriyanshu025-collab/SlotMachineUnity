using UnityEngine;

namespace SlotMachine.Utils
{
    /// <summary>
    /// Small collection of easing functions used to give reel spin-up /
    /// spin-down motion a more natural, "realistic" feel than a linear
    /// scroll would have. All functions take a normalized time value
    /// (0..1) and return a normalized progress value (0..1).
    /// </summary>
    public static class Easing
    {
        public static float EaseOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            float f = t - 1f;
            return f * f * f + 1f;
        }

        public static float EaseInCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t;
        }

        public static float EaseOutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float f = t - 1f;
            return 1f + c3 * f * f * f + c1 * f * f;
        }

        public static float EaseInOutSine(float t)
        {
            t = Mathf.Clamp01(t);
            return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
        }
    }
}

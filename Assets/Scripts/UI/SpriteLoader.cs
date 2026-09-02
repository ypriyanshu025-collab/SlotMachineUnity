using System.Collections.Generic;
using UnityEngine;

namespace SlotMachine.UI
{
    /// <summary>
    /// Thin, cached wrapper around Resources.Load for the machine artwork.
    /// All art from the supplied asset pack lives under
    /// Assets/Resources/Sprites/ so it can be located by path string at
    /// runtime without every UI element needing a hand-wired Inspector
    /// reference — the entire interface is built procedurally by
    /// UIFactory/GameBootstrapper, so this is what stands in for those
    /// references.
    /// </summary>
    public static class SpriteLoader
    {
        private const string BasePath = "Sprites/";
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string spriteName)
        {
            if (Cache.TryGetValue(spriteName, out var cached))
            {
                return cached;
            }

            var sprite = Resources.Load<Sprite>(BasePath + spriteName);
            if (sprite == null)
            {
                Debug.LogWarning($"[SpriteLoader] Could not find sprite '{spriteName}' under Resources/{BasePath}");
            }
            Cache[spriteName] = sprite;
            return sprite;
        }
    }
}

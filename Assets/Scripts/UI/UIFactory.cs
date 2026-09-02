using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SlotMachine.UI
{
    /// <summary>
    /// Small collection of helper methods for building uGUI elements from
    /// code. Centralising object construction here keeps GameBootstrapper
    /// focused on *layout* (what goes where) rather than being cluttered
    /// with repetitive AddComponent boilerplate.
    /// </summary>
    public static class UIFactory
    {
        private static Font _cachedFont;

        public static Font DefaultFont
        {
            get
            {
                if (_cachedFont == null)
                {
                    _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return _cachedFont;
            }
        }

        public static Canvas CreateRootCanvas(string name, out CanvasScaler scaler)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        public static void CreateEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Object.DontDestroyOnLoad(go);
        }

        public static RectTransform CreateRect(Transform parent, string name, Vector2 anchoredPosition, Vector2 size,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>Top-left-pivot convenience overload — matches the pixel
        /// coordinates measured directly off the source artwork.</summary>
        public static RectTransform CreateRectTopLeft(Transform parent, string name, Vector2 pixelPos, Vector2 size)
        {
            return CreateRect(parent, name, new Vector2(pixelPos.x, -pixelPos.y), size,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        }

        public static Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 anchoredPosition, Vector2 size,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, bool raycastTarget = false)
        {
            var rect = CreateRect(parent, name, anchoredPosition, size, anchorMin, anchorMax, pivot);
            var img = rect.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = raycastTarget;
            if (sprite == null)
            {
                img.color = new Color(1f, 1f, 1f, 0f);
            }
            return img;
        }

        public static Image CreateImageTopLeft(Transform parent, string name, Sprite sprite, Vector2 pixelPos, Vector2 size, bool raycastTarget = false)
        {
            var rect = CreateRectTopLeft(parent, name, pixelPos, size);
            var img = rect.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = raycastTarget;
            return img;
        }

        /// <summary>Convenience overload: anchors/pivots the text to its
        /// parent's top-left corner, which matches how most HUD elements are
        /// laid out from measured art (pass an already-negated-Y anchoredPosition,
        /// e.g. from a "TopLeft(x, y) => new Vector2(x, -y)" helper).</summary>
        public static Text CreateText(Transform parent, string name, string content, int fontSize, Color color,
            TextAnchor alignment, Vector2 anchoredPosition, Vector2 size)
        {
            return CreateText(parent, name, content, fontSize, color, alignment, anchoredPosition, size,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        }

        public static Text CreateText(Transform parent, string name, string content, int fontSize, Color color,
            TextAnchor alignment, Vector2 anchoredPosition, Vector2 size,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var rect = CreateRect(parent, name, anchoredPosition, size, anchorMin, anchorMax, pivot);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = DefaultFont;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.text = content;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static RectTransform CreateMaskedViewport(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
        {
            var rect = CreateRect(parent, name, anchoredPosition, size, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            // RectMask2D needs a Graphic on the same object to compute correctly
            // in every Unity version's edge case handling, so give it an
            // invisible full-rect Image as well as the mask itself.
            var img = rect.gameObject.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = false;
            rect.gameObject.AddComponent<RectMask2D>();
            return rect;
        }

        /// <summary>
        /// Creates a Button whose visuals swap between the 4 supplied
        /// sprites (normal/hover/pressed/disabled), matching the sliced
        /// button art from the source pack.
        /// </summary>
        public static Button CreateSpriteButton(Transform parent, string name, Sprite normal, Sprite hover, Sprite pressed, Sprite disabled,
            Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var rect = CreateRect(parent, name, anchoredPosition, size, anchorMin, anchorMax, pivot);
            var img = rect.gameObject.AddComponent<Image>();
            img.sprite = normal;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.transition = Selectable.Transition.SpriteSwap;

            var state = new SpriteState
            {
                highlightedSprite = hover != null ? hover : normal,
                pressedSprite = pressed != null ? pressed : normal,
                disabledSprite = disabled != null ? disabled : normal,
                selectedSprite = normal
            };
            button.spriteState = state;

            return button;
        }
    }
}

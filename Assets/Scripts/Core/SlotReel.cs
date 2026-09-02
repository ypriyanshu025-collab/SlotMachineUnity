using System;
using System.Collections;
using System.Collections.Generic;
using SlotMachine.Data;
using SlotMachine.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.Core
{
    /// <summary>
    /// Drives a single reel: builds its scrolling strip of symbol icons,
    /// animates the spin (constant-speed scroll -> eased deceleration ->
    /// snap to a landed result) and reports the three symbols left visible
    /// in the viewport (top/middle/bottom row) once it comes to rest.
    ///
    /// The strip is built and recycled entirely in code: symbol Image
    /// objects are created once in <see cref="BuildStrip"/> and then reused
    /// (their sprites are swapped, not the GameObjects themselves) every
    /// spin, which avoids per-spin GC allocation from Instantiate/Destroy.
    /// </summary>
    public class SlotReel : MonoBehaviour
    {
        [Header("Wiring (assigned by UIFactory at build time)")]
        public RectTransform viewport;   // Masked window the player sees.
        public RectTransform strip;      // Scrolls vertically inside the viewport.

        [Header("Layout")]
        public float rowHeight = 69.6f;
        public int visibleRows = 3;

        [Header("Spin Feel")]
        public float spinSpeedPixelsPerSecond = 1400f;
        public float minSpinDuration = 0.55f;
        public float decelerateDuration = 0.45f;
        public float bounceOvershoot = 14f;
        public float bounceSettleDuration = 0.12f;

        private SymbolDatabase _database;
        private RandomGenerator _rng;
        private readonly List<Image> _stripIcons = new List<Image>();
        private int _bufferRowsAboveBelow = 3; // Extra rows above/below the visible window for a seamless scroll.
        private int _totalStripRows;

        private SymbolData[] _currentVisible; // length == visibleRows, index 0 = top.
        public SymbolData[] CurrentVisible => _currentVisible;

        public bool IsSpinning { get; private set; }

        public void Init(SymbolDatabase database, RandomGenerator rng)
        {
            _database = database;
            _rng = rng;
            BuildStrip();
        }

        /// <summary>
        /// Creates the pool of symbol icons that make up the scrolling
        /// strip. The strip is taller than the viewport by _bufferRowsAboveBelow
        /// rows on each side so that, mid-scroll, symbols can wrap smoothly
        /// from bottom back to top with no visible gap or pop.
        /// </summary>
        private void BuildStrip()
        {
            _totalStripRows = visibleRows + _bufferRowsAboveBelow * 2;

            for (int i = 0; i < _totalStripRows; i++)
            {
                var iconGO = new GameObject($"SymbolIcon_{i}", typeof(RectTransform), typeof(Image));
                var iconRect = (RectTransform)iconGO.transform;
                iconRect.SetParent(strip, false);
                iconRect.anchorMin = new Vector2(0f, 1f);
                iconRect.anchorMax = new Vector2(0f, 1f);
                iconRect.pivot = new Vector2(0f, 1f);
                iconRect.sizeDelta = new Vector2(strip.rect.width, rowHeight);
                iconRect.anchoredPosition = new Vector2(0f, -i * rowHeight);

                var img = iconGO.GetComponent<Image>();
                img.preserveAspect = true;
                img.raycastTarget = false;

                _stripIcons.Add(img);
            }

            strip.sizeDelta = new Vector2(strip.rect.width, _totalStripRows * rowHeight);
            RandomiseAllIcons();
            SnapStripToTop();
        }

        private void RandomiseAllIcons()
        {
            foreach (var icon in _stripIcons)
            {
                var symbol = _database.PickWeighted(_rng);
                icon.sprite = symbol != null ? symbol.sprite : null;
                icon.gameObject.name = symbol != null ? $"Symbol_{symbol.symbolType}" : "Symbol_Empty";
            }
        }

        private void SnapStripToTop()
        {
            strip.anchoredPosition = new Vector2(strip.anchoredPosition.x, 0f);
        }

        /// <summary>
        /// Starts the spin animation. When it finishes, the three symbols
        /// currently centred in the viewport are decided (weighted-random)
        /// and written into <see cref="CurrentVisible"/> before onComplete
        /// is invoked.
        /// </summary>
        public Coroutine Spin(float startDelay, Action onComplete)
        {
            return StartCoroutine(SpinRoutine(startDelay, onComplete));
        }

        private IEnumerator SpinRoutine(float startDelay, Action onComplete)
        {
            if (startDelay > 0f)
            {
                yield return new WaitForSeconds(startDelay);
            }

            IsSpinning = true;

            // Decide the final result up-front so the deceleration phase can
            // animate towards a known target instead of guessing.
            var finalSymbols = new SymbolData[visibleRows];
            for (int r = 0; r < visibleRows; r++)
            {
                finalSymbols[r] = _database.PickWeighted(_rng);
            }

            // Phase 1: constant-speed scroll for a minimum duration so the
            // spin always feels "alive" even if the caller stops it quickly.
            float elapsed = 0f;
            while (elapsed < minSpinDuration)
            {
                float delta = spinSpeedPixelsPerSecond * Time.deltaTime;
                ScrollStrip(delta);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Phase 2: ease the speed down to zero while continuing to
            // scroll, then snap-align to a row boundary so symbols never
            // rest half-visible.
            float decelElapsed = 0f;
            float startSpeed = spinSpeedPixelsPerSecond;
            while (decelElapsed < decelerateDuration)
            {
                float t = decelElapsed / decelerateDuration;
                float eased = 1f - Easing.EaseInCubic(t);
                float delta = startSpeed * eased * Time.deltaTime;
                ScrollStrip(delta);
                decelElapsed += Time.deltaTime;
                yield return null;
            }

            // Snap the remaining fractional scroll to an exact row boundary
            // FIRST, so the visible window is an unambiguous set of exactly
            // `visibleRows` icons; only then paint the pre-decided result
            // into that window. Doing it in this order avoids any epsilon
            // guesswork about which icons are "currently visible".
            AlignStripToRowGrid();
            ApplyResultToVisibleWindow(finalSymbols);

            // Phase 3: small overshoot "bounce" for extra game feel, as if
            // the reel had physical weight/momentum.
            yield return StartCoroutine(BounceSettle());

            _currentVisible = finalSymbols;
            IsSpinning = false;
            onComplete?.Invoke();
        }

        private void ScrollStrip(float deltaPixels)
        {
            Vector2 pos = strip.anchoredPosition;
            pos.y += deltaPixels;

            // The strip scrolls in the +Y direction, so the icon(s) currently
            // sitting at the TOP of the strip's local space are the ones
            // scrolling out of view first. Once a full row has passed,
            // recycle that top icon down to below the current bottom icon so
            // the strip appears to loop endlessly in the scroll direction.
            while (pos.y >= rowHeight)
            {
                pos.y -= rowHeight;
                RecycleTopIconToBottom();
            }

            strip.anchoredPosition = pos;
        }

        private void RecycleTopIconToBottom()
        {
            int bottomIndex = 0;
            float lowestY = float.MaxValue;   // most negative local Y = physically lowest icon.
            int topIndex = 0;
            float highestY = float.NegativeInfinity; // least negative/most positive local Y = physically highest icon.
            for (int i = 0; i < _stripIcons.Count; i++)
            {
                float y = _stripIcons[i].rectTransform.anchoredPosition.y;
                if (y < lowestY) { lowestY = y; bottomIndex = i; }
                if (y > highestY) { highestY = y; topIndex = i; }
            }

            var bottomRect = _stripIcons[bottomIndex].rectTransform;
            var topRect = _stripIcons[topIndex].rectTransform;

            // The icon that just scrolled out the top becomes new buffer
            // content one row below the current bottom-most icon.
            topRect.anchoredPosition = new Vector2(topRect.anchoredPosition.x, bottomRect.anchoredPosition.y - rowHeight);

            var symbol = _database.PickWeighted(_rng);
            _stripIcons[topIndex].sprite = symbol != null ? symbol.sprite : null;
        }

        /// <summary>
        /// Overwrites whichever icons are currently sitting in the visible
        /// rows (top/middle/bottom of the viewport) with the pre-decided
        /// final result. Must be called only after <see cref="AlignStripToRowGrid"/>
        /// has zeroed the strip's fractional offset, at which point the
        /// visible window is unambiguous: it is exactly the `visibleRows`
        /// icons with the largest local Y (closest to the viewport's top edge).
        /// </summary>
        private void ApplyResultToVisibleWindow(SymbolData[] finalSymbols)
        {
            var ordered = new List<Image>(_stripIcons);
            // Descending by local Y: index 0 = topmost visible row.
            ordered.Sort((a, b) => b.rectTransform.anchoredPosition.y.CompareTo(a.rectTransform.anchoredPosition.y));

            for (int i = 0; i < visibleRows && i < ordered.Count; i++)
            {
                var symbol = finalSymbols[i];
                ordered[i].sprite = symbol != null ? symbol.sprite : null;
                ordered[i].gameObject.name = symbol != null ? $"Symbol_{symbol.symbolType}" : "Symbol_Empty";
            }
        }

        /// <summary>Zeroes the strip's fractional scroll offset so its local
        /// Y values, not just strip.anchoredPosition, define the grid.</summary>
        private void AlignStripToRowGrid()
        {
            Vector2 pos = strip.anchoredPosition;
            pos.y = 0f;
            strip.anchoredPosition = pos;
        }

        private IEnumerator BounceSettle()
        {
            float elapsed = 0f;
            Vector2 basePos = strip.anchoredPosition;
            while (elapsed < bounceSettleDuration)
            {
                float t = elapsed / bounceSettleDuration;
                float overshoot = Mathf.Sin(t * Mathf.PI) * bounceOvershoot * (1f - t);
                strip.anchoredPosition = new Vector2(basePos.x, basePos.y - overshoot);
                elapsed += Time.deltaTime;
                yield return null;
            }
            strip.anchoredPosition = basePos;
        }
    }
}

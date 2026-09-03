using System;
using System.Collections;
using System.Collections.Generic;
using SlotMachine.Data;
using SlotMachine.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.Core
{
    public class SlotReel : MonoBehaviour
    {
        public RectTransform viewport;
        public RectTransform strip;

        public float rowHeight = 69.6f;
        public int visibleRows = 3;

        public float spinSpeedPixelsPerSecond = 1400f;
        public float minSpinDuration = 0.8f;
        public float decelerateDuration = 0.6f;

        public float bounceOvershoot = 14f;
        public float bounceSettleDuration = 0.12f;

        private SymbolDatabase _database;
        private RandomGenerator _rng;

        private readonly List<Image> _stripIcons = new List<Image>();

        private int _bufferRowsAboveBelow = 3;
        private int _totalStripRows;

        private SymbolData[] _currentVisible;

        public SymbolData[] CurrentVisible => _currentVisible;
        public bool IsSpinning { get; private set; }

        public void Init(SymbolDatabase database, RandomGenerator rng)
        {
            _database = database;
            _rng = rng;

            BuildStrip();
        }

        private void BuildStrip()
        {
            _totalStripRows = visibleRows + (_bufferRowsAboveBelow * 2);

            _stripIcons.Clear();

            // Make sure the strip has a usable width.
            float width = strip.rect.width;

            if (width <= 0f && viewport != null)
                width = viewport.rect.width;

            if (width <= 0f)
                width = 108f;

            for (int i = 0; i < _totalStripRows; i++)
            {
                GameObject iconGO = new GameObject(
                    $"SymbolIcon_{i}",
                    typeof(RectTransform),
                    typeof(Image)
                );

                RectTransform iconRect = iconGO.GetComponent<RectTransform>();

                iconRect.SetParent(strip, false);

                // Top-left layout.
                iconRect.anchorMin = new Vector2(0f, 1f);
                iconRect.anchorMax = new Vector2(0f, 1f);
                iconRect.pivot = new Vector2(0f, 1f);

                iconRect.sizeDelta = new Vector2(width, rowHeight);

                iconRect.anchoredPosition =
                    new Vector2(0f, -i * rowHeight);

                Image image = iconGO.GetComponent<Image>();

                image.preserveAspect = true;
                image.raycastTarget = false;

                _stripIcons.Add(image);
            }

            strip.sizeDelta =
                new Vector2(width, _totalStripRows * rowHeight);

            RandomiseAllIcons();

            // Start at the top.
            strip.anchoredPosition =
                new Vector2(strip.anchoredPosition.x, 0f);
        }

        private void RandomiseAllIcons()
        {
            if (_database == null || _rng == null)
                return;

            for (int i = 0; i < _stripIcons.Count; i++)
            {
                SymbolData symbol =
                    _database.PickWeighted(_rng);

                ApplySymbol(_stripIcons[i], symbol);
            }
        }

        private void ApplySymbol(Image image, SymbolData symbol)
        {
            if (image == null)
                return;

            image.sprite = symbol != null ? symbol.sprite : null;

            image.gameObject.name =
                symbol != null
                    ? $"Symbol_{symbol.symbolType}"
                    : "Symbol_Empty";
        }

        public Coroutine Spin(float startDelay, Action onComplete)
        {
            return StartCoroutine(
                SpinRoutine(startDelay, onComplete)
            );
        }

        private IEnumerator SpinRoutine(
            float startDelay,
            Action onComplete)
        {
            if (startDelay > 0f)
                yield return new WaitForSeconds(startDelay);

            IsSpinning = true;

            // Decide the final result before animation starts.
            SymbolData[] finalSymbols =
                new SymbolData[visibleRows];

            for (int i = 0; i < visibleRows; i++)
            {
                finalSymbols[i] =
                    _database.PickWeighted(_rng);
            }

            // -----------------------------------------
            // FAST SPIN
            // -----------------------------------------

            float elapsed = 0f;

            while (elapsed < minSpinDuration)
            {
                float delta =
                    spinSpeedPixelsPerSecond *
                    Time.deltaTime;

                ScrollStrip(delta);

                elapsed += Time.deltaTime;

                yield return null;
            }

            // -----------------------------------------
            // DECELERATION
            // -----------------------------------------

            float decelElapsed = 0f;

            while (decelElapsed < decelerateDuration)
            {
                float t =
                    decelElapsed / decelerateDuration;

                float speedMultiplier =
                    1f - Easing.EaseInCubic(t);

                float delta =
                    spinSpeedPixelsPerSecond *
                    speedMultiplier *
                    Time.deltaTime;

                ScrollStrip(delta);

                decelElapsed += Time.deltaTime;

                yield return null;
            }

            // -----------------------------------------
            // STOP ON GRID
            // -----------------------------------------

            strip.anchoredPosition =
                new Vector2(
                    strip.anchoredPosition.x,
                    0f
                );

            // Put the selected result into the
            // three visible positions.
            ApplyResultToVisibleWindow(finalSymbols);

            // Small realistic bounce.
            yield return StartCoroutine(
                BounceSettle()
            );

            _currentVisible = finalSymbols;

            IsSpinning = false;

            onComplete?.Invoke();
        }

        private void ScrollStrip(float deltaPixels)
        {
            Vector2 position =
                strip.anchoredPosition;

            // Move the strip upward.
            position.y += deltaPixels;

            while (position.y >= rowHeight)
            {
                position.y -= rowHeight;

                RecycleTopIconToBottom();
            }

            strip.anchoredPosition = position;
        }

        private void RecycleTopIconToBottom()
        {
            if (_stripIcons.Count == 0)
                return;

            // First icon is currently at the top.
            Image topIcon = _stripIcons[0];

            // Last icon is currently at the bottom.
            Image bottomIcon =
                _stripIcons[_stripIcons.Count - 1];

            RectTransform topRect =
                topIcon.rectTransform;

            RectTransform bottomRect =
                bottomIcon.rectTransform;

            // Move the top icon underneath the
            // current bottom icon.
            topRect.anchoredPosition =
                new Vector2(
                    topRect.anchoredPosition.x,
                    bottomRect.anchoredPosition.y - rowHeight
                );

            // Give it a new random symbol.
            SymbolData symbol =
                _database.PickWeighted(_rng);

            ApplySymbol(topIcon, symbol);

            // Keep the list in visual order.
            _stripIcons.RemoveAt(0);
            _stripIcons.Add(topIcon);
        }

        private void ApplyResultToVisibleWindow(
            SymbolData[] finalSymbols)
        {
            int count =
                Mathf.Min(
                    visibleRows,
                    finalSymbols.Length
                );

            for (int i = 0; i < count; i++)
            {
                ApplySymbol(
                    _stripIcons[i],
                    finalSymbols[i]
                );
            }
        }

        private IEnumerator BounceSettle()
        {
            float elapsed = 0f;

            Vector2 basePosition =
                strip.anchoredPosition;

            while (elapsed < bounceSettleDuration)
            {
                float t =
                    elapsed / bounceSettleDuration;

                float overshoot =
                    Mathf.Sin(t * Mathf.PI) *
                    bounceOvershoot *
                    (1f - t);

                strip.anchoredPosition =
                    new Vector2(
                        basePosition.x,
                        basePosition.y - overshoot
                    );

                elapsed += Time.deltaTime;

                yield return null;
            }

            strip.anchoredPosition = basePosition;
        }
    }
}
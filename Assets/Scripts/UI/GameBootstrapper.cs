using SlotMachine.Audio;
using SlotMachine.Core;
using SlotMachine.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.UI
{
    /// <summary>
    /// Composition root for the entire game.
    /// Builds the complete slot-machine UI at runtime.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private GameConfig config = new GameConfig();
        [SerializeField] private SymbolDatabase symbolDatabase;

        private static readonly Vector2 FrameSize =
            new Vector2(816f, 624f);

        private static readonly Vector2[] ReelWindowPos =
        {
            new Vector2(229f, 246f),
            new Vector2(359f, 246f),
            new Vector2(489f, 246f),
        };

        private static readonly Vector2 ReelWindowSize =
            new Vector2(108f, 210f);

        private static readonly Vector2 LeverPos =
            new Vector2(673f, 296f);

        private static readonly Vector2 LeverSize =
            new Vector2(92f, 270f);

        private static readonly Vector2 SpinHotspotPos =
            new Vector2(686f, 300f);

        private static readonly Vector2 SpinHotspotSize =
            new Vector2(100f, 120f);


        private void Awake()
        {
            UIFactory.CreateEventSystem();
            EnsureAudioManager();

            var database =
                symbolDatabase != null
                    ? symbolDatabase
                    : BuildDefaultSymbolDatabase();

            var canvas =
                UIFactory.CreateRootCanvas(
                    "MainCanvas",
                    out _
                );

            var canvasTransform =
                canvas.transform;

            BuildBackground(canvasTransform);

            var framePanel =
                BuildMachineFrame(
                    canvasTransform,
                    out SlotReel[] reels
                );

            BuildLever(framePanel);

            var spinHotspot =
                BuildSpinHotspot(framePanel);

            var hud =
                BuildHud(canvasTransform);

            var popup =
                BuildPopup(canvasTransform);


            // --------------------------------------------------------
            // SLOT MACHINE CONTROLLER
            // --------------------------------------------------------

            var controllerGO =
                new GameObject("SlotMachineController");

            var controller =
                controllerGO.AddComponent<SlotMachineController>();

            controller.Configure(config);
            controller.Init(reels, database);


            // --------------------------------------------------------
            // UI MANAGER
            // --------------------------------------------------------

            var uiManagerGO =
                new GameObject("UIManager");

            var uiManager =
                uiManagerGO.AddComponent<UIManager>();

            uiManager.creditsText =
                hud.creditsText;

            uiManager.betValueText =
                hud.betValueText;

            uiManager.winText =
                hud.winText;

            uiManager.betPlusButton =
                hud.betPlusButton;

            uiManager.betMinusButton =
                hud.betMinusButton;

            uiManager.spinButton =
                hud.spinButton;

            uiManager.popupRoot =
                popup.root;

            uiManager.popupTitleText =
                popup.titleText;

            uiManager.popupWinAmountText =
                popup.winAmountText;

            uiManager.popupGambleQuestionText =
                popup.gambleQuestionText;

            uiManager.popupYesButton =
                popup.yesButton;

            uiManager.popupNoButton =
                popup.noButton;

            uiManager.popupCloseButton =
                popup.closeButton;

            uiManager.gambleButtonsGroup =
                popup.gambleButtonsGroup;

            uiManager.Bind(controller);


            // --------------------------------------------------------
            // DECORATIVE MACHINE SPIN BUTTON
            // --------------------------------------------------------

            spinHotspot.onClick.AddListener(() =>
            {
                if (!popup.root.activeSelf)
                {
                    controller.RequestSpin();
                }
            });
        }


        // ============================================================
        // AUDIO MANAGER
        // ============================================================

        private void EnsureAudioManager()
        {
            if (FindObjectOfType<AudioManager>() != null)
            {
                return;
            }

            var go =
                new GameObject("AudioManager");

            go.AddComponent<AudioManager>();
        }


        // ============================================================
        // BACKGROUND
        // ============================================================

        private void BuildBackground(
            Transform canvasTransform)
        {
            UIFactory.CreateImage(
                canvasTransform,
                "Background",
                SpriteLoader.Get("bg_gradient"),
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f)
            );
        }


        // ============================================================
        // MACHINE FRAME + REELS
        // ============================================================

        private RectTransform BuildMachineFrame(
            Transform canvasTransform,
            out SlotReel[] reels)
        {
            // Main machine container.
            var panelRect =
                UIFactory.CreateRect(
                    canvasTransform,
                    "MachineFrame",
                    new Vector2(0f, 40f),
                    FrameSize,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f)
                );

            panelRect.localScale =
                Vector3.one * 1.25f;


            // --------------------------------------------------------
            // FRAME ART
            // --------------------------------------------------------
            // IMPORTANT:
            // The frame is now a CHILD Image instead of putting the
            // Image directly on MachineFrame.
            //
            // This allows the reel viewports to render above it.
            // --------------------------------------------------------

            GameObject frameObject =
                new GameObject(
                    "FrameArt",
                    typeof(RectTransform),
                    typeof(Image)
                );

            RectTransform frameRect =
                frameObject.GetComponent<RectTransform>();

            frameRect.SetParent(
                panelRect,
                false
            );

            frameRect.anchorMin =
                new Vector2(0f, 1f);

            frameRect.anchorMax =
                new Vector2(0f, 1f);

            frameRect.pivot =
                new Vector2(0f, 1f);

            frameRect.anchoredPosition =
                Vector2.zero;

            frameRect.sizeDelta =
                FrameSize;

            Image frameImage =
                frameObject.GetComponent<Image>();

            frameImage.sprite =
                SpriteLoader.Get("slot_frame");

            frameImage.raycastTarget =
                false;

            // Frame MUST be behind the reels.
            frameRect.SetAsFirstSibling();


            // --------------------------------------------------------
            // REELS
            // --------------------------------------------------------

            reels =
                new SlotReel[ReelWindowPos.Length];

            for (int i = 0;
                 i < ReelWindowPos.Length;
                 i++)
            {
                RectTransform viewport =
                    UIFactory.CreateMaskedViewport(
                        panelRect,
                        $"Reel{i}_Viewport",
                        new Vector2(
                            ReelWindowPos[i].x,
                            -ReelWindowPos[i].y
                        ),
                        ReelWindowSize
                    );

                // Reel viewport MUST be above frame.
                viewport.SetAsLastSibling();


                // ----------------------------------------------------
                // STRIP
                // ----------------------------------------------------

                RectTransform stripRect =
                    UIFactory.CreateRect(
                        viewport,
                        "Strip",
                        Vector2.zero,
                        new Vector2(
                            ReelWindowSize.x,
                            ReelWindowSize.y
                        ),
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f)
                    );

                stripRect.SetAsLastSibling();


                // ----------------------------------------------------
                // SLOT REEL
                // ----------------------------------------------------

                SlotReel reel =
                    viewport.gameObject.AddComponent<SlotReel>();

                reel.viewport =
                    viewport;

                reel.strip =
                    stripRect;

                reel.rowHeight =
                    ReelWindowSize.y / 3f;

                reel.visibleRows =
                    3;

                reel.spinSpeedPixelsPerSecond =
                    1400f;

                reel.minSpinDuration =
                    0.8f;

                reel.decelerateDuration =
                    0.6f;

                reels[i] =
                    reel;
            }


            // IMPORTANT:
            // DO NOT create reel_glass_shine here.
            //
            // That image is large and can cover the symbols.
            // --------------------------------------------------------

            return panelRect;
        }


        // ============================================================
        // LEVER
        // ============================================================

        private void BuildLever(
            RectTransform framePanel)
        {
            UIFactory.CreateImageTopLeft(
                framePanel,
                "Lever",
                SpriteLoader.Get("lever"),
                LeverPos,
                LeverSize,
                raycastTarget: false
            );
        }


        // ============================================================
        // INVISIBLE SPIN HOTSPOT
        // ============================================================

        private Button BuildSpinHotspot(
            RectTransform framePanel)
        {
            var rect =
                UIFactory.CreateRectTopLeft(
                    framePanel,
                    "SpinHotspot",
                    SpinHotspotPos,
                    SpinHotspotSize
                );

            var img =
                rect.gameObject.AddComponent<Image>();

            img.color =
                new Color(
                    1f,
                    1f,
                    1f,
                    0f
                );

            img.raycastTarget =
                true;

            var button =
                rect.gameObject.AddComponent<Button>();

            button.targetGraphic =
                img;

            button.transition =
                Selectable.Transition.ColorTint;

            var colors =
                button.colors;

            colors.normalColor =
                new Color(
                    1f,
                    1f,
                    1f,
                    0f
                );

            colors.highlightedColor =
                new Color(
                    1f,
                    1f,
                    1f,
                    0.12f
                );

            colors.pressedColor =
                new Color(
                    1f,
                    0.7f,
                    0.2f,
                    0.35f
                );

            colors.disabledColor =
                new Color(
                    1f,
                    1f,
                    1f,
                    0f
                );

            button.colors =
                colors;

            return button;
        }


        // ============================================================
        // HUD
        // ============================================================

        private struct HudRefs
        {
            public Text creditsText;
            public Text betValueText;
            public Text winText;

            public Button betPlusButton;
            public Button betMinusButton;
            public Button spinButton;
        }


        private HudRefs BuildHud(
            Transform canvasTransform)
        {
            var panelSize =
                new Vector2(658f, 277f);

            var panel =
                UIFactory.CreateRect(
                    canvasTransform,
                    "HudPanel",
                    new Vector2(-160f, 40f),
                    panelSize,
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f)
                );

            var bg =
                panel.gameObject.AddComponent<Image>();

            bg.sprite =
                SpriteLoader.Get("credits_panel_bg");

            bg.raycastTarget =
                false;

            var refs =
                new HudRefs();


            refs.creditsText =
                UIFactory.CreateText(
                    panel,
                    "CreditsText",
                    "CREDITS: 1000",
                    30,
                    Color.white,
                    TextAnchor.MiddleLeft,
                    TopLeft(24, 18),
                    new Vector2(360, 40)
                );


            refs.winText =
                UIFactory.CreateText(
                    panel,
                    "WinText",
                    "WIN: 0",
                    24,
                    new Color(1f, 0.85f, 0.2f),
                    TextAnchor.MiddleLeft,
                    TopLeft(24, 62),
                    new Vector2(360, 36)
                );


            UIFactory.CreateText(
                panel,
                "BetLabel",
                "BET",
                22,
                Color.white,
                TextAnchor.MiddleLeft,
                TopLeft(24, 108),
                new Vector2(90, 36)
            );


            refs.betMinusButton =
                UIFactory.CreateSpriteButton(
                    panel,
                    "BetMinusButton",
                    SpriteLoader.Get("btn_betminus_normal"),
                    SpriteLoader.Get("btn_betminus_hover"),
                    SpriteLoader.Get("btn_betminus_pressed"),
                    SpriteLoader.Get("btn_betminus_disabled"),
                    TopLeftAnchored(190, 100),
                    new Vector2(46, 46),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f)
                );


            refs.betValueText =
                UIFactory.CreateText(
                    panel,
                    "BetValueText",
                    "10",
                    26,
                    Color.white,
                    TextAnchor.MiddleCenter,
                    TopLeft(244, 108),
                    new Vector2(80, 36)
                );


            refs.betPlusButton =
                UIFactory.CreateSpriteButton(
                    panel,
                    "BetPlusButton",
                    SpriteLoader.Get("btn_betplus_normal"),
                    SpriteLoader.Get("btn_betplus_hover"),
                    SpriteLoader.Get("btn_betplus_pressed"),
                    SpriteLoader.Get("btn_betplus_disabled"),
                    TopLeftAnchored(332, 100),
                    new Vector2(46, 46),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f)
                );


            UIFactory.CreateText(
                panel,
                "PaytableLegend",
                "PAYTABLE (x BET)\nBAR x3    CHERRY x8    BELL x15    7-WILD x50",
                15,
                new Color(0.85f, 0.9f, 1f),
                TextAnchor.UpperLeft,
                TopLeft(24, 158),
                new Vector2(610, 80)
            );


            // --------------------------------------------------------
            // PRIMARY SPIN BUTTON
            // --------------------------------------------------------

            var spinRect =
                UIFactory.CreateRect(
                    canvasTransform,
                    "SpinButton",
                    new Vector2(360f, 130f),
                    new Vector2(170f, 170f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f)
                );

            var spinBg =
                spinRect.gameObject.AddComponent<Image>();

            spinBg.color =
                new Color(
                    0.75f,
                    0.12f,
                    0.1f
                );

            var spinButtonComp =
                spinRect.gameObject.AddComponent<Button>();

            spinButtonComp.targetGraphic =
                spinBg;

            spinButtonComp.transition =
                Selectable.Transition.ColorTint;

            var spinColors =
                spinButtonComp.colors;

            spinColors.normalColor =
                new Color(
                    0.78f,
                    0.15f,
                    0.12f
                );

            spinColors.highlightedColor =
                new Color(
                    0.9f,
                    0.25f,
                    0.18f
                );

            spinColors.pressedColor =
                new Color(
                    0.55f,
                    0.08f,
                    0.06f
                );

            spinColors.disabledColor =
                new Color(
                    0.4f,
                    0.4f,
                    0.4f
                );

            spinButtonComp.colors =
                spinColors;

            refs.spinButton =
                spinButtonComp;


            UIFactory.CreateText(
                spinRect,
                "Label",
                "SPIN",
                34,
                Color.white,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                new Vector2(170, 170),
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f)
            );


            return refs;


            Vector2 TopLeft(
                float x,
                float y)
            {
                return new Vector2(
                    x,
                    -y
                );
            }


            Vector2 TopLeftAnchored(
                float x,
                float y)
            {
                return new Vector2(
                    x,
                    -y
                );
            }
        }


        // ============================================================
        // WIN POPUP
        // ============================================================

        private struct PopupRefs
        {
            public GameObject root;
            public GameObject gambleButtonsGroup;

            public Text titleText;
            public Text winAmountText;
            public Text gambleQuestionText;

            public Button yesButton;
            public Button noButton;
            public Button closeButton;
        }


        private PopupRefs BuildPopup(
            Transform canvasTransform)
        {
            var panelSize =
                new Vector2(700f, 353f);

            var panel =
                UIFactory.CreateRect(
                    canvasTransform,
                    "WinPopup",
                    Vector2.zero,
                    panelSize,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f)
                );

            var bg =
                panel.gameObject.AddComponent<Image>();

            bg.sprite =
                SpriteLoader.Get("popup_bg");

            bg.raycastTarget =
                true;


            var refs =
                new PopupRefs
                {
                    root = panel.gameObject
                };


            refs.titleText =
                UIFactory.CreateText(
                    panel,
                    "Title",
                    "YOU WIN!",
                    40,
                    new Color(1f, 0.85f, 0.2f),
                    TextAnchor.MiddleCenter,
                    new Vector2(0, -50),
                    new Vector2(600, 60),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f)
                );


            refs.winAmountText =
                UIFactory.CreateText(
                    panel,
                    "WinAmount",
                    "+0",
                    56,
                    Color.white,
                    TextAnchor.MiddleCenter,
                    new Vector2(0, -130),
                    new Vector2(600, 70),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f)
                );


            refs.gambleQuestionText =
                UIFactory.CreateText(
                    panel,
                    "GambleQuestion",
                    "Double or nothing?",
                    22,
                    Color.white,
                    TextAnchor.MiddleCenter,
                    new Vector2(0, -200),
                    new Vector2(600, 40),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f)
                );


            // --------------------------------------------------------
            // GAMBLE BUTTON GROUP
            // --------------------------------------------------------

            var gambleGroup =
                new GameObject(
                    "GambleButtons",
                    typeof(RectTransform)
                );

            var gambleRect =
                (RectTransform)gambleGroup.transform;

            gambleRect.SetParent(
                panel,
                false
            );

            gambleRect.anchorMin =
                new Vector2(0.5f, 1f);

            gambleRect.anchorMax =
                new Vector2(0.5f, 1f);

            gambleRect.pivot =
                new Vector2(0.5f, 1f);

            gambleRect.anchoredPosition =
                new Vector2(0, -250);

            gambleRect.sizeDelta =
                new Vector2(500, 90);

            refs.gambleButtonsGroup =
                gambleGroup;


            // --------------------------------------------------------
            // YES
            // --------------------------------------------------------

            refs.yesButton =
                UIFactory.CreateSpriteButton(
                    gambleRect,
                    "YesButton",
                    SpriteLoader.Get("btn_yes_normal"),
                    SpriteLoader.Get("btn_yes_hover"),
                    SpriteLoader.Get("btn_yes_pressed"),
                    null,
                    new Vector2(-130, -45),
                    new Vector2(220, 90),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f)
                );


            // --------------------------------------------------------
            // NO
            // --------------------------------------------------------

            refs.noButton =
                UIFactory.CreateSpriteButton(
                    gambleRect,
                    "NoButton",
                    SpriteLoader.Get("btn_no_normal"),
                    SpriteLoader.Get("btn_no_hover"),
                    SpriteLoader.Get("btn_no_pressed"),
                    null,
                    new Vector2(130, -45),
                    new Vector2(220, 90),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f)
                );


            // --------------------------------------------------------
            // CLOSE
            // --------------------------------------------------------

            refs.closeButton =
                UIFactory.CreateSpriteButton(
                    panel,
                    "CloseButton",
                    SpriteLoader.Get("btn_close_normal"),
                    SpriteLoader.Get("btn_close_hover"),
                    SpriteLoader.Get("btn_close_pressed"),
                    SpriteLoader.Get("btn_close_disabled"),
                    new Vector2(-15, -15),
                    new Vector2(44, 44),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f)
                );


            panel.gameObject.SetActive(false);

            return refs;
        }


        // ============================================================
        // DEFAULT SYMBOL DATABASE
        // ============================================================

        private static SymbolDatabase BuildDefaultSymbolDatabase()
        {
            var db =
                ScriptableObject.CreateInstance<SymbolDatabase>();

            db.symbols.Add(
                MakeSymbol(
                    SlotSymbolType.Bar,
                    "symbol_bar",
                    "Bar",
                    weight: 42,
                    payout: 3f,
                    wild: false
                )
            );

            db.symbols.Add(
                MakeSymbol(
                    SlotSymbolType.Cherry,
                    "symbol_cherry",
                    "Cherry",
                    weight: 30,
                    payout: 8f,
                    wild: false
                )
            );

            db.symbols.Add(
                MakeSymbol(
                    SlotSymbolType.Bell,
                    "symbol_bell",
                    "Bell",
                    weight: 20,
                    payout: 15f,
                    wild: false
                )
            );

            db.symbols.Add(
                MakeSymbol(
                    SlotSymbolType.Seven,
                    "symbol_seven",
                    "Seven (Wild)",
                    weight: 8,
                    payout: 50f,
                    wild: true
                )
            );

            return db;
        }


        // ============================================================
        // SYMBOL CREATION
        // ============================================================

        private static SymbolData MakeSymbol(
            SlotSymbolType type,
            string spriteName,
            string displayName,
            int weight,
            float payout,
            bool wild)
        {
            var data =
                ScriptableObject.CreateInstance<SymbolData>();

            data.symbolType =
                type;

            data.sprite =
                SpriteLoader.Get(spriteName);

            data.weight =
                weight;

            data.payoutMultiplier =
                payout;

            data.isWild =
                wild;

            data.displayName =
                displayName;

            return data;
        }
    }
}
using SlotMachine.Audio;
using SlotMachine.Core;
using SlotMachine.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.UI
{
    /// <summary>
    /// Composition root for the entire game. This is the ONE object placed
    /// by hand in MainScene.unity; everything else — Canvas, background,
    /// machine frame, reels, HUD, popup, EventSystem, AudioManager — is
    /// constructed here in code on Awake/Start.
    ///
    /// Why build the UI from code instead of hand-wiring it in the Editor?
    /// It makes the entire visible game reproducible from source (no
    /// fragile drag-and-drop references that silently break if an asset is
    /// renamed or moved), it keeps the diff for any layout tweak readable
    /// in code review, and it means anyone can regenerate the whole scene
    /// from a clean checkout just by pressing Play. The trade-off is that
    /// the Scene view looks empty until you press Play — that's expected.
    ///
    /// All pixel coordinates below were measured directly from the source
    /// artwork (the reel windows, lever mount, etc. all share one 816x624
    /// composition, see README "Art & Layout Notes").
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private GameConfig config = new GameConfig();
        [SerializeField] private SymbolDatabase symbolDatabase; // Optional: assign in Inspector to override the built-in default.

        private static readonly Vector2 FrameSize = new Vector2(816f, 624f);
        private static readonly Vector2[] ReelWindowPos =
        {
            new Vector2(229f, 246f),
            new Vector2(359f, 246f),
            new Vector2(489f, 246f),
        };
        private static readonly Vector2 ReelWindowSize = new Vector2(108f, 210f);
        private static readonly Vector2 LeverPos = new Vector2(673f, 296f);
        private static readonly Vector2 LeverSize = new Vector2(92f, 270f);
        private static readonly Vector2 SpinHotspotPos = new Vector2(686f, 300f);
        private static readonly Vector2 SpinHotspotSize = new Vector2(100f, 120f);

        private void Awake()
        {
            UIFactory.CreateEventSystem();
            EnsureAudioManager();

            var database = symbolDatabase != null ? symbolDatabase : BuildDefaultSymbolDatabase();

            var canvas = UIFactory.CreateRootCanvas("MainCanvas", out _);
            var canvasTransform = canvas.transform;

            BuildBackground(canvasTransform);

            var framePanel = BuildMachineFrame(canvasTransform, out SlotReel[] reels);
            BuildLever(framePanel);
            var spinHotspot = BuildSpinHotspot(framePanel);

            var hud = BuildHud(canvasTransform);
            var popup = BuildPopup(canvasTransform);

            var controllerGO = new GameObject("SlotMachineController");
            var controller = controllerGO.AddComponent<SlotMachineController>();
            controller.Configure(config);
            controller.Init(reels, database);

            var uiManagerGO = new GameObject("UIManager");
            var uiManager = uiManagerGO.AddComponent<UIManager>();
            uiManager.creditsText = hud.creditsText;
            uiManager.betValueText = hud.betValueText;
            uiManager.winText = hud.winText;
            uiManager.betPlusButton = hud.betPlusButton;
            uiManager.betMinusButton = hud.betMinusButton;
            uiManager.spinButton = hud.spinButton;
            uiManager.popupRoot = popup.root;
            uiManager.popupTitleText = popup.titleText;
            uiManager.popupWinAmountText = popup.winAmountText;
            uiManager.popupGambleQuestionText = popup.gambleQuestionText;
            uiManager.popupYesButton = popup.yesButton;
            uiManager.popupNoButton = popup.noButton;
            uiManager.popupCloseButton = popup.closeButton;
            uiManager.gambleButtonsGroup = popup.gambleButtonsGroup;
            uiManager.Bind(controller);

            // The decorative red button baked into the frame art also spins,
            // for players who click the "physical" button instead of the HUD one.
            spinHotspot.onClick.AddListener(() =>
            {
                if (!popup.root.activeSelf) controller.RequestSpin();
            });
        }

        private void EnsureAudioManager()
        {
            if (FindObjectOfType<AudioManager>() != null) return;
            var go = new GameObject("AudioManager");
            go.AddComponent<AudioManager>();
        }

        private void BuildBackground(Transform canvasTransform)
        {
            UIFactory.CreateImage(canvasTransform, "Background", SpriteLoader.Get("bg_gradient"),
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        }

        private RectTransform BuildMachineFrame(Transform canvasTransform, out SlotReel[] reels)
        {
            var panelRect = UIFactory.CreateRect(canvasTransform, "MachineFrame", new Vector2(0f, 40f), FrameSize,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            panelRect.localScale = Vector3.one * 1.25f;

            var frameImage = panelRect.gameObject.AddComponent<Image>();
            frameImage.sprite = SpriteLoader.Get("slot_frame");
            frameImage.raycastTarget = false;

            reels = new SlotReel[ReelWindowPos.Length];
            for (int i = 0; i < ReelWindowPos.Length; i++)
            {
                var viewport = UIFactory.CreateMaskedViewport(panelRect, $"Reel{i}_Viewport",
                    new Vector2(ReelWindowPos[i].x, -ReelWindowPos[i].y), ReelWindowSize);

                var stripRect = UIFactory.CreateRect(viewport, "Strip", Vector2.zero,
                    new Vector2(ReelWindowSize.x, ReelWindowSize.y),
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));

                var reel = viewport.gameObject.AddComponent<SlotReel>();
                reel.viewport = viewport;
                reel.strip = stripRect;
                reel.rowHeight = ReelWindowSize.y / 3f;
                reel.visibleRows = 3;
                reels[i] = reel;
            }

            // Glass shine overlay renders last (on top) so it sits above the
            // reel symbols, matching how the source art layers were composed.
            UIFactory.CreateImageTopLeft(panelRect, "GlassShine", SpriteLoader.Get("reel_glass_shine"),
                Vector2.zero, FrameSize, raycastTarget: false);

            return panelRect;
        }

        private void BuildLever(RectTransform framePanel)
        {
            UIFactory.CreateImageTopLeft(framePanel, "Lever", SpriteLoader.Get("lever"), LeverPos, LeverSize, raycastTarget: false);
        }

        private Button BuildSpinHotspot(RectTransform framePanel)
        {
            // Invisible clickable region placed exactly over the red spin
            // button that is already painted into the frame artwork. Uses a
            // colour-tint transition (rather than sprite swap) since there is
            // only one baked-in visual state for this button in the source art.
            var rect = UIFactory.CreateRectTopLeft(framePanel, "SpinHotspot", SpinHotspotPos, SpinHotspotSize);
            var img = rect.gameObject.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.12f);
            colors.pressedColor = new Color(1f, 0.7f, 0.2f, 0.35f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0f);
            button.colors = colors;
            return button;
        }

        private struct HudRefs
        {
            public Text creditsText, betValueText, winText;
            public Button betPlusButton, betMinusButton, spinButton;
        }

        private HudRefs BuildHud(Transform canvasTransform)
        {
            var panelSize = new Vector2(658f, 277f);
            var panel = UIFactory.CreateRect(canvasTransform, "HudPanel", new Vector2(-160f, 40f), panelSize,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            var bg = panel.gameObject.AddComponent<Image>();
            bg.sprite = SpriteLoader.Get("credits_panel_bg");
            bg.raycastTarget = false;

            var refs = new HudRefs();

            refs.creditsText = UIFactory.CreateText(panel, "CreditsText", "CREDITS: 1000", 30, Color.white,
                TextAnchor.MiddleLeft, TopLeft(24, 18), new Vector2(360, 40));

            refs.winText = UIFactory.CreateText(panel, "WinText", "WIN: 0", 24, new Color(1f, 0.85f, 0.2f),
                TextAnchor.MiddleLeft, TopLeft(24, 62), new Vector2(360, 36));

            UIFactory.CreateText(panel, "BetLabel", "BET", 22, Color.white,
                TextAnchor.MiddleLeft, TopLeft(24, 108), new Vector2(90, 36));

            refs.betMinusButton = UIFactory.CreateSpriteButton(panel, "BetMinusButton",
                SpriteLoader.Get("btn_betminus_normal"), SpriteLoader.Get("btn_betminus_hover"),
                SpriteLoader.Get("btn_betminus_pressed"), SpriteLoader.Get("btn_betminus_disabled"),
                TopLeftAnchored(190, 100), new Vector2(46, 46), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));

            refs.betValueText = UIFactory.CreateText(panel, "BetValueText", "10", 26, Color.white,
                TextAnchor.MiddleCenter, TopLeft(244, 108), new Vector2(80, 36));

            refs.betPlusButton = UIFactory.CreateSpriteButton(panel, "BetPlusButton",
                SpriteLoader.Get("btn_betplus_normal"), SpriteLoader.Get("btn_betplus_hover"),
                SpriteLoader.Get("btn_betplus_pressed"), SpriteLoader.Get("btn_betplus_disabled"),
                TopLeftAnchored(332, 100), new Vector2(46, 46), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));

            UIFactory.CreateText(panel, "PaytableLegend",
                "PAYTABLE (x BET)\nBAR x3    CHERRY x8    BELL x15    7-WILD x50",
                15, new Color(0.85f, 0.9f, 1f), TextAnchor.UpperLeft, TopLeft(24, 158), new Vector2(610, 80));

            // Primary SPIN button: a plain, clearly-labelled control so the
            // game is always playable even before noticing the decorative
            // red button built into the machine artwork.
            var spinRect = UIFactory.CreateRect(canvasTransform, "SpinButton", new Vector2(360f, 130f), new Vector2(170f, 170f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            var spinBg = spinRect.gameObject.AddComponent<Image>();
            spinBg.color = new Color(0.75f, 0.12f, 0.1f);
            var spinButtonComp = spinRect.gameObject.AddComponent<Button>();
            spinButtonComp.targetGraphic = spinBg;
            spinButtonComp.transition = Selectable.Transition.ColorTint;
            var spinColors = spinButtonComp.colors;
            spinColors.normalColor = new Color(0.78f, 0.15f, 0.12f);
            spinColors.highlightedColor = new Color(0.9f, 0.25f, 0.18f);
            spinColors.pressedColor = new Color(0.55f, 0.08f, 0.06f);
            spinColors.disabledColor = new Color(0.4f, 0.4f, 0.4f);
            spinButtonComp.colors = spinColors;
            refs.spinButton = spinButtonComp;

            UIFactory.CreateText(spinRect, "Label", "SPIN", 34, Color.white, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(170, 170), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f));

            return refs;

            Vector2 TopLeft(float x, float y) => new Vector2(x, -y);
            Vector2 TopLeftAnchored(float x, float y) => new Vector2(x, -y);
        }

        private struct PopupRefs
        {
            public GameObject root, gambleButtonsGroup;
            public Text titleText, winAmountText, gambleQuestionText;
            public Button yesButton, noButton, closeButton;
        }

        private PopupRefs BuildPopup(Transform canvasTransform)
        {
            var panelSize = new Vector2(700f, 353f);
            var panel = UIFactory.CreateRect(canvasTransform, "WinPopup", Vector2.zero, panelSize,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var bg = panel.gameObject.AddComponent<Image>();
            bg.sprite = SpriteLoader.Get("popup_bg");
            bg.raycastTarget = true; // Blocks clicks from reaching the spin button while shown.

            var refs = new PopupRefs { root = panel.gameObject };

            refs.titleText = UIFactory.CreateText(panel, "Title", "YOU WIN!", 40, new Color(1f, 0.85f, 0.2f),
                TextAnchor.MiddleCenter, new Vector2(0, -50), new Vector2(600, 60),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));

            refs.winAmountText = UIFactory.CreateText(panel, "WinAmount", "+0", 56, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0, -130), new Vector2(600, 70),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));

            refs.gambleQuestionText = UIFactory.CreateText(panel, "GambleQuestion", "Double or nothing?", 22, Color.white,
                TextAnchor.MiddleCenter, new Vector2(0, -200), new Vector2(600, 40),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));

            var gambleGroup = new GameObject("GambleButtons", typeof(RectTransform));
            var gambleRect = (RectTransform)gambleGroup.transform;
            gambleRect.SetParent(panel, false);
            gambleRect.anchorMin = new Vector2(0.5f, 1f);
            gambleRect.anchorMax = new Vector2(0.5f, 1f);
            gambleRect.pivot = new Vector2(0.5f, 1f);
            gambleRect.anchoredPosition = new Vector2(0, -250);
            gambleRect.sizeDelta = new Vector2(500, 90);
            refs.gambleButtonsGroup = gambleGroup;

            refs.yesButton = UIFactory.CreateSpriteButton(gambleRect, "YesButton",
                SpriteLoader.Get("btn_yes_normal"), SpriteLoader.Get("btn_yes_hover"), SpriteLoader.Get("btn_yes_pressed"), null,
                new Vector2(-130, -45), new Vector2(220, 90), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

            refs.noButton = UIFactory.CreateSpriteButton(gambleRect, "NoButton",
                SpriteLoader.Get("btn_no_normal"), SpriteLoader.Get("btn_no_hover"), SpriteLoader.Get("btn_no_pressed"), null,
                new Vector2(130, -45), new Vector2(220, 90), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

            refs.closeButton = UIFactory.CreateSpriteButton(panel, "CloseButton",
                SpriteLoader.Get("btn_close_normal"), SpriteLoader.Get("btn_close_hover"), SpriteLoader.Get("btn_close_pressed"), SpriteLoader.Get("btn_close_disabled"),
                new Vector2(-15, -15), new Vector2(44, 44), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));

            panel.gameObject.SetActive(false);
            return refs;
        }

        private static SymbolDatabase BuildDefaultSymbolDatabase()
        {
            var db = ScriptableObject.CreateInstance<SymbolDatabase>();
            db.symbols.Add(MakeSymbol(SlotSymbolType.Bar, "symbol_bar", "Bar", weight: 42, payout: 3f, wild: false));
            db.symbols.Add(MakeSymbol(SlotSymbolType.Cherry, "symbol_cherry", "Cherry", weight: 30, payout: 8f, wild: false));
            db.symbols.Add(MakeSymbol(SlotSymbolType.Bell, "symbol_bell", "Bell", weight: 20, payout: 15f, wild: false));
            db.symbols.Add(MakeSymbol(SlotSymbolType.Seven, "symbol_seven", "Seven (Wild)", weight: 8, payout: 50f, wild: true));
            return db;
        }

        private static SymbolData MakeSymbol(SlotSymbolType type, string spriteName, string displayName, int weight, float payout, bool wild)
        {
            var data = ScriptableObject.CreateInstance<SymbolData>();
            data.symbolType = type;
            data.sprite = SpriteLoader.Get(spriteName);
            data.weight = weight;
            data.payoutMultiplier = payout;
            data.isWild = wild;
            data.displayName = displayName;
            return data;
        }
    }
}

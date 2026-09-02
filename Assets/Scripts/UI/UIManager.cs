using System.Collections;
using System.Collections.Generic;
using SlotMachine.Audio;
using SlotMachine.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SlotMachine.UI
{
    /// <summary>
    /// Bridges SlotMachineController's gameplay events to the on-screen
    /// widgets built by GameBootstrapper. UIManager owns no gameplay rules
    /// of its own — it only reads state (Credits, Bet, results) and updates
    /// Text/Image components, and forwards button clicks back into the
    /// controller. This keeps the "what happened" logic (SlotMachineController
    /// / PayoutManager) fully separate from the "how it looks" logic (here).
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private SlotMachineController _controller;

        // HUD
        public Text creditsText;
        public Text betValueText;
        public Text winText;
        public Button betPlusButton;
        public Button betMinusButton;
        public Button spinButton;
        public Text spinButtonLabel;

        // Win / Gamble popup
        public GameObject popupRoot;
        public Text popupTitleText;
        public Text popupWinAmountText;
        public Text popupGambleQuestionText;
        public Button popupYesButton;
        public Button popupNoButton;
        public Button popupCloseButton;
        public GameObject gambleButtonsGroup;

        public void Bind(SlotMachineController controller)
        {
            _controller = controller;

            _controller.Credits.OnCreditsChanged += HandleCreditsChanged;
            _controller.Credits.OnBetChanged += HandleBetChanged;
            _controller.OnSpinStarted += HandleSpinStarted;
            _controller.OnSpinResolved += HandleSpinResolved;
            _controller.OnSpinRejectedInsufficientCredits += HandleInsufficientCredits;
            _controller.OnGambleResolved += HandleGambleResolved;

            spinButton.onClick.AddListener(HandleSpinClicked);
            betPlusButton.onClick.AddListener(() => { AudioManager.Instance?.PlayClick(); _controller.IncreaseBet(); });
            betMinusButton.onClick.AddListener(() => { AudioManager.Instance?.PlayClick(); _controller.DecreaseBet(); });
            popupYesButton.onClick.AddListener(HandleGambleYesClicked);
            popupNoButton.onClick.AddListener(HandleGambleNoClicked);
            popupCloseButton.onClick.AddListener(HandleClosePopup);

            RefreshCredits();
            RefreshBet();
            winText.text = "WIN: 0";
            popupRoot.SetActive(false);
        }

        private void HandleSpinClicked()
        {
            if (popupRoot.activeSelf) return; // Don't allow spinning while the win/gamble popup is open.
            AudioManager.Instance?.PlayClick();
            _controller.RequestSpin();
        }

        private void HandleCreditsChanged(float _) => RefreshCredits();
        private void HandleBetChanged(float _) => RefreshBet();

        private void RefreshCredits()
        {
            creditsText.text = $"CREDITS: {Mathf.FloorToInt(_controller.Credits.Credits)}";
        }

        private void RefreshBet()
        {
            betValueText.text = Mathf.FloorToInt(_controller.Credits.Bet).ToString();
        }

        private void HandleSpinStarted()
        {
            winText.text = "WIN: 0";
            SetInteractable(spinButton, false);
            SetInteractable(betPlusButton, false);
            SetInteractable(betMinusButton, false);
        }

        private void HandleSpinResolved(List<PaylineResult> results, float totalWin, bool isBigWin)
        {
            SetInteractable(spinButton, true);
            SetInteractable(betPlusButton, true);
            SetInteractable(betMinusButton, true);

            winText.text = $"WIN: {Mathf.FloorToInt(totalWin)}";

            if (totalWin > 0f)
            {
                ShowWinPopup(totalWin, isBigWin);
            }
        }

        private void HandleInsufficientCredits()
        {
            StopAllCoroutines();
            StartCoroutine(FlashMessage("NOT ENOUGH CREDITS"));
        }

        private IEnumerator FlashMessage(string message)
        {
            string previous = winText.text;
            winText.text = message;
            yield return new WaitForSeconds(1.2f);
            if (winText.text == message)
            {
                winText.text = previous;
            }
        }

        private void ShowWinPopup(float amount, bool isBigWin)
        {
            popupRoot.SetActive(true);
            popupTitleText.text = isBigWin ? "BIG WIN!" : "YOU WIN!";
            popupWinAmountText.text = $"+{Mathf.FloorToInt(amount)}";

            bool canGamble = _controller.CanOfferGamble();
            gambleButtonsGroup.SetActive(canGamble);
            popupGambleQuestionText.text = canGamble ? "Double or nothing?" : "";

            if (canGamble)
            {
                _controller.StartGambleSession();
            }
        }

        private void HandleGambleYesClicked()
        {
            AudioManager.Instance?.PlayClick();
            _controller.PlayGambleRound();
        }

        private void HandleGambleNoClicked()
        {
            AudioManager.Instance?.PlayClick();
            HandleClosePopup();
        }

        private void HandleGambleResolved(bool won, float resultingAmount)
        {
            popupTitleText.text = won ? "DOUBLED!" : "LOST IT ALL";
            popupWinAmountText.text = won ? $"+{Mathf.FloorToInt(resultingAmount)}" : "0";
            winText.text = $"WIN: {Mathf.FloorToInt(resultingAmount)}";

            bool canContinueGambling = won && _controller.CanOfferGamble();
            gambleButtonsGroup.SetActive(canContinueGambling);
            popupGambleQuestionText.text = canContinueGambling ? "Double or nothing again?" : (won ? "Banked!" : "");
        }

        private void HandleClosePopup()
        {
            AudioManager.Instance?.PlayClick();
            popupRoot.SetActive(false);
        }

        private static void SetInteractable(Selectable selectable, bool interactable)
        {
            if (selectable != null) selectable.interactable = interactable;
        }
    }
}

using System.Collections.Generic;
using SlotMachine.Audio;
using SlotMachine.Bonus;
using SlotMachine.Data;
using UnityEngine;

namespace SlotMachine.Core
{
    /// <summary>
    /// Top-level gameplay orchestrator. Owns the three SlotReel instances,
    /// the CreditsManager, PayoutManager and GambleManager, and drives the
    /// spin -> evaluate -> payout -> (optional gamble) sequence. Does not
    /// touch any UI directly — instead it exposes C# events that
    /// SlotMachine.UI.UIManager subscribes to, keeping gameplay logic and
    /// presentation cleanly separated (the same reason CreditsManager is a
    /// plain class rather than a MonoBehaviour).
    /// </summary>
    public class SlotMachineController : MonoBehaviour
    {
        [SerializeField] private GameConfig config = new GameConfig();

        private SlotReel[] _reels;
        private SymbolDatabase _database;
        private RandomGenerator _rng;
        private PayoutManager _payoutManager;

        public CreditsManager Credits { get; private set; }
        public GambleManager Gamble { get; private set; }
        public GameConfig Config => config;

        public bool IsSpinning { get; private set; }
        public float LastWinAmount { get; private set; }

        /// <summary>
        /// Lets the composition root (GameBootstrapper) hand in a fully
        /// populated GameConfig before Init() runs, instead of relying on
        /// the Inspector-assigned default. Safe no-op if called after Init.
        /// </summary>
        public void Configure(GameConfig externalConfig)
        {
            if (externalConfig != null)
            {
                config = externalConfig;
            }
        }

        // ----- Events consumed by SlotMachine.UI.UIManager -----
        public System.Action OnSpinStarted;
        public System.Action<List<PaylineResult>, float, bool> OnSpinResolved; // results, totalWin, isBigWin
        public System.Action OnSpinRejectedInsufficientCredits;
        public System.Action<bool, float> OnGambleResolved; // won?, resultingAmount

        public void Init(SlotReel[] reels, SymbolDatabase database)
        {
            _reels = reels;
            _database = database;
            _rng = new RandomGenerator();
            _payoutManager = new PayoutManager
            {
                paylineCount = config.paylineCount,
                enableNearMissBonus = config.enableNearMissBonus
            };

            Credits = new CreditsManager(config.startingCredits, config.defaultBet, config.minBet, config.maxBet, config.betStep);
            Gamble = new GambleManager(_rng, config.gambleWinChance, config.maxGambleRounds);

            foreach (var reel in _reels)
            {
                reel.Init(_database, _rng);
            }
        }

        public void RequestSpin()
        {
            if (IsSpinning) return;

            if (!Credits.TryPlaceBet())
            {
                OnSpinRejectedInsufficientCredits?.Invoke();
                return;
            }

            IsSpinning = true;
            LastWinAmount = 0f;
            OnSpinStarted?.Invoke();
            AudioManager.Instance?.PlaySpinStart();

            int remaining = _reels.Length;
            for (int i = 0; i < _reels.Length; i++)
            {
                float delay = i * config.staggerDelayPerReel;
                _reels[i].Spin(delay, () =>
                {
                    AudioManager.Instance?.PlayReelStop();
                    remaining--;
                    if (remaining == 0)
                    {
                        ResolveSpin();
                    }
                });
            }
        }

        private void ResolveSpin()
        {
            var grid = new SymbolData[_reels.Length][];
            for (int i = 0; i < _reels.Length; i++)
            {
                grid[i] = _reels[i].CurrentVisible;
            }

            var results = _payoutManager.EvaluateSpin(grid, Credits.Bet);

            float totalWin = 0f;
            foreach (var r in results)
            {
                if (r.isWin) totalWin += r.payoutAmount;
            }

            bool isBigWin = totalWin >= Credits.Bet * 20f;

            if (totalWin > 0f)
            {
                Credits.AddCredits(totalWin);
                AudioManager.Instance?.PlayWin(isBigWin);
            }

            LastWinAmount = totalWin;
            IsSpinning = false;
            OnSpinResolved?.Invoke(results, totalWin, isBigWin);
        }

        // ----- Gamble ("Double or Nothing") bonus feature -----

        public bool CanOfferGamble()
        {
            return config.enableGambleFeature && LastWinAmount > 0f && !Gamble.RoundsExhausted;
        }

        public void StartGambleSession()
        {
            Gamble.ResetRounds();
        }

        /// <summary>Player chose to risk their current win on a coin flip.</summary>
        public void PlayGambleRound()
        {
            bool won = Gamble.PlayRound();
            if (won)
            {
                // The original win amount is already sitting in the
                // player's balance (credited back in ResolveSpin), so a
                // successful gamble just credits the same amount again.
                Credits.AddCredits(LastWinAmount);
                LastWinAmount *= 2f;
                AudioManager.Instance?.PlayWin(true);
            }
            else
            {
                // A lost gamble claws back the winnings that were credited
                // after the spin (and, on a second+ round, after the
                // previous successful gamble).
                Credits.RemoveCredits(LastWinAmount);
                LastWinAmount = 0f;
                AudioManager.Instance?.PlayLose();
            }
            OnGambleResolved?.Invoke(won, LastWinAmount);
        }

        public void IncreaseBet()
        {
            if (IsSpinning) return;
            Credits.IncreaseBet();
            AudioManager.Instance?.PlayClick();
        }

        public void DecreaseBet()
        {
            if (IsSpinning) return;
            Credits.DecreaseBet();
            AudioManager.Instance?.PlayClick();
        }
    }
}

using System;
using UnityEngine;

namespace SlotMachine.Core
{
    /// <summary>
    /// Owns the player's credit balance and current bet. Exposes simple
    /// C# events so any number of UI elements can react to a balance/bet
    /// change without CreditsManager needing to know anything about the UI
    /// (keeps gameplay state decoupled from presentation).
    /// </summary>
    public class CreditsManager
    {
        public event Action<float> OnCreditsChanged;
        public event Action<float> OnBetChanged;

        public float Credits { get; private set; }
        public float Bet { get; private set; }

        public float MinBet { get; }
        public float MaxBet { get; }
        public float BetStep { get; }

        public CreditsManager(float startingCredits, float defaultBet, float minBet, float maxBet, float betStep)
        {
            Credits = startingCredits;
            Bet = defaultBet;
            MinBet = minBet;
            MaxBet = maxBet;
            BetStep = betStep;
        }

        public bool CanAffordBet()
        {
            return Credits >= Bet;
        }

        /// <summary>Deducts the current bet from the balance. Call before spinning.</summary>
        public bool TryPlaceBet()
        {
            if (!CanAffordBet()) return false;
            Credits -= Bet;
            OnCreditsChanged?.Invoke(Credits);
            return true;
        }

        public void AddCredits(float amount)
        {
            if (amount <= 0f) return;
            Credits += amount;
            OnCreditsChanged?.Invoke(Credits);
        }

        /// <summary>Used by the gamble feature when a double-or-nothing attempt is lost.</summary>
        public void RemoveCredits(float amount)
        {
            if (amount <= 0f) return;
            Credits = Mathf.Max(0f, Credits - amount);
            OnCreditsChanged?.Invoke(Credits);
        }

        public void IncreaseBet()
        {
            Bet = Mathf.Min(MaxBet, Bet + BetStep);
            OnBetChanged?.Invoke(Bet);
        }

        public void DecreaseBet()
        {
            Bet = Mathf.Max(MinBet, Bet - BetStep);
            OnBetChanged?.Invoke(Bet);
        }
    }
}

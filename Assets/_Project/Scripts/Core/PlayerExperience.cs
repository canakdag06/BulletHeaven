using System;
using UnityEngine;

namespace BulletHeaven.Core
{
    /// <summary>
    /// Owns the player's EXP, level and level-up curve.
    /// Other systems (UI, upgrade picker, audio…) only subscribe to the events below.
    /// </summary>
    public class PlayerExperience : MonoBehaviour
    {
        [Header("Level Curve  —  required = base * level ^ exponent")]
        [SerializeField, Min(1)]  private int   baseExpRequirement = 5;
        [SerializeField, Min(1f)] private float growthExponent     = 1.5f;
        [SerializeField, Min(1)]  private int   maxLevel           = 99;

        public int  CurrentLevel      { get; private set; } = 1;
        public int  CurrentExp        { get; private set; }
        public int  ExpToNextLevel    { get; private set; }
        public bool IsMaxLevel        => CurrentLevel >= maxLevel;
        public float Progress01       => ExpToNextLevel > 0 ? (float)CurrentExp / ExpToNextLevel : 1f;

        /// <summary>Fired whenever EXP changes. Params: (currentExp, expToNextLevel)</summary>
        public event Action<int, int> OnExperienceChanged;

        /// <summary>Fired once per level gained. Param: newLevel</summary>
        public event Action<int> OnLevelUp;

        private void Awake()
        {
            ExpToNextLevel = GetRequiredExp(CurrentLevel);
        }

        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnRoundReset += ResetProgress;

            OnExperienceChanged?.Invoke(CurrentExp, ExpToNextLevel);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnRoundReset -= ResetProgress;
        }

        // ── Public ────────────────────────────────────────────────────────────

        public void AddExperience(int amount)
        {
            if (amount <= 0 || IsMaxLevel) return;

            CurrentExp += amount;

            // A single big gem may grant several levels at once.
            while (!IsMaxLevel && CurrentExp >= ExpToNextLevel)
            {
                CurrentExp    -= ExpToNextLevel;
                CurrentLevel++;
                ExpToNextLevel = GetRequiredExp(CurrentLevel);
                OnLevelUp?.Invoke(CurrentLevel);
            }

            if (IsMaxLevel) CurrentExp = 0;

            OnExperienceChanged?.Invoke(CurrentExp, ExpToNextLevel);
        }

        public int GetRequiredExp(int level)
        {
            return Mathf.Max(1, Mathf.RoundToInt(baseExpRequirement * Mathf.Pow(level, growthExponent)));
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void ResetProgress()
        {
            CurrentLevel   = 1;
            CurrentExp     = 0;
            ExpToNextLevel = GetRequiredExp(CurrentLevel);
            OnExperienceChanged?.Invoke(CurrentExp, ExpToNextLevel);
        }
    }
}

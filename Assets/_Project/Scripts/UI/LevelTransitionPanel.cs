using TMPro;
using UnityEngine;
using DG.Tweening;
using BulletHeaven.Core;

namespace BulletHeaven.UI
{
    /// <summary>
    /// Handles the full-screen fade that plays during a level transition.
    /// </summary>
    public class LevelTransitionPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup overlay;
        [SerializeField] private TextMeshProUGUI levelLabel;

        [Header("Timing")]
        [SerializeField] private float fadeDuration = 0.45f;
        [SerializeField] private float holdDuration = 0.6f;

        private Sequence _sequence;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            overlay.alpha          = 0f;
            overlay.interactable   = false;
            overlay.blocksRaycasts = false;

            if (levelLabel != null)
                levelLabel.alpha = 0f;
        }

        private void Start()
        {
            GameManager.Instance.OnLevelTransitionStarted += PlayTransition;
        }

        private void OnDestroy()
        {
            _sequence?.Kill();

            if (GameManager.Instance != null)
                GameManager.Instance.OnLevelTransitionStarted -= PlayTransition;
        }

        // ── Transition sequence ───────────────────────────────────────────────

        private void PlayTransition()
        {
            _sequence?.Kill();

            overlay.blocksRaycasts = true;

            _sequence = DOTween.Sequence()
                // 1. Fade overlay to black
                .Append(overlay.DOFade(1f, fadeDuration).SetEase(Ease.InQuad))
                // 2. Heavy work while screen is invisible
                .AppendCallback(OnScreenBlack)
                // 3. Brief hold (lets the map finish initializing)
                .AppendInterval(holdDuration)
                // 4. Fade back in
                .Append(overlay.DOFade(0f, fadeDuration).SetEase(Ease.OutQuad))
                // 5. Hand control back to GameManager
                .AppendCallback(OnTransitionComplete);
        }

        private void OnScreenBlack()
        {
            GameManager gm = GameManager.Instance;
            gm.EnemySpawner?.ClearAllEnemies();
            gm.SpawnMap();

            LevelData data = gm.LevelConfig?.GetLevel(gm.CurrentLevel);
            Vector3 spawnPos = data != null ? data.playerSpawnPoint : Vector3.zero;
            gm.SetPlayerPosition(spawnPos);

            // Reset round stats while the screen is black so TimerDisplay shows
            // the correct value as soon as the overlay fades back in.
            gm.ResetRoundStats();

            if (levelLabel != null)
            {
                levelLabel.text  = $"Level {gm.CurrentLevel} is loading...";
                levelLabel.alpha = 1f;
            }
        }

        private void OnTransitionComplete()
        {
            overlay.blocksRaycasts = false;

            if (levelLabel != null)
                levelLabel.alpha = 0f;

            GameManager.Instance.FinishTransition();
        }
    }
}

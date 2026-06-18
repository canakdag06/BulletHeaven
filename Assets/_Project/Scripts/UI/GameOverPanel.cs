using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using BulletHeaven.Core;

namespace BulletHeaven.UI
{
    public class GameOverPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI levelKillsText;
        [SerializeField] private TextMeshProUGUI totalKillsText;
        [SerializeField] private Button          retryButton;

        [Header("Animation")]
        [SerializeField] private float showDuration = 0.35f;
        [SerializeField] private float hideDuration = 0.25f;
        [SerializeField] private float scaleFrom    = 0.85f;

        private CanvasGroup _canvasGroup;
        private Sequence    _sequence;
        private bool        _isVisible;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();

            _canvasGroup.alpha          = 0f;
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;

            retryButton.onClick.AddListener(OnRetryClicked);
        }

        private void Start()
        {
            GameManager.Instance.OnGameOver   += AnimateIn;
            GameManager.Instance.OnRoundReset += AnimateOut;
        }

        private void OnDestroy()
        {
            _sequence?.Kill();

            if (GameManager.Instance == null) return;
            GameManager.Instance.OnGameOver   -= AnimateIn;
            GameManager.Instance.OnRoundReset -= AnimateOut;
        }

        // ── Event handlers ───────────────────────────────────────────────────

        private void OnRetryClicked()
        {
            AnimateOut();
            GameManager.Instance.RetryLevel();
        }

        // ── Animations ───────────────────────────────────────────────────────

        private void AnimateIn()
        {
            if (levelKillsText != null)
                levelKillsText.text = GameManager.Instance.EnemiesDefeatedThisRun.ToString();

            if (totalKillsText != null)
                totalKillsText.text = GameManager.Instance.TotalEnemiesDefeated.ToString();

            _isVisible = true;
            _sequence?.Kill();

            transform.localScale        = Vector3.one * scaleFrom;
            _canvasGroup.alpha          = 0f;
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;

            _sequence = DOTween.Sequence()
                .Join(_canvasGroup.DOFade(1f, showDuration).SetEase(Ease.OutQuad))
                .Join(transform.DOScale(Vector3.one, showDuration).SetEase(Ease.OutBack))
                .OnComplete(() =>
                {
                    _canvasGroup.interactable   = true;
                    _canvasGroup.blocksRaycasts = true;
                });
        }

        private void AnimateOut()
        {
            if (!_isVisible) return;

            _isVisible = false;
            _sequence?.Kill();
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;

            _sequence = DOTween.Sequence()
                .Join(_canvasGroup.DOFade(0f, hideDuration).SetEase(Ease.InQuad))
                .Join(transform.DOScale(scaleFrom, hideDuration).SetEase(Ease.InBack));
        }
    }
}

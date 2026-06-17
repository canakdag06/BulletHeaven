using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using BulletHeaven.Core;

namespace BulletHeaven.UI
{
    public class GameWonPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI levelKillsText;
        [SerializeField] private TextMeshProUGUI totalKillsText;
        [SerializeField] private Button          nextLevelButton;

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

            nextLevelButton.onClick.AddListener(OnNextLevelClicked);
        }

        private void Start()
        {
            GameManager.Instance.OnLevelComplete += OnLevelComplete;
            GameManager.Instance.OnRoundReset    += AnimateOut;
        }

        private void OnDestroy()
        {
            _sequence?.Kill();

            if (GameManager.Instance == null) return;
            GameManager.Instance.OnLevelComplete -= OnLevelComplete;
            GameManager.Instance.OnRoundReset    -= AnimateOut;
        }

        // ── Event handlers ───────────────────────────────────────────────────

        private void OnLevelComplete(int levelKills, int totalKills, bool isLastLevel)
        {
            levelKillsText.text = levelKills.ToString();
            totalKillsText.text = totalKills.ToString();

            nextLevelButton.gameObject.SetActive(true);

            AnimateIn();
        }

        private void OnNextLevelClicked()
        {
            AnimateOut();
            GameManager.Instance.GoToNextLevel();
        }

        // ── Animations ───────────────────────────────────────────────────────

        private void AnimateIn()
        {
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

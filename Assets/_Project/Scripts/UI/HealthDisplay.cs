using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using BulletHeaven.Core;

namespace BulletHeaven.UI
{
    public class HealthDisplay : MonoBehaviour
    {
        [SerializeField] private Image            healthFill;
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("Fill")]
        [SerializeField] private float fillDuration = 0.25f;
        [SerializeField] private Ease  fillEase     = Ease.OutQuad;

        [Header("Hit Feedback")]
        [SerializeField] private Color hitColor       = Color.red;
        [SerializeField] private float colorDuration  = 0.25f;
        [SerializeField] private float punchStrength  = 0.3f;
        [SerializeField] private float punchDuration  = 0.3f;

        private PlayerHealth _playerHealth;
        private float        _lastHealth;
        private Color        _normalColor;
        private Tween        _colorTween;
        private Tween        _punchTween;
        private Tween        _fillTween;

        private void Start()
        {
            _playerHealth = FindFirstObjectByType<PlayerHealth>();

            if (_playerHealth == null)
            {
                Debug.LogWarning("[HealthDisplay] PlayerHealth not found.");
                return;
            }

            _normalColor = healthText != null ? healthText.color : Color.white;
            _lastHealth  = _playerHealth.CurrentHealth;

            _playerHealth.OnHealthChanged += UpdateDisplay;

            if (GameManager.Instance != null)
                GameManager.Instance.OnRoundReset += OnRoundReset;

            Refresh(_playerHealth.CurrentHealth, _playerHealth.MaxHealth, instant: true);
        }

        private void OnDestroy()
        {
            KillTweens();
            _fillTween?.Kill();

            if (_playerHealth != null)
                _playerHealth.OnHealthChanged -= UpdateDisplay;

            if (GameManager.Instance != null)
                GameManager.Instance.OnRoundReset -= OnRoundReset;
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void OnRoundReset()
        {
            if (_playerHealth == null) return;

            _lastHealth = _playerHealth.CurrentHealth;
            KillTweens();
            _fillTween?.Kill();
            Refresh(_playerHealth.CurrentHealth, _playerHealth.MaxHealth, instant: true);
        }

        private void UpdateDisplay(float current, float max)
        {
            bool tookDamage = current < _lastHealth;
            _lastHealth = current;

            Refresh(current, max, instant: false);

            if (tookDamage)
                PlayHitFeedback();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void Refresh(float current, float max, bool instant)
        {
            if (healthText != null)
                healthText.SetText("{0}", Mathf.CeilToInt(current));

            if (healthFill == null || max <= 0f) return;

            float target = Mathf.Clamp01(current / max);
            _fillTween?.Kill();

            if (instant)
            {
                healthFill.fillAmount = target;
                return;
            }

            _fillTween = healthFill
                .DOFillAmount(target, fillDuration)
                .SetEase(fillEase);
        }

        private void PlayHitFeedback()
        {
            KillTweens();

            if (healthText != null)
            {
                _colorTween = healthText
                    .DOColor(hitColor, colorDuration * 0.5f)
                    .SetEase(Ease.OutQuad)
                    .SetLoops(2, LoopType.Yoyo)
                    .OnComplete(() => healthText.color = _normalColor);
            }

            _punchTween = transform
                .DOPunchScale(Vector3.one * punchStrength, punchDuration, vibrato: 1, elasticity: 0.5f)
                .OnComplete(() => transform.localScale = Vector3.one);
        }

        private void KillTweens()
        {
            _colorTween?.Kill(complete: true);
            _punchTween?.Kill(complete: true);
            _colorTween = null;
            _punchTween = null;

            transform.localScale = Vector3.one;

            if (healthText != null)
                healthText.color = _normalColor;
        }
    }
}

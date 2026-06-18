using TMPro;
using UnityEngine;
using DG.Tweening;
using BulletHeaven.Core;

namespace BulletHeaven.UI
{
    public class HealthDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI healthText;

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

            RefreshText(_playerHealth.CurrentHealth);
        }

        private void OnDestroy()
        {
            _colorTween?.Kill();
            _punchTween?.Kill();

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
            RefreshText(_playerHealth.CurrentHealth);
        }

        private void UpdateDisplay(float current, float max)
        {
            bool tookDamage = current < _lastHealth;
            _lastHealth = current;

            RefreshText(current);

            if (tookDamage)
                PlayHitFeedback();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void RefreshText(float current)
        {
            if (healthText == null) return;
            healthText.SetText("{0}", Mathf.CeilToInt(current));
        }

        private void PlayHitFeedback()
        {
            if (healthText == null) return;

            KillTweens();

            _colorTween = healthText
                .DOColor(hitColor, colorDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .SetLoops(2, LoopType.Yoyo)
                .OnComplete(() => healthText.color = _normalColor);

            _punchTween = healthText.rectTransform
                .DOPunchScale(Vector3.one * punchStrength, punchDuration, vibrato: 1, elasticity: 0.5f)
                .OnComplete(() => healthText.rectTransform.localScale = Vector3.one);
        }

        private void KillTweens()
        {
            _colorTween?.Kill(complete: true);
            _punchTween?.Kill(complete: true);
            _colorTween = null;
            _punchTween = null;

            if (healthText != null)
            {
                healthText.color                    = _normalColor;
                healthText.rectTransform.localScale = Vector3.one;
            }
        }
    }
}

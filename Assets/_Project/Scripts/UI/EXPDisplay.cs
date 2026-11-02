using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using BulletHeaven.Core;

namespace BulletHeaven.UI
{
    public class EXPDisplay : MonoBehaviour
    {
        [SerializeField] private Image            expFill;
        [SerializeField] private Image            ghostFill;
        [SerializeField] private TextMeshProUGUI levelText;

        [Header("Fill")]
        [SerializeField] private float fillDuration = 0.25f;
        [SerializeField] private Ease  fillEase     = Ease.OutQuad;

        [Header("Level Up Feedback")]
        [SerializeField] private float punchStrength = 0.15f;
        [SerializeField] private float punchDuration = 0.3f;

        private PlayerExperience _playerExperience;
        private int              _pendingLevelUps;
        private Tween            _fillTween;
        private Tween            _punchTween;

        private void Start()
        {
            _playerExperience = FindFirstObjectByType<PlayerExperience>();

            if (_playerExperience == null)
            {
                Debug.LogWarning("[EXPDisplay] PlayerExperience not found.");
                return;
            }

            _playerExperience.OnExperienceChanged += UpdateDisplay;
            _playerExperience.OnLevelUp           += OnLevelUp;

            if (GameManager.Instance != null)
                GameManager.Instance.OnRoundReset += OnRoundReset;

            Refresh(instant: true);
        }

        private void OnDestroy()
        {
            _fillTween?.Kill();
            _punchTween?.Kill();

            if (_playerExperience != null)
            {
                _playerExperience.OnExperienceChanged -= UpdateDisplay;
                _playerExperience.OnLevelUp           -= OnLevelUp;
            }

            if (GameManager.Instance != null)
                GameManager.Instance.OnRoundReset -= OnRoundReset;
        }

        // ── Event handlers ────────────────────────────────────────────────────

        // OnLevelUp fires before OnExperienceChanged within AddExperience.
        private void OnLevelUp(int newLevel) => _pendingLevelUps++;

        private void UpdateDisplay(int currentExp, int expToNextLevel)
        {
            bool leveledUp = _pendingLevelUps > 0;
            _pendingLevelUps = 0;

            Refresh(instant: false, leveledUp);

            if (leveledUp)
                PlayLevelUpFeedback();
        }

        private void OnRoundReset()
        {
            _pendingLevelUps = 0;
            _punchTween?.Kill(complete: true);
            if (levelText != null)
                levelText.rectTransform.localScale = Vector3.one;
            Refresh(instant: true);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private float GetTarget()
        {
            return _playerExperience.IsMaxLevel ? 1f : Mathf.Clamp01(_playerExperience.Progress01);
        }

        private void Refresh(bool instant, bool leveledUp = false)
        {
            if (levelText != null)
                levelText.SetText("{0}", _playerExperience.CurrentLevel);

            if (expFill == null) return;

            float target = GetTarget();
            _fillTween?.Kill();

            if (instant)
            {
                SetFill(ghostFill, target);
                SetFill(expFill, target);
                return;
            }

            Sequence sequence = DOTween.Sequence();

            if (leveledUp)
            {
                AppendCatchUp(sequence, 1f);
                sequence.AppendCallback(() =>
                {
                    SetFill(ghostFill, 0f);
                    SetFill(expFill, 0f);
                });
            }

            AppendCatchUp(sequence, target);
            sequence.OnComplete(() =>
            {
                SetFill(ghostFill, target);
                SetFill(expFill, target);
            });

            _fillTween = sequence;
        }

        // Ghost reaches the value first; the real fill follows so the gain stays visible.
        private void AppendCatchUp(Sequence sequence, float value)
        {
            if (ghostFill != null)
            {
                sequence.AppendCallback(() => { if (value > 0f) ghostFill.enabled = true; });
                sequence.Append(AnimateFill(ghostFill.rectTransform, value));
            }

            sequence.AppendCallback(() => { if (value > 0f) expFill.enabled = true; });
            sequence.Append(AnimateFill(expFill.rectTransform, value));
        }

        private Tween AnimateFill(RectTransform fillRect, float value)
        {
            return fillRect
                .DOAnchorMax(new Vector2(value, fillRect.anchorMax.y), fillDuration)
                .SetEase(fillEase);
        }

        private void SetFill(Image image, float value)
        {
            if (image == null) return;

            RectTransform fillRect = image.rectTransform;
            fillRect.anchorMax     = new Vector2(value, fillRect.anchorMax.y);
            image.enabled          = value > 0f;
        }

        private void PlayLevelUpFeedback()
        {
            if (levelText == null) return;

            _punchTween?.Kill(complete: true);
            _punchTween = levelText.rectTransform
                .DOPunchScale(Vector3.one * punchStrength, punchDuration, vibrato: 1, elasticity: 0.5f)
                .OnComplete(() => levelText.rectTransform.localScale = Vector3.one);
        }
    }
}

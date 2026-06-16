using TMPro;
using UnityEngine;
using BulletHeaven.Core;
using DG.Tweening;

namespace BulletHeaven.UI
{
    public class TimerDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Urgency Effect (< 10s)")]
        [SerializeField] private float punchScale      = 1.25f;
        [SerializeField] private float punchDuration   = 0.4f;
        [SerializeField] private Color urgencyColor   = Color.yellow;

        private Tween _pulseTween;
        private float _normalFontSize;
        private Color _normalColor;
        private bool  _urgencyActive;

        private void Start()
        {
            _normalFontSize = timerText.fontSize;
            _normalColor = timerText.color;
            GameManager.Instance.OnTimerSecondChanged += UpdateTimerDisplay;
            UpdateTimerDisplay(Mathf.CeilToInt(GameManager.Instance.RoundTimer));
        }

        private void OnDestroy()
        {
            _pulseTween?.Kill();

            if (GameManager.Instance != null)
                GameManager.Instance.OnTimerSecondChanged -= UpdateTimerDisplay;
        }

        private void UpdateTimerDisplay(int seconds)
        {
            int m = seconds / 60;
            int s = seconds % 60;
            timerText.text = $"{m:D2}:{s:D2}";

            if (seconds < 10)
                EnterUrgency();
            else
                ExitUrgency();
        }

        private void EnterUrgency()
        {            
            timerText.color = urgencyColor;

            _pulseTween?.Kill(complete: true);

            _pulseTween = timerText.transform
                .DOPunchScale(Vector3.one * (punchScale - 1f), punchDuration, vibrato: 1, elasticity: 0.5f)
                .OnComplete(() => timerText.transform.localScale = Vector3.one);
                
            _urgencyActive = true;
        }

        private void ExitUrgency()
        {
            if (!_urgencyActive) return;

            _pulseTween?.Kill(complete: true);
            timerText.transform.localScale = Vector3.one;
            timerText.fontSize = _normalFontSize;
            timerText.color    = _normalColor;
            _urgencyActive     = false;
        }
    }
}

using TMPro;
using UnityEngine;
using BulletHeaven.Core;

namespace BulletHeaven.UI
{
    public class TimerDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timerText;

        private void Start()
        {
            GameManager.Instance.OnTimerSecondChanged += UpdateTimerDisplay;
            UpdateTimerDisplay(Mathf.CeilToInt(GameManager.Instance.RoundTimer));
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnTimerSecondChanged -= UpdateTimerDisplay;
        }

        private void UpdateTimerDisplay(int seconds)
        {
            int m = seconds / 60;
            int s = seconds % 60;
            timerText.text = $"{m:D2}:{s:D2}";
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using BulletHeaven.Loadout;

namespace BulletHeaven.UI
{
    /// <summary>
    /// One upgrade card: icon, name, level line, description and the next-rank line.
    /// Views are assigned in the inspector, or filled by <see cref="Construct"/> when the panel builds them.
    /// </summary>
    public class LevelUpCardView : MonoBehaviour
    {
        private static readonly Color HealTint = new(0.45f, 0.78f, 0.5f, 1f);
        private static readonly Color ItemTint = new(0.45f, 0.55f, 0.72f, 1f);

        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI upgradeDescriptionText;
        [SerializeField] private Button button;

        private Sprite _blankSprite;

        public void Construct(
            Image iconImage,
            TextMeshProUGUI nameLabel,
            TextMeshProUGUI levelLabel,
            TextMeshProUGUI descriptionLabel,
            TextMeshProUGUI upgradeLabel,
            Button cardButton,
            Sprite blank)
        {
            icon                   = iconImage;
            nameText               = nameLabel;
            levelText              = levelLabel;
            descriptionText        = descriptionLabel;
            upgradeDescriptionText = upgradeLabel;
            button                 = cardButton;
            _blankSprite           = blank;
        }

        public void SetClickHandler(UnityAction callback)
        {
            if (button == null) return;

            button.onClick.RemoveAllListeners();
            if (callback != null)
                button.onClick.AddListener(callback);
        }

        public void SetInteractable(bool value)
        {
            if (button != null)
                button.interactable = value;
        }

        public void SetVisible(bool value) => gameObject.SetActive(value);

        public void Bind(UpgradeOffer offer)
        {
            if (nameText != null)
                nameText.text = string.IsNullOrEmpty(offer.DisplayName) ? "Upgrade" : offer.DisplayName;

            if (descriptionText != null)
                descriptionText.text = offer.Description ?? string.Empty;

            if (upgradeDescriptionText != null)
                upgradeDescriptionText.text = offer.UpgradeDescription ?? string.Empty;

            if (levelText != null)
                levelText.text = LevelLabel(offer);

            if (icon == null) return;

            bool hasIcon = offer.Icon != null;
            Sprite sprite = hasIcon ? offer.Icon : _blankSprite;
            if (sprite != null)
                icon.sprite = sprite;

            icon.color = hasIcon ? Color.white : (offer.IsFallback ? HealTint : ItemTint);
            icon.preserveAspect = hasIcon;
            icon.enabled = icon.sprite != null;
        }

        private static string LevelLabel(UpgradeOffer offer)
        {
            if (offer.IsFallback) return string.Empty;
            if (offer.CurrentLevel <= 0) return "NEW";
            return $"Lv.{offer.CurrentLevel} -> {offer.NextLevel}";
        }
    }
}

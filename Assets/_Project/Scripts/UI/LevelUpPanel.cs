using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using BulletHeaven.Core;
using BulletHeaven.Loadout;

namespace BulletHeaven.UI
{
    /// <summary>
    /// Level-up picker. Tweens are unscaled because the game is paused at timeScale 0.
    /// A second offer set refreshes the cards in place instead of closing the panel.
    /// </summary>
    public class LevelUpPanel : MonoBehaviour
    {
        [SerializeField] private LevelUpCardView[] cards;

        [Header("Animation")]
        [SerializeField] private float showDuration = 0.28f;
        [SerializeField] private float hideDuration = 0.18f;
        [SerializeField] private float scaleFrom    = 0.94f;

        private readonly List<UpgradeOffer> _offers = new();

        private CanvasGroup   _canvasGroup;
        private RectTransform _content;
        private Sequence      _sequence;
        private bool          _isVisible;
        private bool          _selectionLocked;
        private Sprite        _blankSprite;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            EnsureCards();
            _content = transform.Find("Content") as RectTransform;
            WireCards();
            HideImmediate();
        }

        private void Start()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[LevelUpPanel] GameManager is missing.", this);
                return;
            }

            GameManager.Instance.OnLevelUpOffers += ShowOffers;
            GameManager.Instance.OnLevelUpClosed += Hide;
            GameManager.Instance.OnRoundReset    += Hide;
        }

        private void OnDestroy()
        {
            _sequence?.Kill();

            if (GameManager.Instance == null) return;
            GameManager.Instance.OnLevelUpOffers -= ShowOffers;
            GameManager.Instance.OnLevelUpClosed -= Hide;
            GameManager.Instance.OnRoundReset    -= Hide;
        }

        // ── Offers ────────────────────────────────────────────────────────────

        private void ShowOffers(IReadOnlyList<UpgradeOffer> offers)
        {
            _selectionLocked = false;
            _offers.Clear();

            if (offers != null)
            {
                for (int i = 0; i < offers.Count; i++)
                    _offers.Add(offers[i]);
            }

            BindCards();

            if (transform is RectTransform rect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

            if (_isVisible)
            {
                _canvasGroup.interactable   = true;
                _canvasGroup.blocksRaycasts = true;
                SetCardsInteractable(true);
                return;
            }

            AnimateIn();
        }

        private void OnCardClicked(int index)
        {
            if (_selectionLocked || index < 0 || index >= _offers.Count) return;

            GameManager manager = GameManager.Instance;
            if (manager == null) return;

            _selectionLocked = true;
            SetCardsInteractable(false);
            manager.SelectUpgrade(_offers[index]);

            // A rejected offer leaves the overlay up. Unlock so another card can be chosen.
            if (manager.StateMachine.CurrentState is LevelUpState)
            {
                _selectionLocked = false;
                SetCardsInteractable(true);
            }
        }

        private void BindCards()
        {
            if (cards == null) return;

            for (int i = 0; i < cards.Length; i++)
            {
                LevelUpCardView card = cards[i];
                if (card == null) continue;

                bool hasOffer = i < _offers.Count;
                card.SetVisible(hasOffer);
                if (!hasOffer) continue;

                card.Bind(_offers[i]);
                card.SetInteractable(!_selectionLocked);
            }
        }

        private void SetCardsInteractable(bool value)
        {
            if (cards == null) return;

            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] != null && cards[i].gameObject.activeSelf)
                    cards[i].SetInteractable(value);
            }
        }

        private void WireCards()
        {
            if (cards == null) return;

            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null) continue;

                int index = i;
                cards[i].SetClickHandler(() => OnCardClicked(index));
            }
        }

        // ── Animation ─────────────────────────────────────────────────────────

        private void AnimateIn()
        {
            _isVisible = true;
            _sequence?.Kill();

            RectTransform scaled = ScaledRoot;
            scaled.localScale           = Vector3.one * scaleFrom;
            _canvasGroup.alpha          = 0f;
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = true;

            _sequence = DOTween.Sequence()
                .SetUpdate(true)
                .Join(_canvasGroup.DOFade(1f, showDuration).SetEase(Ease.OutQuad).SetUpdate(true))
                .Join(scaled.DOScale(Vector3.one, showDuration).SetEase(Ease.OutBack).SetUpdate(true))
                .OnComplete(() =>
                {
                    if (!_isVisible) return;

                    _canvasGroup.interactable   = true;
                    _canvasGroup.blocksRaycasts = true;
                    SetCardsInteractable(!_selectionLocked);
                });
        }

        private void Hide()
        {
            if (!_isVisible && _canvasGroup.alpha <= 0f) return;

            _isVisible = false;
            _selectionLocked = true;
            _sequence?.Kill();
            SetCardsInteractable(false);

            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;

            _sequence = DOTween.Sequence()
                .SetUpdate(true)
                .Join(_canvasGroup.DOFade(0f, hideDuration).SetEase(Ease.InQuad).SetUpdate(true))
                .Join(ScaledRoot.DOScale(scaleFrom, hideDuration).SetEase(Ease.InQuad).SetUpdate(true));
        }

        private void HideImmediate()
        {
            _isVisible = false;
            _selectionLocked = true;
            _sequence?.Kill();

            ScaledRoot.localScale       = Vector3.one;
            _canvasGroup.alpha          = 0f;
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;
        }

        // Scale the card column when the layout built one, so the dimmer stays full-screen.
        private RectTransform ScaledRoot => _content != null ? _content : (RectTransform)transform;

        // ── Built layout ──────────────────────────────────────────────────────
        // Used when the inspector has no cards yet, so a level-up still has something to click.

        private void EnsureCards()
        {
            if (cards != null && cards.Length > 0) return;

            _blankSprite = CreateBlankSprite();
            BuildChrome();
            cards = new LevelUpCardView[GameManager.UpgradeOfferCount];

            RectTransform row = CreateRow();
            for (int i = 0; i < cards.Length; i++)
                cards[i] = CreateCard(row, i);
        }

        private void BuildChrome()
        {
            RectTransform dim = CreateRect("Dim", transform);
            Stretch(dim);
            Image dimImage = dim.gameObject.AddComponent<Image>();
            dimImage.sprite = _blankSprite;
            dimImage.color = new Color(0f, 0f, 0f, 0.72f);
            dimImage.raycastTarget = true;
            dim.SetAsFirstSibling();

            RectTransform content = CreateRect("Content", transform);
            content.anchorMin = new Vector2(0f, 0.5f);
            content.anchorMax = new Vector2(1f, 0.5f);
            content.pivot     = new Vector2(0.5f, 0.5f);
            content.sizeDelta = new Vector2(-48f, 920f);

            VerticalLayoutGroup column = content.gameObject.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(8, 8, 0, 0);
            column.spacing = 28f;
            column.childAlignment = TextAnchor.MiddleCenter;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            TextMeshProUGUI title = CreateText("Title", content, "Choose an upgrade", 52, FontStyles.Bold, Color.white);
            LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 80f;
        }

        private RectTransform CreateRow()
        {
            RectTransform content = (RectTransform)transform.Find("Content");
            RectTransform row = CreateRect("Cards", content);

            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 640f;
            rowLayout.flexibleHeight = 1f;
            return row;
        }

        private LevelUpCardView CreateCard(RectTransform row, int index)
        {
            RectTransform cardRect = CreateRect($"Card {index + 1}", row);
            Image background = cardRect.gameObject.AddComponent<Image>();
            background.sprite = _blankSprite;
            background.color = new Color(0.15f, 0.17f, 0.22f, 1f);
            background.raycastTarget = true;

            Button button = cardRect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            ColorBlock colors = button.colors;
            colors.normalColor      = Color.white;
            colors.highlightedColor = new Color(0.82f, 0.88f, 1f, 1f);
            colors.pressedColor     = new Color(0.68f, 0.74f, 0.84f, 1f);
            colors.disabledColor    = new Color(0.55f, 0.55f, 0.55f, 1f);
            colors.fadeDuration     = 0.08f;
            button.colors = colors;

            VerticalLayoutGroup layout = cardRect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 24, 20);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Image icon = CreateRect("Icon", cardRect).gameObject.AddComponent<Image>();
            icon.sprite = _blankSprite;
            icon.raycastTarget = false;
            icon.gameObject.AddComponent<LayoutElement>().preferredHeight = 150f;

            TextMeshProUGUI name = CreateText("Name", cardRect, string.Empty, 32, FontStyles.Bold, Color.white);
            name.gameObject.AddComponent<LayoutElement>().preferredHeight = 78f;

            TextMeshProUGUI level = CreateText("Level", cardRect, string.Empty, 24, FontStyles.Normal, new Color(1f, 0.84f, 0.4f, 1f));
            level.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;

            TextMeshProUGUI description = CreateText(
                "Description",
                cardRect,
                string.Empty,
                24,
                FontStyles.Normal,
                new Color(0.84f, 0.87f, 0.92f, 1f));
            LayoutElement descriptionLayout = description.gameObject.AddComponent<LayoutElement>();
            descriptionLayout.preferredHeight = 180f;
            descriptionLayout.flexibleHeight = 1f;

            LevelUpCardView view = cardRect.gameObject.AddComponent<LevelUpCardView>();
            view.Construct(icon, name, level, description, button, _blankSprite);
            return view;
        }

        private static TextMeshProUGUI CreateText(
            string objectName,
            Transform parent,
            string value,
            float fontSize,
            FontStyles style,
            Color color)
        {
            RectTransform rect = CreateRect(objectName, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.richText = false;

            if (TMP_Settings.defaultFontAsset != null)
                text.font = TMP_Settings.defaultFontAsset;

            return text;
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            var rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot     = new Vector2(0.5f, 0.5f);
        }

        private static Sprite CreateBlankSprite()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}

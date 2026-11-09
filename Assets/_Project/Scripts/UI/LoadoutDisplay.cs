using UnityEngine;
using UnityEngine.UI;
using BulletHeaven.Loadout;
using BulletHeaven.Progression;
using BulletHeaven.Weapons;

namespace BulletHeaven.UI
{
    /// <summary>
    /// Draws <see cref="PlayerLoadout"/> into the HUD. Empty slots hide the icon and keep the frame.
    /// Lives on LoadoutPanel. Icon rows are filled from ActiveItems and PasiveItems when left empty.
    /// </summary>
    public class LoadoutDisplay : MonoBehaviour
    {
        private const string WeaponRowName  = "ActiveItems";
        private const string PassiveRowName = "PasiveItems";
        private const string IconChildName  = "ItemImage";

        [Tooltip("ItemImage under each weapon slot, left to right. Collected from ActiveItems when empty.")]
        [SerializeField] private Image[] weaponIcons;

        [Tooltip("ItemImage under each passive slot, left to right. Collected from PasiveItems when empty.")]
        [SerializeField] private Image[] passiveIcons;

        private PlayerLoadout _loadout;

        private void Reset()
        {
            weaponIcons  = CollectIcons(WeaponRowName);
            passiveIcons = CollectIcons(PassiveRowName);
        }

        private void Awake()
        {
            if (weaponIcons == null || weaponIcons.Length == 0)
                weaponIcons = CollectIcons(WeaponRowName);

            if (passiveIcons == null || passiveIcons.Length == 0)
                passiveIcons = CollectIcons(PassiveRowName);

            ClearRow(weaponIcons);
            ClearRow(passiveIcons);
        }

        private void Start()
        {
            _loadout = FindFirstObjectByType<PlayerLoadout>();

            if (_loadout == null)
            {
                Debug.LogWarning("[LoadoutDisplay] PlayerLoadout not found.", this);
                return;
            }

            _loadout.OnLoadoutChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_loadout != null)
                _loadout.OnLoadoutChanged -= Refresh;
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        private void Refresh()
        {
            DrawWeapons();
            DrawPassives();
        }

        private void DrawWeapons()
        {
            if (weaponIcons == null) return;

            int count = _loadout != null ? _loadout.WeaponCount : 0;

            for (int i = 0; i < weaponIcons.Length; i++)
            {
                Image icon = weaponIcons[i];
                if (icon == null) continue;

                if (_loadout == null || i >= count)
                {
                    Clear(icon);
                    continue;
                }

                WeaponDefinitionSO weapon = _loadout.GetWeapon(i);
                Show(icon, weapon != null ? weapon.icon : null);
            }
        }

        private void DrawPassives()
        {
            if (passiveIcons == null) return;

            int count = _loadout != null ? _loadout.PassiveCount : 0;

            for (int i = 0; i < passiveIcons.Length; i++)
            {
                Image icon = passiveIcons[i];
                if (icon == null) continue;

                if (_loadout == null || i >= count)
                {
                    Clear(icon);
                    continue;
                }

                PassiveDefinitionSO passive = _loadout.GetPassive(i);
                Show(icon, passive != null ? passive.icon : null);
            }
        }

        private static void ClearRow(Image[] icons)
        {
            if (icons == null) return;

            for (int i = 0; i < icons.Length; i++)
            {
                if (icons[i] != null)
                    Clear(icons[i]);
            }
        }

        private static void Show(Image icon, Sprite sprite)
        {
            icon.sprite  = sprite;
            icon.enabled = sprite != null;
        }

        private static void Clear(Image icon)
        {
            icon.sprite  = null;
            icon.enabled = false;
        }

        // ── Hierarchy ─────────────────────────────────────────────────────────

        private Image[] CollectIcons(string rowName)
        {
            Transform row = transform.Find(rowName);
            if (row == null)
            {
                Debug.LogWarning($"[LoadoutDisplay] '{rowName}' was not found under {name}.", this);
                return new Image[0];
            }

            int count = row.childCount;
            var icons = new Image[count];

            for (int i = 0; i < count; i++)
            {
                Transform slot = row.GetChild(i);
                Transform iconTransform = slot.Find(IconChildName);
                Image icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;

                if (icon == null)
                    Debug.LogWarning($"[LoadoutDisplay] '{slot.name}' has no '{IconChildName}'.", slot);

                icons[i] = icon;
            }

            return icons;
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.Serialization;
using BulletHeaven.Progression;

namespace BulletHeaven.Weapons
{
    [Serializable]
    public class WeaponLevelData
    {
        [TextArea]
        [Tooltip("Shown on the upgrade card as the next-rank line. The weapon description stays on its own line.")]
        public string upgradeDescription;

        // Shared set; each weapon reads these its own way
        // (aura and orbit: spread = radius, orbit: speed = degrees per second).

        [Min(0f)]    public float damage   = 10f;
        [Tooltip("Seconds between activations.")]
        [Min(0.01f)] public float cooldown = 1f;
        [Tooltip("Objects spawned per activation (projectiles, strikes, orbiters...).")]
        [Min(1)]     public int   amount   = 1;
        [Tooltip("Size of the effect. Meaning depends on the weapon (radius, scale...).")]
        [Min(0f)]    public float spread   = 1f;
        [Tooltip("Travel or rotation speed. Meaning depends on the weapon.")]
        [FormerlySerializedAs("projectileSpeed")]
        [Min(0f)]    public float speed    = 20f;
        [Tooltip("Seconds the effect stays active. Unused by instant weapons.")]
        [Min(0f)]    public float duration;
        [Tooltip("Extra enemies a single object can hit before it is spent.")]
        [Min(0)]     public int   pierce;
    }

    [CreateAssetMenu(menuName = "Weapons/WeaponDefinitionSO", fileName = "Weapon")]
    public class WeaponDefinitionSO : ScriptableObject
    {
        [Header("Display")]
        public string displayName;
        public Sprite icon;
        [TextArea] public string description;

        [Header("Runtime")]
        [Tooltip("Prefab with a WeaponBase. Spawned under the player the first time this weapon is taken.")]
        public WeaponBase weaponPrefab;

        [Header("Scaling")]
        [Tooltip("Passive modifiers apply only to the stats checked here.")]
        public WeaponStat scaledStats = WeaponStat.Damage | WeaponStat.Cooldown | WeaponStat.Amount;

        [Header("Levels")]
        [Tooltip("Index 0 is level 1.")]
        public WeaponLevelData[] levels = { new WeaponLevelData() };

        public int MaxLevel => levels != null ? levels.Length : 0;

        /// <summary>True when a passive of this stat should scale the weapon.</summary>
        public bool Uses(StatType type)
        {
            WeaponStat stat = ToWeaponStat(type);
            return stat != WeaponStat.None && (scaledStats & stat) != 0;
        }

        public WeaponLevelData GetLevel(int level)
        {
            if (levels == null || levels.Length == 0) return null;
            return levels[Mathf.Clamp(level - 1, 0, levels.Length - 1)];
        }

        /// <summary>
        /// Card line for the rank about to be gained. Empty when that rank has no text.
        /// </summary>
        public string GetUpgradeDescription(int level)
        {
            int index = level - 1;
            if (levels == null || index < 0 || index >= levels.Length)
                return string.Empty;

            WeaponLevelData data = levels[index];
            return data != null ? (data.upgradeDescription ?? string.Empty) : string.Empty;
        }

        private static WeaponStat ToWeaponStat(StatType type) => type switch
        {
            StatType.Damage          => WeaponStat.Damage,
            StatType.Cooldown        => WeaponStat.Cooldown,
            StatType.Amount          => WeaponStat.Amount,
            StatType.Spread          => WeaponStat.Spread,
            StatType.ProjectileSpeed => WeaponStat.ProjectileSpeed,
            StatType.Duration        => WeaponStat.Duration,
            _                        => WeaponStat.None
        };
    }
}

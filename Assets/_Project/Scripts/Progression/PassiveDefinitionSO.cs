using System;
using UnityEngine;

namespace BulletHeaven.Progression
{
    /// <summary>
    /// One passive rank. <see cref="modifiers"/> is the full set active at this rank,
    /// not a delta stacked on the previous rank.
    /// </summary>
    [Serializable]
    public class PassiveLevelData
    {
        [TextArea]
        [Tooltip("Shown on the upgrade card as the next-rank line. The passive description stays on its own line.")]
        public string upgradeDescription;

        public StatModifier[] modifiers;
    }

    /// <summary>
    /// Data-only passive. Index 0 is level 1, matching <see cref="BulletHeaven.Weapons.WeaponDefinitionSO"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Progression/PassiveDefinitionSO", fileName = "Passive")]
    public class PassiveDefinitionSO : ScriptableObject
    {
        [Header("Display")]
        public string displayName;
        public Sprite icon;
        [TextArea] public string description;

        [Header("Levels")]
        [Tooltip("Index 0 is level 1. Each rank replaces the previous rank's modifiers.")]
        public PassiveLevelData[] levels = { new PassiveLevelData() };

        public int MaxLevel => levels != null ? levels.Length : 0;

        public PassiveLevelData GetLevel(int level)
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

            PassiveLevelData data = levels[index];
            return data != null ? (data.upgradeDescription ?? string.Empty) : string.Empty;
        }
    }
}

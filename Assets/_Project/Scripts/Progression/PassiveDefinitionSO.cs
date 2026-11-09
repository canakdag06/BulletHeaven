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
    }
}

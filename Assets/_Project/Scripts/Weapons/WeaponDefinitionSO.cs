using System;
using UnityEngine;

namespace BulletHeaven.Weapons
{
    [Serializable]
    public class WeaponLevelData
    {
        [Min(0f)]    public float damage          = 10f;
        [Tooltip("Seconds between shots.")]
        [Min(0.01f)] public float cooldown        = 1f;
        [Min(0f)]    public float range           = 7f;
        [Min(0f)]    public float projectileSpeed = 20f;
        [Tooltip("Projectiles fired per shot.")]
        [Min(1)]     public int   amount          = 1;
        [Tooltip("Total fan angle used when more than one projectile is fired.")]
        [Min(0f)]    public float spreadAngle     = 15f;
    }

    [CreateAssetMenu(menuName = "Weapons/WeaponDefinitionSO", fileName = "Weapon")]
    public class WeaponDefinitionSO : ScriptableObject
    {
        [Header("Display")]
        public string displayName;
        public Sprite icon;
        [TextArea] public string description;

        [Header("Levels")]
        [Tooltip("Index 0 is level 1.")]
        public WeaponLevelData[] levels = { new WeaponLevelData() };

        public int MaxLevel => levels != null ? levels.Length : 0;

        public WeaponLevelData GetLevel(int level)
        {
            if (levels == null || levels.Length == 0) return null;
            return levels[Mathf.Clamp(level - 1, 0, levels.Length - 1)];
        }
    }
}

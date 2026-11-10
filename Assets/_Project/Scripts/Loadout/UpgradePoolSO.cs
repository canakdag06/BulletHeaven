using System;
using UnityEngine;
using BulletHeaven.Progression;
using BulletHeaven.Weapons;

namespace BulletHeaven.Loadout
{
    [Serializable]
    public class WeightedWeapon
    {
        public WeaponDefinitionSO item;
        [Min(0)] public int weight = 1;
    }

    [Serializable]
    public class WeightedPassive
    {
        public PassiveDefinitionSO item;
        [Min(0)] public int weight = 1;
    }

    /// <summary>
    /// Run pool for level-up cards. Weight lives on the entry so the same asset can
    /// be rarer in another pool without editing the weapon or passive itself.
    /// </summary>
    [CreateAssetMenu(menuName = "Loadout/UpgradePool", fileName = "UpgradePool")]
    public class UpgradePoolSO : ScriptableObject
    {
        public WeightedWeapon[] weapons;
        public WeightedPassive[] passives;

        [Header("Fallback")]
        [Range(0f, 1f)] public float fallbackHealFraction = 0.3f;
        public Sprite fallbackIcon;
        public string fallbackName = "Heal";
        [TextArea] public string fallbackDescription = "Restore 30% of max health.";
    }
}

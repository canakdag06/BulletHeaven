using UnityEngine;
using BulletHeaven.Progression;
using BulletHeaven.Weapons;

namespace BulletHeaven.Loadout
{
    /// <summary>
    /// One level-up card. A weapon, a passive, or a fallback heal — never more than one of those.
    /// </summary>
    public struct UpgradeOffer
    {
        public WeaponDefinitionSO Weapon;
        public PassiveDefinitionSO Passive;
        public int CurrentLevel;
        public int NextLevel;
        public string DisplayName;
        public Sprite Icon;
        public string Description;
        public bool IsFallback;
        public float HealFraction;
    }
}

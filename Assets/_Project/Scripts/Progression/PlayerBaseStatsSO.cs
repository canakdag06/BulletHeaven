using UnityEngine;

namespace BulletHeaven.Progression
{
    /// <summary>Starting stats of a playable character. Modifiers from <see cref="PlayerStats"/> are applied on top.</summary>
    [CreateAssetMenu(menuName = "Progression/PlayerBaseStatsSO", fileName = "PlayerBaseStats")]
    public class PlayerBaseStatsSO : ScriptableObject
    {
        [Header("Movement")]
        [Min(0f)] public float moveSpeed = 5f;

        [Header("Health")]
        [Min(1f)] public float maxHealth = 100f;
        [Min(0f)] public float armor;
        [Tooltip("Health restored per second.")]
        [Min(0f)] public float regen;

        [Header("Collection")]
        [Min(0.1f)] public float pickupRadius = 1f;

        [Header("Weapon Multipliers")]
        [Min(0f)] public float might           = 1f;
        [Min(0f)] public float cooldown        = 1f;
        [Min(0f)] public float area            = 1f;
        [Min(0f)] public float projectileSpeed = 1f;
        [Min(0f)] public float duration        = 1f;
        [Tooltip("Extra projectiles added to every weapon.")]
        [Min(0)]  public int   amount;

        public float GetBase(StatType type) => type switch
        {
            StatType.MoveSpeed       => moveSpeed,
            StatType.MaxHealth       => maxHealth,
            StatType.Armor           => armor,
            StatType.Regen           => regen,
            StatType.PickupRadius    => pickupRadius,
            StatType.Might           => might,
            StatType.Cooldown        => cooldown,
            StatType.Area            => area,
            StatType.Amount          => amount,
            StatType.ProjectileSpeed => projectileSpeed,
            StatType.Duration        => duration,
            _                        => 0f
        };
    }
}

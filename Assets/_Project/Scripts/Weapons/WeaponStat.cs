using System;

namespace BulletHeaven.Weapons
{
    /// <summary>
    /// Stats a weapon actually scales with. A passive changes a weapon only when its bit is set.
    /// </summary>
    [Flags]
    public enum WeaponStat
    {
        None            = 0,
        Damage          = 1 << 0,
        Cooldown        = 1 << 1,
        Amount          = 1 << 2,
        Spread          = 1 << 3,
        ProjectileSpeed = 1 << 4,
        Duration        = 1 << 5,
    }
}

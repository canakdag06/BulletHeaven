using System.Collections.Generic;
using UnityEngine;

namespace BulletHeaven.Enemy
{
    /// <summary>
    /// Single source of truth for enemies currently out of the pool.
    /// <see cref="Active"/> includes enemies playing their death animation; <see cref="AliveCount"/> does not.
    /// Enemies register in OnGet, are marked dead in Die and unregister in OnRelease.
    /// </summary>
    public static class EnemyRegistry
    {
        private static readonly List<Enemy> _active = new();

        public static IReadOnlyList<Enemy> Active => _active;
        public static int AliveCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _active.Clear();
            AliveCount = 0;
        }

        public static void Register(Enemy enemy)
        {
            if (enemy.RegistryIndex >= 0) return;

            enemy.RegistryIndex     = _active.Count;
            enemy.IsRegisteredAlive = true;
            _active.Add(enemy);
            AliveCount++;
        }

        public static void MarkDead(Enemy enemy)
        {
            if (enemy.RegistryIndex < 0 || !enemy.IsRegisteredAlive) return;

            enemy.IsRegisteredAlive = false;
            AliveCount--;
        }

        // Swap-remove: O(1), order of Active is not preserved.
        public static void Unregister(Enemy enemy)
        {
            int index = enemy.RegistryIndex;
            if (index < 0) return;

            MarkDead(enemy);

            int last = _active.Count - 1;
            if (index != last)
            {
                Enemy moved = _active[last];
                _active[index]      = moved;
                moved.RegistryIndex = index;
            }

            _active.RemoveAt(last);
            enemy.RegistryIndex = -1;
        }
    }
}

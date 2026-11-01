using BulletHeaven.Collectibles;
using UnityEngine;

namespace BulletHeaven.Enemy
{
    /// <summary>
    /// Weighted EXP tier rolled from the enemy's tier. Weights are integers out of 100.
    /// </summary>
    public static class ExpDropTable
    {
        private readonly struct Drop
        {
            public readonly ExpTier Tier;
            public readonly int Weight;

            public Drop(ExpTier tier, int weight)
            {
                Tier   = tier;
                Weight = weight;
            }
        }

        private static readonly Drop[] Tier1 = { new(ExpTier.LowEXP, 100) };
        private static readonly Drop[] Tier2 = { new(ExpTier.LowEXP, 75), new(ExpTier.MidEXP, 25) };
        private static readonly Drop[] Tier3 = { new(ExpTier.MidEXP, 90), new(ExpTier.HighEXP, 10) };

        public static ExpTier Roll(int enemyTier)
        {
            Drop[] drops = enemyTier switch
            {
                1 => Tier1,
                2 => Tier2,
                3 => Tier3,
                _ => Tier1,
            };

            int total = 0;
            for (int i = 0; i < drops.Length; i++)
                total += drops[i].Weight;

            int roll       = Random.Range(0, total);
            int cumulative = 0;

            for (int i = 0; i < drops.Length; i++)
            {
                cumulative += drops[i].Weight;
                if (roll < cumulative)
                    return drops[i].Tier;
            }

            return drops[drops.Length - 1].Tier;
        }
    }
}

using System.Collections.Generic;

namespace BulletHeaven.Collectibles
{
    /// <summary>
    /// EXP gems currently out of the pool. Magnet reads this list.
    /// Health and magnet pickups are never registered, so a magnet only pulls gems.
    /// </summary>
    public static class ExpGemRegistry
    {
        private static readonly List<ExpGem> Active = new();

        public static void Register(ExpGem gem)
        {
            if (gem != null && !Active.Contains(gem))
                Active.Add(gem);
        }

        public static void Unregister(ExpGem gem)
        {
            if (gem != null)
                Active.Remove(gem);
        }

        public static void AttractAll(PlayerCollector collector)
        {
            if (collector == null) return;

            for (int i = 0; i < Active.Count; i++)
            {
                ExpGem gem = Active[i];
                if (gem != null && gem.CanBeCollected)
                    gem.BeginCollect(collector);
            }
        }
    }
}

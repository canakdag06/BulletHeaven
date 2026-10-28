namespace BulletHeaven.Collectibles
{
    /// <summary>
    /// Anything the player can pick up. The collector only knows this contract,
    /// so new pickup types (health, magnet, chest…) need no changes on the player side.
    /// </summary>
    public interface ICollectible
    {
        /// <summary>False while the item is already being pulled or has been returned to the pool.</summary>
        bool CanBeCollected { get; }

        /// <summary>Called once when the item enters the collector's pickup radius.</summary>
        void BeginCollect(PlayerCollector collector);
    }
}

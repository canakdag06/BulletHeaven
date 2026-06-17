namespace BulletHeaven.Core.Save
{
    public interface ISaveService
    {
        void Save(GameSaveData data);
        GameSaveData Load();
    }
}

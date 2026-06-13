using UnityEngine;

[CreateAssetMenu(menuName = "Pooling/PoolConfigSO")]
public class PoolConfigSO : ScriptableObject
{
    public PoolData[] poolObjects;

#if UNITY_EDITOR
    private void OnValidate()
    {
        foreach (var pool in poolObjects)
        {
            pool.name = pool.type.ToString();
        }
    }

#endif
}

[System.Serializable]
public class PoolData
{
    [HideInInspector] public string name;
    public EPoolType type;
    public PoolableBehaviour prefab;
    public int initialSize = 10;
}

public interface IPoolable
{
    void OnGet();
    void OnRelease();
}


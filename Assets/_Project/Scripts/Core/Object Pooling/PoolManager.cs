using System.Collections.Generic;
using UnityEngine;

public class PoolManager : SingletonLocal<PoolManager>
{
    [SerializeField] private PoolConfigSO poolConfigSO;
    private Dictionary<EPoolType, Queue<PoolableBehaviour>> poolDict = new();
    private Dictionary<EPoolType, PoolData> poolDataDict = new();
    private Dictionary<EPoolType, Transform> containerDict = new();

    protected override void Awake()
    {
        base.Awake();
        foreach (var poolData in poolConfigSO.poolObjects)
        {
            if (poolData.prefab == null)
            {
                Debug.LogError($"Prefab null: {poolData.type}");
                continue;
            }

            if (!poolDict.ContainsKey(poolData.type))
                poolDict[poolData.type] = new Queue<PoolableBehaviour>();

            if (!poolDataDict.ContainsKey(poolData.type))
                poolDataDict[poolData.type] = poolData;

            Transform container = GetContainer(poolData.type);
            Queue<PoolableBehaviour> queue = poolDict[poolData.type];

            for (int i = 0; i < poolData.initialSize; i++)
            {
                PoolableBehaviour obj = Instantiate(poolData.prefab, container);
                obj.gameObject.SetActive(false);
                queue.Enqueue(obj);
            }
        }
    }

    public PoolableBehaviour Get(EPoolType type)
    {
        if (poolDict.TryGetValue(type, out var queue))
        {
            PoolableBehaviour obj = queue.Count > 0
                ? queue.Dequeue()
                : SpawnNew(type);

            if (obj == null) return null;

            obj.gameObject.SetActive(true);
            obj.OnGet();
            return obj;
        }

        Debug.LogError($"Pool not found: {type}");
        return null;
    }

    public void Release(PoolableBehaviour obj)
    {
        if (obj.InPool) return;
        obj.OnRelease();
        obj.gameObject.SetActive(false);
        obj.transform.SetParent(GetContainer(obj.PoolType));

        if (poolDict.TryGetValue(obj.PoolType, out var queue))
        {
            queue.Enqueue(obj);
        }
        else
        {
            Debug.LogError($"Pool not found: {obj.PoolType}");
        }
    }

    private PoolableBehaviour SpawnNew(EPoolType type)
    {
        if (poolDataDict.TryGetValue(type, out var data))
        {
            return Instantiate(data.prefab, GetContainer(type));
        }

        Debug.LogError($"Pool data not found for auto-expand: {type}");
        return null;
    }

    private Transform GetContainer(EPoolType type)
    {
        if (!containerDict.TryGetValue(type, out Transform container))
        {
            GameObject go = new GameObject(type.ToString());
            go.transform.SetParent(transform);
            container = go.transform;
            containerDict[type] = container;
        }
        return container;
    }


}

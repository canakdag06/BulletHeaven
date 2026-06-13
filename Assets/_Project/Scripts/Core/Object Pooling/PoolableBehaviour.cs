using UnityEngine;

public abstract class PoolableBehaviour : MonoBehaviour, IPoolable
{
    public bool InPool { get; private set; }
    public abstract EPoolType PoolType { get; }

    protected virtual void Awake() { }

    public virtual void OnGet()
    {
        InPool = false;
    }

    public virtual void OnRelease()
    {
        InPool = true;
    }

    public void Release()
    {
        PoolManager.Instance.Release(this);
    }
}

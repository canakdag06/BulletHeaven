using UnityEngine;

public abstract class PoolableBehaviour : MonoBehaviour, IPoolable
{
    public bool inPool = false;
    public abstract EPoolType PoolType { get; }
    protected virtual void Awake()
    {
    }
    public virtual void OnGet()
    {
        inPool = false;
        gameObject.SetActive(true);
    }

    public virtual void OnRelease()
    {
        inPool = true;
        gameObject.SetActive(false);
    }

}

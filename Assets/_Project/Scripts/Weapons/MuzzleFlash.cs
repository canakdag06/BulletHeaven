using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class MuzzleFlash : PoolableBehaviour
{
    public override EPoolType PoolType => EPoolType.MuzzleFlash;

    private ParticleSystem _ps;

    protected override void Awake()
    {
        base.Awake();
        _ps = GetComponent<ParticleSystem>();
    }

    public void Play(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        _ps.Play();
    }

    public override void OnRelease()
    {
        base.OnRelease();
        _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnParticleSystemStopped()
    {
        if (!InPool)
            Release();
    }
}

using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class HitEffect : PoolableBehaviour
{
    public override EPoolType PoolType => EPoolType.BloodDirectional;

    private ParticleSystem _ps;

    protected override void Awake()
    {
        base.Awake();
        _ps = GetComponent<ParticleSystem>();
    }

    /// <summary>
    /// Positions and orients the effect, then plays it.
    /// Pass the direction the blood should spray toward.
    /// </summary>
    public void Play(Vector3 position, Vector3 direction)
    {
        transform.SetPositionAndRotation(
            position,
            direction != Vector3.zero ? Quaternion.LookRotation(direction) : Quaternion.identity);

        _ps.Play();
    }

    public override void OnRelease()
    {
        base.OnRelease();
        _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    // Called by Unity when the ParticleSystem naturally finishes
    private void OnParticleSystemStopped()
    {
        if (!InPool)
            Release();
    }
}

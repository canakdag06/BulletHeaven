using System.Collections;
using UnityEngine;

/// <summary>
/// Pooled lightning strike. The bolt and the ground burst are one particle effect.
/// Played at the impact point, then returned to the pool once it finishes.
/// </summary>
public class ThunderboltStrike : PoolableBehaviour
{
    public override EPoolType PoolType => EPoolType.Thunderbolt;

    private const float MaxLifetime = 2f;

    private ParticleSystem[] _systems;
    private Coroutine        _releaseRoutine;

    protected override void Awake()
    {
        base.Awake();
        _systems = GetComponentsInChildren<ParticleSystem>(true);
    }

    /// <summary>Places the effect on the impact point and plays it.</summary>
    public void Play(Vector3 position)
    {
        transform.position = position;

        float lifetime = RestartEffects();

        if (_releaseRoutine != null)
            StopCoroutine(_releaseRoutine);

        _releaseRoutine = StartCoroutine(ReleaseAfter(lifetime));
    }

    public override void OnGet()
    {
        base.OnGet();
        StopEffects();
    }

    public override void OnRelease()
    {
        if (_releaseRoutine != null)
        {
            StopCoroutine(_releaseRoutine);
            _releaseRoutine = null;
        }

        StopEffects();
        base.OnRelease();
    }

    private float RestartEffects()
    {
        float lifetime = 0.2f;
        if (_systems == null) return lifetime;

        for (int i = 0; i < _systems.Length; i++)
        {
            ParticleSystem ps = _systems[i];
            if (ps == null) continue;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(false);

            ParticleSystem.MainModule main = ps.main;
            float systemLife = main.duration + main.startDelay.constantMax + main.startLifetime.constantMax;
            if (systemLife > lifetime)
                lifetime = systemLife;
        }

        return Mathf.Min(lifetime, MaxLifetime);
    }

    private void StopEffects()
    {
        if (_systems == null) return;

        for (int i = 0; i < _systems.Length; i++)
        {
            if (_systems[i] == null) continue;
            _systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private IEnumerator ReleaseAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        _releaseRoutine = null;

        if (!InPool)
            Release();
    }
}

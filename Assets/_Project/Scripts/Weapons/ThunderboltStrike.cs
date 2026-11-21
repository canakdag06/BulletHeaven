using System.Collections;
using UnityEngine;

/// <summary>
/// Pooled lightning strike. The whole effect scales with the strike radius.
/// Played at the impact point, then returned to the pool once it finishes.
/// </summary>
public class ThunderboltStrike : PoolableBehaviour
{
    public override EPoolType PoolType => EPoolType.Thunderbolt;

    [Tooltip("Root of the effect. Its scale on the prefab is the size at rank 1.")]
    [SerializeField] private Transform visual;

    private const float MaxLifetime = 2f;

    private ParticleSystem[] _systems;
    private Coroutine        _releaseRoutine;
    private Vector3          _baseScale = Vector3.one;

    protected override void Awake()
    {
        base.Awake();
        _systems = GetComponentsInChildren<ParticleSystem>(true);

        if (visual != null)
            _baseScale = visual.localScale;
    }

    /// <summary>
    /// Places the effect on the impact point and plays it.
    /// <paramref name="spreadScale"/> is 1 at rank 1 and grows with Spread.
    /// </summary>
    public void Play(Vector3 position, float spreadScale)
    {
        transform.position = position;
        ApplyScale(spreadScale);

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

    private void ApplyScale(float spreadScale)
    {
        if (visual == null) return;

        visual.localScale = _baseScale * spreadScale;
    }

    private float RestartEffects()
    {
        float lifetime = 0.2f;
        if (_systems == null) return lifetime;

        for (int i = 0; i < _systems.Length; i++)
        {
            ParticleSystem ps = _systems[i];
            if (ps == null) continue;

            ParticleSystem.MainModule main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(false);
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

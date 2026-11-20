using System.Collections;
using UnityEngine;

/// <summary>
/// Pooled bolt and impact burst. Child particle systems are replayed at the strike point,
/// then the object returns to the pool once they have finished.
/// </summary>
public class LightningStrike : PoolableBehaviour
{
    public override EPoolType PoolType => EPoolType.Lightning;

    [SerializeField] private LineRenderer bolt;

    [Tooltip("Scaled uniformly so the burst matches the strike radius. Diameter of the effect at scale 1.")]
    [SerializeField] private Transform splashVisual;
    [SerializeField, Min(0.01f)] private float splashBaseDiameter = 2f;

    private const float MaxLifetime = 2f;

    private ParticleSystem[] _systems;
    private Coroutine        _releaseRoutine;

    protected override void Awake()
    {
        base.Awake();
        _systems = GetComponentsInChildren<ParticleSystem>(true);
    }

    /// <summary>
    /// Draws a bolt from origin to target and plays the impact burst at target.
    /// </summary>
    public void Play(Vector3 origin, Vector3 target, float radius)
    {
        transform.position = target;

        if (bolt != null)
        {
            bolt.enabled = true;
            bolt.positionCount = 2;
            bolt.SetPosition(0, origin);
            bolt.SetPosition(1, target);
        }

        if (splashVisual != null)
        {
            splashVisual.localPosition = Vector3.zero;
            float scale = radius * 2f / splashBaseDiameter;
            splashVisual.localScale = new Vector3(scale, scale, scale);
        }

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

        if (bolt != null)
            bolt.enabled = false;

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

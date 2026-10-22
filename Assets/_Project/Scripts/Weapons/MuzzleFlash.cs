using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class MuzzleFlash : MonoBehaviour
{
    private ParticleSystem _ps;

    private void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
    }

    public void Play()
    {
        // Restart so rapid shots retrigger the effect instead of being ignored while it is still playing.
        _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _ps.Play();
    }
}

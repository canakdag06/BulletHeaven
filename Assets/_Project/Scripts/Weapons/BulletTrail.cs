using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class BulletTrail : PoolableBehaviour
{
    [SerializeField] private float duration = 0.05f;
    private LineRenderer _lineRenderer;

    public override EPoolType PoolType => EPoolType.BulletTrail;

    protected override void Awake()
    {
        base.Awake();
        _lineRenderer = GetComponent<LineRenderer>();
    }

    public void SetUpTrail(Vector3 startPoint, Vector3 endPoint)
    {
        _lineRenderer.SetPosition(0, startPoint);
        _lineRenderer.SetPosition(1, endPoint);

        StartCoroutine(ReleaseRoutine());
    }

    private IEnumerator ReleaseRoutine()
    {
        yield return new WaitForSeconds(duration);
        Release();
    }
}
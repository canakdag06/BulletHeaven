using TMPro;
using UnityEngine;

public class DamageText : PoolableBehaviour
{
    public override EPoolType PoolType => EPoolType.DamageText;

    [SerializeField] private float lifetime = 0.6f;
    [SerializeField] private float riseSpeed = 1.6f;
    [SerializeField] private float horizontalJitter = 0.4f;

    private TextMeshPro _text;
    private float _age;
    private Vector3 _velocity;
    private Color _baseColor;

    protected override void Awake()
    {
        base.Awake();
        _text = GetComponent<TextMeshPro>();
    }

    public override void OnGet()
    {
        base.OnGet();
        _age = 0f;
    }

    public void Show(Vector3 worldPosition, float amount)
    {
        Camera cam = Camera.main;
        Vector3 right = cam != null ? cam.transform.right : Vector3.right;
        right.y = 0f;
        if (right.sqrMagnitude < 0.001f) right = Vector3.right;

        transform.position = worldPosition;
        _velocity = Vector3.up * riseSpeed
                    + right.normalized * Random.Range(-horizontalJitter, horizontalJitter);
        _age = 0f;

        _baseColor = _text.color;
        _baseColor.a = 1f;
        _text.color = _baseColor;
        _text.text = Mathf.Max(1, Mathf.RoundToInt(amount)).ToString();

        FaceCamera(cam);
    }

    private void Update()
    {
        if (InPool) return;

        _age += Time.deltaTime;
        transform.position += _velocity * Time.deltaTime;

        Camera cam = Camera.main;
        FaceCamera(cam);

        float t = Mathf.Clamp01(_age / lifetime);
        Color c = _baseColor;
        c.a = 1f - t;
        _text.color = c;

        if (_age >= lifetime)
            Release();
    }

    private void FaceCamera(Camera cam)
    {
        if (cam == null) return;
        transform.rotation = cam.transform.rotation;
    }
}

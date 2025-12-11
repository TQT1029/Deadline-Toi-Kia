using UnityEngine;

[RequireComponent(typeof(BackgroundManager))]
public class ParallaxBackground : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Kéo Player hoặc Camera vào đây. Nếu để trống sẽ tự tìm MainCamera")]
    [SerializeField] private Transform targetSubject;

    [Header("Parallax Settings")]
    [SerializeField] private bool enableParallaxX = true;
    [SerializeField] private bool enableParallaxY = true;

    [Space(10)]
    [Tooltip("Tốc độ tự động cuộn (Auto Scroll) - Đặt 0 nếu muốn background chỉ chạy khi nhân vật chạy")]
    [SerializeField] private float baseScrollSpeedX = 0f; // Mặc định để 0 để test theo nhân vật

    [Tooltip("Hệ số nhân vận tốc nhân vật (Giá trị càng lớn, background trôi càng nhanh theo nhân vật)")]
    [SerializeField] private float velocityMultiplierX = 0.5f; // Thay thế số cứng 0.05f cũ

    [Tooltip("Độ mạnh của hiệu ứng Parallax dọc (0 = không trôi, 1 = trôi theo nhân vật)")]
    [Range(0f, 1f)]
    [SerializeField] private float verticalParallaxStrength = 0.5f;

    [Tooltip("Làm mượt chuyển động Y")]
    [SerializeField] private float smoothingY = 10f;

    private BackgroundManager _bgManager;
    private DynamicLayer[] _dynamicLayers;
    private Rigidbody2D _targetRb;

    private void Awake()
    {
        _bgManager = GetComponent<BackgroundManager>();

        if (targetSubject == null && Camera.main != null)
        {
            targetSubject = Camera.main.transform;
        }

        if (targetSubject != null)
        {
            _targetRb = targetSubject.GetComponent<Rigidbody2D>();
        }
    }

    private void Start()
    {
        SetupLayers();
    }

    private void SetupLayers()
    {
        _bgManager.FetchLayers();
        var layersTransforms = _bgManager.Layers;
        int count = layersTransforms.Length;

        _dynamicLayers = new DynamicLayer[count];

        for (int i = 0; i < count; i++)
        {
            if (!layersTransforms[i].TryGetComponent(out DynamicLayer layerScript))
            {
                layerScript = layersTransforms[i].gameObject.AddComponent<DynamicLayer>();
            }

            float ratio = (count <= 1) ? 0f : (float)i / (count - 1);

            // Layer xa chạy chậm (gần 0), layer gần chạy nhanh (gần 1)
            float speedFactorX = Mathf.Lerp(0.05f, 1.0f, ratio);

            // Layer xa ít bị trôi Y, gần bị trôi nhiều
            float parallaxFactorY = Mathf.Lerp(0.05f, verticalParallaxStrength, ratio);

            layerScript.Initialize(this, speedFactorX, parallaxFactorY);
            _dynamicLayers[i] = layerScript;
        }
    }

    public Vector2 GetTargetVelocity()
    {
        if (_targetRb != null)
        {
#if UNITY_6000_0_OR_NEWER
            return _targetRb.linearVelocity;
#else
            return _targetRb.velocity;
#endif
        }
        return Vector2.zero;
    }

    public bool IsXEnabled => enableParallaxX;
    public bool IsYEnabled => enableParallaxY;
    public float BaseSpeedX => baseScrollSpeedX;

    // Property mới để DynamicLayer truy cập
    public float VelocityMultiplierX => velocityMultiplierX;

    public float SmoothingY => smoothingY;

    public float GetTargetPositionY()
    {
        return targetSubject != null ? targetSubject.position.y : 0f;
    }
}
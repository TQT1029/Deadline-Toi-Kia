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
    [SerializeField] private float baseScrollSpeedX = 0f;

    [Tooltip("Hệ số nhân vận tốc nhân vật (Giá trị càng lớn, background trôi càng nhanh theo nhân vật)")]
    [SerializeField] private float velocityMultiplierX = 0.5f;

    [Header("Smoothing & Inertia")] // [MỚI] Phần cài đặt độ mượt
    [Tooltip("Thời gian để background giảm tốc khi nhân vật dừng lại. 0 = dừng ngay, 0.5 = trôi 1 đoạn rồi dừng.")]
    [SerializeField] private float smoothTimeX = 0.25f;

    [Tooltip("Độ mạnh của hiệu ứng Parallax dọc (0 = không trôi, 1 = trôi theo nhân vật)")]
    [Range(0f, 1f)]
    [SerializeField] private float verticalParallaxStrength = 0.5f;

    [Tooltip("Làm mượt chuyển động Y")]
    [SerializeField] private float smoothingY = 10f;

    private BackgroundManager _bgManager;
    private DynamicLayer[] _dynamicLayers;
    private Rigidbody2D _targetRb;

    // [MỚI] Các biến phục vụ tính toán SmoothDamp
    private float _smoothedVelocityX;
    private float _velocityRefX; // Biến tham chiếu cho SmoothDamp

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

    private void Update()
    {
        // [MỚI] Tính toán vận tốc mượt ở mỗi frame trong Update
        CalculateSmoothedVelocity();
    }

    private void CalculateSmoothedVelocity()
    {
        float targetVelocityX = 0f;

        if (_targetRb != null)
        {
#if UNITY_6000_0_OR_NEWER
            targetVelocityX = _targetRb.linearVelocity.x;
#else
            targetVelocityX = _targetRb.velocity.x;
#endif
        }

        // Dùng SmoothDamp để chuyển từ vận tốc hiện tại sang vận tốc mục tiêu một cách mượt mà
        // Điều này tạo ra hiệu ứng quán tính: khi targetVelocityX về 0, _smoothedVelocityX sẽ giảm từ từ.
        _smoothedVelocityX = Mathf.SmoothDamp(_smoothedVelocityX, targetVelocityX, ref _velocityRefX, smoothTimeX);
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
            float speedFactorX = Mathf.Lerp(0.05f, 1.0f, ratio);
            float parallaxFactorY = Mathf.Lerp(0.05f, verticalParallaxStrength, ratio);

            layerScript.Initialize(this, speedFactorX, parallaxFactorY);
            _dynamicLayers[i] = layerScript;
        }
    }

    // [MỚI] Hàm public để DynamicLayer lấy vận tốc đã làm mượt
    public float GetSmoothedVelocityX()
    {
        return _smoothedVelocityX;
    }

    public bool IsXEnabled => enableParallaxX;
    public bool IsYEnabled => enableParallaxY;
    public float BaseSpeedX => baseScrollSpeedX;
    public float VelocityMultiplierX => velocityMultiplierX;
    public float SmoothingY => smoothingY;

    public float GetTargetPositionY()
    {
        return targetSubject != null ? targetSubject.position.y : 0f;
    }
}
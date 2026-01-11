using UnityEngine;

[RequireComponent(typeof(BackgroundManager))]
public class ParallaxBackgroundController : MonoBehaviour
{
    [Header("Target")]
    public Transform targetSubject; // Player
    [SerializeField] private Camera mainCamera;

    [Header("Settings")]
    [SerializeField] private bool enableParallaxX = true;
    [SerializeField] private bool enableParallaxY = true;

    [Tooltip("Tốc độ tự trôi (Auto Scroll)")]
    [SerializeField] private float baseScrollSpeedX = 0f;

    [Tooltip("Hệ số nhân với vận tốc Player. 1 = trôi cùng tốc độ, 0.5 = chậm hơn")]
    [SerializeField] private float velocityMultiplierX = 0.5f;

    [Header("Smoothing")]
    [Tooltip("Độ trễ quán tính (0 = dừng ngay, cao = trôi mượt)")]
    [SerializeField] private float smoothTimeX = 0.25f;
    [SerializeField] private float smoothingY = 10f;

    [Header("Depth Control")]
    [Range(0f, 1f)][SerializeField] private float verticalParallaxStrength = 0.5f;

    // Internal
    private BackgroundManager _bgManager;
    private DynamicLayer[] _dynamicLayers;
    private Rigidbody2D _targetRb;

    // Physics vars
    private float _smoothedVelocityX;
    private float _velocityRefX;

    // Getters
    public bool IsXEnabled => enableParallaxX;
    public bool IsYEnabled => enableParallaxY;
    public float BaseSpeedX => baseScrollSpeedX;
    public float VelocityMultiplierX => velocityMultiplierX;
    public float SmoothingY => smoothingY;
    public float SmoothedVelocityX => _smoothedVelocityX;

    private void Awake()
    {
        _bgManager = GetComponent<BackgroundManager>();

        if (mainCamera == null) mainCamera = Camera.main;

        // Tự tìm player nếu chưa gán
        if (targetSubject == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) targetSubject = player.transform;
        }

        if (targetSubject != null)
            _targetRb = targetSubject.GetComponent<Rigidbody2D>();
    }

    private void Start() => SetupLayers();

    private void Update()
    {
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
        // Dùng SmoothDamp để tạo hiệu ứng quán tính
        _smoothedVelocityX = Mathf.SmoothDamp(_smoothedVelocityX, targetVelocityX, ref _velocityRefX, smoothTimeX);
    }

    public float GetCamPosY() => mainCamera != null ? mainCamera.transform.position.y : 0;

    private void SetupLayers()
    {
        _bgManager.FetchLayers();
        var layersTransforms = _bgManager.Layers;
        int count = layersTransforms.Count;

        _dynamicLayers = new DynamicLayer[count];

        for (int i = 0; i < count; i++)
        {
            Transform layerTr = layersTransforms[i];
            if (!layerTr.TryGetComponent(out DynamicLayer layerScript))
            {
                layerScript = layerTr.gameObject.AddComponent<DynamicLayer>();
            }

            // --- Tính toán Parallax Factor dựa trên Z ---
            float zPos = layerTr.localPosition.z;
            float speedFactorX = 0f;
            float parallaxFactorY = 0f;

            if (zPos > 0)
            {
                // BACKGROUND: Z càng lớn (xa) -> factor càng nhỏ (trôi chậm)
                speedFactorX = 1f / (zPos * 0.1f + 1f);
                parallaxFactorY = speedFactorX * verticalParallaxStrength;
            }
            else
            {
                // FOREGROUND: Z càng nhỏ (âm) -> factor càng lớn (trôi nhanh hơn Player)
                speedFactorX = 1f + Mathf.Abs(zPos) * 0.5f;
                parallaxFactorY = speedFactorX * verticalParallaxStrength;
            }

            layerScript.Initialize(this, speedFactorX, parallaxFactorY);
            _dynamicLayers[i] = layerScript;
        }
    }
}
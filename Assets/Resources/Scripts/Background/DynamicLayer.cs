using UnityEngine;

public class DynamicLayer : MonoBehaviour
{
    private ParallaxBackground _controller;
    private Renderer _renderer;
    private Material _material;

    private float _speedFactorX;
    private float _parallaxFactorY;

    private Vector2 _currentTextureOffset;
    private float _initialLocalY;

    // Biến kiểm tra xem material có hỗ trợ offset không
    private bool _hasMainTexture;

    public void Initialize(ParallaxBackground controller, float speedX, float parallaxY)
    {
        _controller = controller;
        _speedFactorX = speedX;
        _parallaxFactorY = parallaxY;

        _renderer = GetComponent<Renderer>();

        // Clone material để chỉnh offset riêng
        _material = _renderer.material;

        _initialLocalY = transform.localPosition.y;

        // KIỂM TRA AN TOÀN:
        // Kiểm tra xem Material có thuộc tính mainTexture không để tránh lỗi null
        _hasMainTexture = _material.HasProperty("_MainTex") || _material.HasProperty("_BaseMap");

        if (_hasMainTexture)
        {
            _currentTextureOffset = _material.mainTextureOffset;
        }
    }

    private void Update()
    {
        if (_controller == null) return;

        HandleHorizontalScroll();
        HandleVerticalParallax();
    }

    private void HandleHorizontalScroll()
    {
        // Chỉ chạy nếu tính năng bật và Material hợp lệ
        if (_controller.IsXEnabled && _hasMainTexture)
        {
            float velX = _controller.GetTargetVelocity().x;

            // Tính toán bước nhảy
            float moveStep = (_controller.BaseSpeedX + (velX * 0.05f)) * _speedFactorX * Time.deltaTime;

            _currentTextureOffset.x += moveStep;

            // SỬA LỖI: Dùng API chuẩn thay vì SetVector ID
            _material.mainTextureOffset = _currentTextureOffset;
        }
    }

    private void HandleVerticalParallax()
    {
        if (_controller.IsYEnabled)
        {
            // Logic Parallax theo vị trí Y (Không dùng offset texture mà dùng Position)
            float targetY = _controller.GetTargetPositionY();
            float newY = _initialLocalY - (targetY * _parallaxFactorY);

            Vector3 currentPos = transform.localPosition;
            float smoothY = Mathf.Lerp(currentPos.y, newY, Time.deltaTime * _controller.SmoothingY);

            transform.localPosition = new Vector3(currentPos.x, smoothY, currentPos.z);
        }
        else
        {
            // Reset về vị trí gốc nếu tắt tính năng
            if (Mathf.Abs(transform.localPosition.y - _initialLocalY) > 0.01f)
            {
                Vector3 currentPos = transform.localPosition;
                float smoothY = Mathf.Lerp(currentPos.y, _initialLocalY, Time.deltaTime * 5f);
                transform.localPosition = new Vector3(currentPos.x, smoothY, currentPos.z);
            }
        }
    }

    private void OnDestroy()
    {
        if (_material != null)
        {
            Destroy(_material);
        }
    }
}
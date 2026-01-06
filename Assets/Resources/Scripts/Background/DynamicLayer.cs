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

    private bool _hasMainTexture;

    public void Initialize(ParallaxBackground controller, float speedX, float parallaxY)
    {
        _controller = controller;
        _speedFactorX = speedX;
        _parallaxFactorY = parallaxY;

        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _material = _renderer.material;
            // Kiểm tra xem material có texture không để tránh lỗi
            _hasMainTexture = _material.HasProperty("_MainTex") || _material.HasProperty("_BaseMap");

            if (_hasMainTexture)
            {
                _currentTextureOffset = _material.mainTextureOffset;
            }
        }

        _initialLocalY = transform.localPosition.y;
    }

    private void Update()
    {
        if (_controller == null) return;

        HandleHorizontalScroll();
        HandleVerticalParallax();
    }

    private void HandleHorizontalScroll()
    {
        if (_controller.IsXEnabled && _hasMainTexture && _material != null)
        {
            // [THAY ĐỔI QUAN TRỌNG] 
            // Thay vì lấy vận tốc trực tiếp (giật cục), ta lấy vận tốc đã được làm mượt từ Controller
            float smoothedPlayerVelocityX = _controller.GetSmoothedVelocityX();

            // Tính toán tổng tốc độ: Base Speed + (Vận tốc mượt của nhân vật * Hệ số)
            float totalSpeedX = _controller.BaseSpeedX + (smoothedPlayerVelocityX * _controller.VelocityMultiplierX);

            // Nhân với hệ số riêng của layer (xa/gần)
            float moveStep = totalSpeedX * _speedFactorX * Time.deltaTime;

            // Cộng dồn offset
            _currentTextureOffset.x += moveStep;

            _material.mainTextureOffset = _currentTextureOffset;
        }
    }

    private void HandleVerticalParallax()
    {
        if (_controller.IsYEnabled)
        {
            float targetY = _controller.GetTargetPositionY();
            float newY = _initialLocalY - (targetY * _parallaxFactorY);

            Vector3 currentPos = transform.localPosition;
            float smoothY = Mathf.Lerp(currentPos.y, newY, Time.deltaTime * _controller.SmoothingY);

            transform.localPosition = new Vector3(currentPos.x, smoothY, currentPos.z);
        }
        else
        {
            // Logic tự động quay về vị trí Y gốc nếu tắt parallax Y
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
        // Dọn dẹp material instance để tránh memory leak
        if (_material != null)
        {
            Destroy(_material);
        }
    }
}
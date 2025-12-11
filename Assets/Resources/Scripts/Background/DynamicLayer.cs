using UnityEngine;

public class DynamicLayer : MonoBehaviour
{
    private ParallaxBackground _controller;
    private Renderer _renderer;
    private Material _material;

    private float _speedFactorX; // Hệ số tốc độ riêng của từng layer (xa chậm, gần nhanh)
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
        _material = _renderer.material;
        _initialLocalY = transform.localPosition.y;

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
        if (_controller.IsXEnabled && _hasMainTexture)
        {
            // 1. Lấy vận tốc hiện tại của nhân vật (có âm, có dương)
            float playerVelocityX = _controller.GetTargetVelocity().x;

            // 2. Tính toán tốc độ trôi dựa trên BaseSpeed + Vận tốc nhân vật
            // Công thức: (Tốc độ tự động + (Vận tốc nhân vật * Hệ số ảnh hưởng))
            float totalSpeedX = _controller.BaseSpeedX + (playerVelocityX * _controller.VelocityMultiplierX);

            // 3. Nhân với hệ số riêng của layer (để tạo chiều sâu xa gần)
            float moveStep = totalSpeedX * _speedFactorX * Time.deltaTime;

            // 4. Cộng dồn vào Offset
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
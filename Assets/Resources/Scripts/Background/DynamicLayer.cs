using UnityEngine;

public enum ParallaxMode
{
    UV_Scroll,          // Cuộn texture (Tốt cho mây, trời)
    Transform_Move,     // Di chuyển vật lý (Tốt cho vật thể cụ thể)
    Infinite_Reposition // Di chuyển + Tự reset vị trí khi ra khỏi màn hình
}

public class DynamicLayer : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private ParallaxMode mode = ParallaxMode.UV_Scroll;
    [Tooltip("Chiều rộng ảnh để tính loop (0 = tự tính)")]
    [SerializeField] private float spriteWidth = 0f;

    // Internal
    private ParallaxBackgroundController _controller;
    private Renderer _renderer;
    private Material _material;

    private float _speedFactorX;
    private float _parallaxFactorY;
    private Vector2 _currentTextureOffset;
    private float _initialLocalY;

    public void Initialize(ParallaxBackgroundController controller, float speedX, float parallaxY)
    {
        _controller = controller;
        _speedFactorX = speedX;
        _parallaxFactorY = parallaxY;

        _renderer = GetComponent<Renderer>();
        _initialLocalY = transform.localPosition.y;

        if (_renderer != null)
        {
            if (spriteWidth == 0) spriteWidth = _renderer.bounds.size.x;

            if (mode == ParallaxMode.UV_Scroll)
            {
                _material = _renderer.material;
                if (_material.HasProperty("_MainTex") || _material.HasProperty("_BaseMap"))
                    _currentTextureOffset = _material.mainTextureOffset;
            }
        }
    }

    private void Update()
    {
        if (_controller == null) return;
        HandleHorizontal();
        HandleVertical();
    }

    private void HandleHorizontal()
    {
        if (!_controller.IsXEnabled) return;

        // Tính quãng đường di chuyển frame này
        float moveDelta = (_controller.BaseSpeedX + (_controller.SmoothedVelocityX * _controller.VelocityMultiplierX)) * Time.deltaTime;

        // Áp dụng hệ số xa gần
        float finalMoveX = moveDelta * _speedFactorX;

        switch (mode)
        {
            case ParallaxMode.UV_Scroll:
                if (_material != null)
                {
                    _currentTextureOffset.x += finalMoveX * 0.1f; // 0.1f để scale tốc độ UV cho hợp lý
                    _material.mainTextureOffset = _currentTextureOffset;
                }
                break;

            case ParallaxMode.Transform_Move:
                transform.Translate(Vector3.left * finalMoveX);
                break;

            case ParallaxMode.Infinite_Reposition:
                transform.Translate(Vector3.left * finalMoveX);

                // Logic lặp vô tận: Nếu trôi quá xa bên trái cam -> Dịch sang phải
                if (Camera.main != null)
                {
                    float dist = transform.position.x - Camera.main.transform.position.x;
                    if (dist < -spriteWidth)
                    {
                        // Giả sử có 2 bản sao background nối đuôi nhau
                        transform.Translate(Vector3.right * (spriteWidth * 2f));
                    }
                }
                break;
        }
    }

    private void HandleVertical()
    {
        if (_controller.IsYEnabled)
        {
            float camY = _controller.GetCamPosY();
            float targetY = _initialLocalY - (camY * _parallaxFactorY);

            // Lerp để chuyển động Y mượt mà hơn
            Vector3 pos = transform.localPosition;
            pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * _controller.SmoothingY);
            transform.localPosition = pos;
        }
    }

    private void OnDestroy()
    {
        if (_material != null) Destroy(_material);
    }
}
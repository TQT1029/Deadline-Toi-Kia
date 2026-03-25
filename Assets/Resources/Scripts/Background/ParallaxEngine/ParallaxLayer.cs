using UnityEngine;

namespace ParallaxEngine
{

    [RequireComponent(typeof(Renderer))]
    public class ParallaxLayer : MonoBehaviour
    {


        [Tooltip("Chiều rộng ảnh để tính loop (0 = tự lấy từ bounds)")]
        [SerializeField] private float spriteWidth = 0f;

        [Tooltip("Chiều cao ảnh để tính loop (0 = tự lấy từ bounds)")]
        [SerializeField] private float spriteHeight = 0f;

        private ParallaxManager _manager;
        private Renderer _renderer;
        private Material _material;

        private float _speedFactorX;
        private float _speedFactorY;
        private Vector2 _currentTextureOffset;

        public void Initialize(ParallaxManager manager, float speedX, float speedY)
        {
            _manager = manager;
            _speedFactorX = speedX;
            _speedFactorY = speedY;

            _renderer = GetComponent<Renderer>();

            if (spriteWidth == 0) spriteWidth = _renderer.bounds.size.x;
            if (spriteHeight == 0) spriteHeight = _renderer.bounds.size.y;

            if (_manager.globalMode == ParallaxMode.UV_Scroll)
            {
                _material = _renderer.material;
                if (_material.HasProperty("_MainTex") || _material.HasProperty("_BaseMap"))
                    _currentTextureOffset = _material.mainTextureOffset;
            }
        }

        public void UpdateLayer(float baseMoveDeltaX, float baseMoveDeltaY)
        {
            if (_manager.enableParallaxX) HandleHorizontal(baseMoveDeltaX);
            if (_manager.enableParallaxY) HandleVertical(baseMoveDeltaY);
        }

        private void HandleHorizontal(float moveDeltaX)
        {
            float finalMoveX = moveDeltaX * _speedFactorX;

            switch (_manager.globalMode)
            {
                case ParallaxMode.UV_Scroll:
                    if (_material != null)
                    {
                        _currentTextureOffset.x += finalMoveX * 0.1f;
                        _material.mainTextureOffset = _currentTextureOffset;
                    }
                    break;

                case ParallaxMode.Transform_Move:
                    transform.Translate(Vector3.left * finalMoveX);
                    break;

                case ParallaxMode.Infinite_Reposition:
                    transform.Translate(Vector3.left * finalMoveX);

                    if (_manager.MainCam != null)
                    {
                        float dist = transform.position.x - _manager.MainCam.transform.position.x;
                        if (dist < -spriteWidth)
                            transform.Translate(Vector3.right * (spriteWidth * 2f));
                        else if (dist > spriteWidth)
                            transform.Translate(Vector3.left * (spriteWidth * 2f));
                    }
                    break;
            }
        }

        private void HandleVertical(float moveDeltaY)
        {
            float finalMoveY = moveDeltaY * _speedFactorY;

            switch (_manager.globalMode)
            {
                case ParallaxMode.UV_Scroll:
                    if (_material != null)
                    {
                        _currentTextureOffset.y += finalMoveY * 0.1f;
                        _material.mainTextureOffset = _currentTextureOffset;
                    }
                    break;

                case ParallaxMode.Transform_Move:
                    transform.Translate(Vector3.down * finalMoveY);
                    break;

                case ParallaxMode.Infinite_Reposition:
                    transform.Translate(Vector3.down * finalMoveY);

                    if (_manager.MainCam != null)
                    {
                        float distY = transform.position.y - _manager.MainCam.transform.position.y;
                        if (distY < -spriteHeight)
                            transform.Translate(Vector3.up * (spriteHeight * 2f));
                        else if (distY > spriteHeight)
                            transform.Translate(Vector3.down * (spriteHeight * 2f));
                    }
                    break;
            }
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
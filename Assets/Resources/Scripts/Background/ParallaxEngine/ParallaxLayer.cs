using UnityEngine;

namespace ParallaxEngine
{
    public enum RepositionSymmetry
    {
        [Tooltip("Đối xứng qua trục dọc (Y) của Camera. Dùng chủ yếu cho cuộn nền ngang.")]
        VerticalAxis,
        [Tooltip("Đối xứng qua trục ngang (X) của Camera. Dùng chủ yếu cho cuộn nền dọc.")]
        HorizontalAxis,
        [Tooltip("Đối xứng tâm (Lật qua cả X và Y cùng lúc).")]
        Point
    }

    [RequireComponent(typeof(Renderer))]
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("Layer Mode Override")]
        [Tooltip("Bật tùy chọn này để layer không sử dụng Global Mode của Manager.")]
        [SerializeField] private bool overrideGlobalMode = false;
        [Tooltip("Chế độ Parallax riêng cho layer này.")]
        [SerializeField] private ParallaxMode localMode = ParallaxMode.UV_Scroll;

        [Header("Size Settings")]
        [Tooltip("Chiều rộng ảnh để tính loop (0 = tự lấy từ bounds)")]
        [SerializeField] private float spriteWidth = 0f;
        [Tooltip("Chiều cao ảnh để tính loop (0 = tự lấy từ bounds)")]
        [SerializeField] private float spriteHeight = 0f;

        [Header("Advanced Reposition Logic")]
        [Tooltip("Khoảng cách tính từ RÌA Camera trước khi object bị dịch chuyển ngược lại.")]
        [SerializeField] private float edgeThreshold = 2f;
        [Tooltip("Chế độ đối xứng khi dịch chuyển vị trí.")]
        [SerializeField] private RepositionSymmetry symmetryMode = RepositionSymmetry.VerticalAxis;

        private ParallaxManager _manager;
        private Renderer _renderer;
        private Material _material;

        private float _speedFactorX;
        private float _speedFactorY;
        private Vector2 _currentTextureOffset;

        private ParallaxMode CurrentMode => overrideGlobalMode ? localMode : _manager.globalMode;

        public void Initialize(ParallaxManager manager, float speedX, float speedY)
        {
            _manager = manager;
            _speedFactorX = speedX;
            _speedFactorY = speedY;

            _renderer = GetComponent<Renderer>();

            if (spriteWidth == 0) spriteWidth = _renderer.bounds.size.x;
            if (spriteHeight == 0) spriteHeight = _renderer.bounds.size.y;

            if (CurrentMode == ParallaxMode.UV_Scroll)
            {
                _material = _renderer.material;
                if (_material.HasProperty("_MainTex") || _material.HasProperty("_BaseMap"))
                    _currentTextureOffset = _material.mainTextureOffset;
            }
        }

        public void UpdateLayer(float baseMoveDeltaX, float baseMoveDeltaY)
        {
            // 1. Áp dụng di chuyển dựa trên Input/Vận tốc
            if (_manager.enableParallaxX) HandleHorizontal(baseMoveDeltaX);
            if (_manager.enableParallaxY) HandleVertical(baseMoveDeltaY);

            // 2. Kiểm tra khoảng cách và Reposition (Áp dụng chung cho cả Transform_Move và Infinite)
            if (CurrentMode == ParallaxMode.Transform_Move || CurrentMode == ParallaxMode.Infinite_Reposition)
            {
                CheckAndApplyReposition();
            }
        }

        private void HandleHorizontal(float moveDeltaX)
        {
            float finalMoveX = moveDeltaX * _speedFactorX;

            if (CurrentMode == ParallaxMode.UV_Scroll && _material != null)
            {
                _currentTextureOffset.x += finalMoveX * 0.1f;
                _material.mainTextureOffset = _currentTextureOffset;
            }
            else if (CurrentMode == ParallaxMode.Transform_Move || CurrentMode == ParallaxMode.Infinite_Reposition)
            {
                transform.Translate(Vector3.left * finalMoveX);
            }
        }

        private void HandleVertical(float moveDeltaY)
        {
            float finalMoveY = moveDeltaY * _speedFactorY;

            if (CurrentMode == ParallaxMode.UV_Scroll && _material != null)
            {
                _currentTextureOffset.y += finalMoveY * 0.1f;
                _material.mainTextureOffset = _currentTextureOffset;
            }
            else if (CurrentMode == ParallaxMode.Transform_Move || CurrentMode == ParallaxMode.Infinite_Reposition)
            {
                transform.Translate(Vector3.down * finalMoveY);
            }
        }

        private void CheckAndApplyReposition()
        {
            if (_manager.MainCam == null) return;

            Camera cam = _manager.MainCam;
            Vector3 camPos = cam.transform.position;

            // Tính toán nửa chiều cao và nửa chiều rộng của Camera (Áp dụng cho Orthographic Camera)
            float camHalfHeight = cam.orthographicSize;
            float camHalfWidth = cam.aspect * camHalfHeight;

            // Tính khoảng cách giới hạn (Nửa Camera + Khoảng cách Threshold + Nửa kích thước Object)
            float limitX = camHalfWidth + edgeThreshold + (spriteWidth * 0.5f);
            float limitY = camHalfHeight + edgeThreshold + (spriteHeight * 0.5f);

            Vector3 pos = transform.position;
            float distX = pos.x - camPos.x;
            float distY = pos.y - camPos.y;

            bool outOfBoundsX = Mathf.Abs(distX) > limitX;
            bool outOfBoundsY = Mathf.Abs(distY) > limitY;

            if (outOfBoundsX || outOfBoundsY)
            {
                Vector3 newPos = pos;

                switch (symmetryMode)
                {
                    case RepositionSymmetry.VerticalAxis:
                        // Đối xứng trục dọc (Giữ nguyên Y, Lật X)
                        if (outOfBoundsX) newPos.x = camPos.x - distX;
                        break;

                    case RepositionSymmetry.HorizontalAxis:
                        // Đối xứng trục ngang (Giữ nguyên X, Lật Y)
                        if (outOfBoundsY) newPos.y = camPos.y - distY;
                        break;

                    case RepositionSymmetry.Point:
                        // Đối xứng tâm (Lật chéo cả X và Y qua tâm Camera)
                        if (outOfBoundsX || outOfBoundsY)
                        {
                            newPos.x = camPos.x - distX;
                            newPos.y = camPos.y - distY;
                        }
                        break;
                }

                transform.position = newPos;
            }
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
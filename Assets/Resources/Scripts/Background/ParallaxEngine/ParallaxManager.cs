using System.Collections.Generic;
using UnityEngine;

namespace ParallaxEngine
{
    public enum ParallaxMode
    {
        UV_Scroll,
        Transform_Move,
        Infinite_Reposition
    }

    [ExecuteInEditMode]
    public class ParallaxManager : MonoBehaviour
    {
        [Header("Global Settings")]
        [Tooltip("Chế độ di chuyển áp dụng cho TẤT CẢ các layer bên trong.")]
        public ParallaxMode globalMode = ParallaxMode.UV_Scroll;

        [Header("References")]
        [Tooltip("Đối tượng để tính toán vận tốc (ví dụ: Player). Nếu trống, hệ thống tự tìm Tag 'Player'.")]
        [SerializeField] private Transform targetSubject;

        [Tooltip("Camera chính dùng để tính toán điểm neo góc nhìn.")]
        [SerializeField] private Camera mainCamera;

        [Header("Layer Generation (Editor)")]
        [Tooltip("Số lượng layer Background muốn tạo tự động. Ít nhất là 1.")]
        [Min(1)] public int numberOfBackgroundLayers = 1;
        [Tooltip("Material mặc định sẽ gán cho các layer vừa tạo ra.")]
        public Material defaultBackgroundMaterial;

        [Header("Parallax Settings X")]
        [Tooltip("Bật/tắt hiệu ứng Parallax trên trục X")]
        public bool enableParallaxX = true;

        [Tooltip("Tốc độ cuộn tự động (base scroll) trên trục X. Có thể đặt giá trị âm hoặc dương.")]
        public float baseScrollSpeedX = 0f;

        [Tooltip("Hệ số nhân vận tốc từ targetSubject để tạo ra độ trượt trên trục X.")]
        public float velocityMultiplierX = 0.5f;

        [Tooltip("Độ mượt (SmoothDamp) khi cập nhật vận tốc trên trục X.")]
        [SerializeField] private float smoothTimeX = 0.25f;

        [Tooltip("Cường độ Parallax tổng thể dựa trên độ sâu Z của Layer (Trục X).")]
        public float parallaxStrengthX = 1f;

        [Header("Parallax Settings Y")]
        [Tooltip("Bật/tắt hiệu ứng Parallax trên trục Y")]
        public bool enableParallaxY = true;

        [Tooltip("Tốc độ cuộn tự động (base scroll) trên trục Y. Có thể đặt giá trị âm hoặc dương.")]
        public float baseScrollSpeedY = 0f;

        [Tooltip("Hệ số nhân vận tốc từ targetSubject để tạo ra độ trượt trên trục Y.")]
        public float velocityMultiplierY = 0.5f;

        [Tooltip("Độ mượt (SmoothDamp) khi cập nhật vận tốc trên trục Y.")]
        [SerializeField] private float smoothTimeY = 0.25f;

        [Tooltip("Cường độ Parallax tổng thể dựa trên độ sâu Z của Layer (Trục Y).")]
        public float parallaxStrengthY = 1f;

        [Header("Z-Depth Config (Editor)")]
        [Tooltip("Khoảng cách Z xa nhất của Background (Nền di chuyển chậm nhất).")]
        [SerializeField] private float bgFarthestZ = 100f;

        [Tooltip("Khoảng cách Z gần nhất của Background.")]
        [SerializeField] private float bgNearestZ = 10f;

        [Tooltip("Số lượng layer thuộc về Foreground (Nằm trước Camera/Player).")]
        [SerializeField] private int foregroundLayerCount = 0;

        [Tooltip("Vị trí Z bắt đầu cho lớp Foreground đầu tiên.")]
        [SerializeField] private float fgStartZ = -5f;

        [Tooltip("Khoảng cách Z giữa các lớp Foreground với nhau.")]
        [SerializeField] private float fgSpacing = -5f;

        [Tooltip("Tự động sắp xếp lại độ sâu Z mỗi khi có thay đổi trong Editor.")]
        [SerializeField] private bool autoSortOnValidate = false;



        private List<ParallaxLayer> _layers = new List<ParallaxLayer>();
        private Rigidbody2D _targetRb;

        private float _smoothedVelocityX;
        private float _velocityRefX;
        private float _smoothedVelocityY;
        private float _velocityRefY;

        public Camera MainCam => mainCamera;

        private void Awake()
        {
            if (!Application.isPlaying) return;

            if (mainCamera == null) mainCamera = Camera.main;

            if (targetSubject == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) targetSubject = player.transform;
            }

            if (targetSubject != null)
                _targetRb = targetSubject.GetComponent<Rigidbody2D>();

            InitializeLayers();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying) return;

            CalculateSmoothedVelocity();
            UpdateLayers();
        }

        private void CalculateSmoothedVelocity()
        {
            float targetVelX = 0f;
            float targetVelY = 0f;

            if (_targetRb != null)
            {
#if UNITY_6000_0_OR_NEWER
                targetVelX = _targetRb.linearVelocity.x;
                targetVelY = _targetRb.linearVelocity.y;
#else
                targetVelX = _targetRb.velocity.x;
                targetVelY = _targetRb.velocity.y;
#endif
            }

            _smoothedVelocityX = Mathf.SmoothDamp(_smoothedVelocityX, targetVelX, ref _velocityRefX, smoothTimeX);
            _smoothedVelocityY = Mathf.SmoothDamp(_smoothedVelocityY, targetVelY, ref _velocityRefY, smoothTimeY);
        }

        private void UpdateLayers()
        {
            float moveDeltaX = (baseScrollSpeedX + (_smoothedVelocityX * velocityMultiplierX)) * Time.deltaTime;
            float moveDeltaY = (baseScrollSpeedY + (_smoothedVelocityY * velocityMultiplierY)) * Time.deltaTime;

            foreach (var layer in _layers)
            {
                layer.UpdateLayer(moveDeltaX, moveDeltaY);
            }
        }

        public void InitializeLayers()
        {
            _layers.Clear();
            foreach (Transform child in transform)
            {
                if (!child.gameObject.activeSelf) continue;

                if (!child.TryGetComponent(out ParallaxLayer layerScript))
                {
                    layerScript = child.gameObject.AddComponent<ParallaxLayer>();
                }

                float zPos = child.localPosition.z;
                float baseSpeedFactor = (zPos > 0) ? 1f / (zPos * 0.1f + 1f) : 1f + Mathf.Abs(zPos) * 0.5f;

                layerScript.Initialize(this, baseSpeedFactor * parallaxStrengthX, baseSpeedFactor * parallaxStrengthY);
                _layers.Add(layerScript);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (autoSortOnValidate && !Application.isPlaying) SortLayersDepth();
        }

        [ContextMenu("Auto Sort Z-Depth")]
        public void SortLayersDepth()
        {
            List<Transform> children = new List<Transform>();
            foreach (Transform child in transform) children.Add(child);

            int totalCount = children.Count;
            if (totalCount == 0) return;

            int safeFgCount = Mathf.Clamp(foregroundLayerCount, 0, totalCount);
            int bgCount = totalCount - safeFgCount;

            for (int i = 0; i < totalCount; i++)
            {
                Transform layer = children[i];
                float zPos;

                if (i < bgCount)
                {
                    layer.name = $"Layer_BG_{i:00}";
                    float t = (bgCount <= 1) ? 0f : (float)i / (bgCount - 1);
                    zPos = Mathf.Lerp(bgFarthestZ, bgNearestZ, t);
                }
                else
                {
                    int fgIndex = i - bgCount;
                    layer.name = $"Layer_FG_{fgIndex:00}";
                    zPos = fgStartZ + (fgIndex * fgSpacing);
                }

                Vector3 newPos = layer.localPosition;
                newPos.z = zPos;
                layer.localPosition = newPos;
            }

            if (Application.isPlaying) InitializeLayers();
        }



        [ContextMenu("Generate Background Layers")]
        public void GenerateBackgroundLayers()
        {
            for (int i = 0; i < numberOfBackgroundLayers; i++)
            {

                GameObject bgObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                bgObject.name = $"Temp_Layer_{i}"; 

                bgObject.transform.SetParent(this.transform);

                bgObject.transform.localScale = new Vector3(2f, 2f, 2f);
                bgObject.transform.localPosition = Vector3.zero;
                bgObject.transform.localRotation = Quaternion.identity;
                Collider col = bgObject.GetComponent<Collider>();
                if (col != null) DestroyImmediate(col);

                MeshRenderer meshRenderer = bgObject.GetComponent<MeshRenderer>();
                if (defaultBackgroundMaterial != null)
                {
                    meshRenderer.sharedMaterial = defaultBackgroundMaterial;
                }
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }

            SortLayersDepth();

            Debug.Log($"Đã tạo thành công {numberOfBackgroundLayers} layer Background!");
        }
#endif

    }
}
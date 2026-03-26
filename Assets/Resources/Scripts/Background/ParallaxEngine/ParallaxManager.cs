using System.Collections.Generic;
using UnityEngine;

namespace ParallaxEngine
{
    public enum ParallaxMode
    {
        [Tooltip("Cuộn UV của Material. Dành cho ảnh lặp (Wrap Mode = Repeat).")]
        UV_Scroll,
        [Tooltip("Dịch chuyển Transform thực tế. Chuyển động có quán tính mượt mà.")]
        Transform_Move,
        [Tooltip("Dịch chuyển Transform & tự động Loop (vòng lại) khi ra khỏi Camera.")]
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
        [Tooltip("Số lượng layer thuộc về Foreground (Nằm trước Camera/Player).")]
        [SerializeField] private int foregroundLayerCount = 0;


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

        [ContextMenu("Initialize Layers")]
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

                if (!Application.isPlaying) continue;

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

            // Tìm Camera và Player để làm mốc tính toán
            Camera cam = mainCamera != null ? mainCamera : Camera.main;
            Transform target = targetSubject;

            if (target == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) target = playerObj.transform;
            }

            // Lấy tọa độ Z làm mốc (Mặc định -10 cho Cam và 0 cho Player nếu không tìm thấy)
            float cameraZ = cam != null ? cam.transform.position.z : -10f;
            float playerZ = target != null ? target.position.z : 0f;

            // Kiểm tra và cảnh báo nếu khoảng cách Camera và Player quá hẹp
            if (cameraZ + 1f >= playerZ - 1f && safeFgCount > 0)
            {
                Debug.LogWarning("ParallaxManager: Khoảng cách giữa Camera và Player quá hẹp để xếp Foreground! Hãy lùi Camera ra xa hơn so với Player.");
            }

            // Thiết lập giới hạn
            float bgStartZ = playerZ + 5f;
            float bgEndZ = 100f;

            float fgStartZ = cameraZ + 1f;
            float fgEndZ = playerZ - 1f;

            for (int i = 0; i < totalCount; i++)
            {
                Transform layer = children[i];
                float zPos;

                if (i < bgCount)
                {
                    // --- BACKGROUND ---
                    // Đánh số tăng dần từ 00, tính từ Player ra xa
                    layer.name = $"Layer_BG_{i:00}";

                    // Nếu chỉ có 1 layer BG, đặt ngay tại bgStartZ
                    float t = (bgCount <= 1) ? 0f : (float)i / (bgCount - 1);
                    zPos = Mathf.Lerp(bgStartZ, bgEndZ, t);
                }
                else
                {
                    // --- FOREGROUND ---
                    // Đánh số tăng dần từ 00, tính từ Camera hướng về Player
                    int fgIndex = i - bgCount;
                    layer.name = $"Layer_FG_{fgIndex:00}";

                    // Nếu chỉ có 1 layer FG, đặt ở giữa (0.5). Nếu nhiều hơn thì trải đều.
                    float t = (safeFgCount <= 1) ? 0.5f : (float)fgIndex / (safeFgCount - 1);
                    zPos = Mathf.Lerp(fgStartZ, fgEndZ, t);
                }

                // Áp dụng Z mới vào vị trí
                Vector3 newPos = layer.position;
                newPos.z = zPos;
                layer.position = newPos;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
            InitializeLayers();
        }


        [ContextMenu("Generate Layers")]
        public void GenerateBackgroundLayers()
        {
            int currentCount = transform.childCount;
            // Tổng số layer cần thiết để hệ thống hoạt động đúng cấu hình
            int targetTotalCount = numberOfBackgroundLayers + foregroundLayerCount;

            if (currentCount < targetTotalCount)
            {
                // THIẾU: Chỉ tạo thêm phần bị thiếu
                int amountToCreate = targetTotalCount - currentCount;
                for (int i = 0; i < amountToCreate; i++)
                {
                    GameObject bgObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    bgObject.name = $"Temp_Layer_{currentCount + i}";

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
                Debug.Log($"[ParallaxManager] Đã tạo THÊM {amountToCreate} layer. Các layer cũ được giữ nguyên!");
            }
            else if (currentCount > targetTotalCount)
            {
                Debug.Log("[ParallaxManager] Số lượng layer đã dư.");

                // THỪA: Xóa bớt các object nằm ở cuối danh sách
                /*
                                int amountToRemove = currentCount - targetTotalCount;
                                for (int i = 0; i < amountToRemove; i++)
                                {
                                    Transform lastChild = transform.GetChild(transform.childCount - 1);
                                    DestroyImmediate(lastChild.gameObject);
                                }
                                Debug.Log($"[ParallaxManager] Đã XÓA BỚT {amountToRemove} layer thừa ở cuối danh sách.");*/
            }
            else
            {
                // ĐÃ ĐỦ
                Debug.Log("[ParallaxManager] Số lượng layer đã đủ theo cấu hình, bỏ qua bước tạo mới.");
            }

            // Gọi hàm sắp xếp để phân bổ lại Z-Depth và đổi tên cho cả layer cũ lẫn mới
            SortLayersDepth();
        }
#endif

    }
}
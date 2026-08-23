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

    [System.Serializable]
    public class ParallaxLayerGroup
    {
        public string groupName = "New Layer";
        [Tooltip("Cường độ Parallax cụ thể của nhóm layer hiện tại (Trục X).")]
        public bool useParallaxStrenghtX = false;
        public float parallaxStrengthX = 1f;
        [Tooltip("Cường độ Parallax cụ thể của nhóm layer hiện tại (Trục Y).")]
        public bool useParallaxStrenghtY = false;
        public float parallaxStrengthY = 1f;

        public List<Transform> elements = new List<Transform>();
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

        [Header("Layer Setup (List Based)")]
        [Tooltip("Danh sách Background (Xa camera). Index 0 sẽ nằm gần Player nhất, Index cuối nằm xa nhất.")]
        public List<ParallaxLayerGroup> backgroundLayers = new List<ParallaxLayerGroup>();

        [Tooltip("Danh sách Foreground (Gần camera). Index 0 sẽ nằm gần Camera nhất.")]
        public List<ParallaxLayerGroup> foregroundLayers = new List<ParallaxLayerGroup>();

        [SerializeField] private Material defaultBackgroundMaterial;


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
        [Tooltip("Tự động sắp xếp lại độ sâu Z mỗi khi có thay đổi trong Editor.")]
        public bool autoSortOnValidate = false;



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
                GameObject player = GameObject.FindGameObjectWithTag(GameConstants.TAG_PLAYER);
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

            // Hàm cục bộ (Local Function) để quét nhanh List
            void InitGroup(List<ParallaxLayerGroup> groups)
            {
                foreach (var group in groups)
                {
                    foreach (var child in group.elements)
                    {
                        if (child == null || !child.gameObject.activeSelf) continue;

                        if (!child.TryGetComponent(out ParallaxLayer layerScript))
                        {
                            layerScript = child.gameObject.AddComponent<ParallaxLayer>();
                        }

                        if (!Application.isPlaying) continue;

                        float zPos = child.localPosition.z;
                        float baseSpeedFactor = (zPos > 0) ? 1f / (zPos * 0.1f + 1f) : 1f + Mathf.Abs(zPos) * 0.5f;
                        
                        float finalParallaxStrengthX = group.useParallaxStrenghtX ? parallaxStrengthX * group.parallaxStrengthX : baseSpeedFactor * parallaxStrengthX;
                        float finalParallaxStrengthY = group.useParallaxStrenghtY ? parallaxStrengthY * group.parallaxStrengthY : baseSpeedFactor * parallaxStrengthY;

                        layerScript.Initialize(this, finalParallaxStrengthX, baseSpeedFactor * finalParallaxStrengthY);
                        _layers.Add(layerScript);
                    }
                }
            }

            InitGroup(backgroundLayers);
            InitGroup(foregroundLayers);
        }
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (autoSortOnValidate && !Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null && autoSortOnValidate && !Application.isPlaying)
                    {
                        SortLayersDepth();
                    }
                };
            }
        }

        [ContextMenu("Auto Sort Z-Depth")]
        public void SortLayersDepth()
        {
            Camera cam = mainCamera != null ? mainCamera : Camera.main;
            Transform target = targetSubject;
            if (target == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag(GameConstants.TAG_PLAYER);
                if (playerObj != null) target = playerObj.transform;
            }

            float cameraZ = cam != null ? cam.transform.position.z : -10f;
            float playerZ = target != null ? target.position.z : 0f;

            // Xếp Background
            if (backgroundLayers.Count > 0)
            {
                float bgStartZ = playerZ + 5f;
                float bgEndZ = 100f;

                for (int i = 0; i < backgroundLayers.Count; i++)
                {
                    float t = (backgroundLayers.Count <= 1) ? 0f : (float)i / (backgroundLayers.Count - 1);
                    float groupZ = Mathf.Lerp(bgStartZ, bgEndZ, t);

                    foreach (var element in backgroundLayers[i].elements)
                    {
                        if (element != null)
                        {
                            Vector3 pos = element.position;
                            pos.z = groupZ;
                            element.position = pos;
                        }
                    }
                }
            }

            // Xếp Foreground
            if (foregroundLayers.Count > 0)
            {
                float fgStartZ = cameraZ + 1f;
                float fgEndZ = playerZ - 1f;

                for (int i = 0; i < foregroundLayers.Count; i++)
                {
                    float t = (foregroundLayers.Count <= 1) ? 0.5f : (float)i / (foregroundLayers.Count - 1);
                    float groupZ = Mathf.Lerp(fgStartZ, fgEndZ, t);

                    foreach (var element in foregroundLayers[i].elements)
                    {
                        if (element != null)
                        {
                            Vector3 pos = element.position;
                            pos.z = groupZ;
                            element.position = pos;
                        }
                    }
                }
            }

            ApplyNamingConvention();
            SortHierarchyOrder();

            if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(this);
            if (Application.isPlaying) InitializeLayers();
        }

        [ContextMenu("Apply Naming Convention")]
        public void ApplyNamingConvention()
        {
            RenameGroup(backgroundLayers, "BG");
            RenameGroup(foregroundLayers, "FG");
        }

        private void RenameGroup(List<ParallaxLayerGroup> layers, string prefix)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                ParallaxLayerGroup group = layers[i];
                // Đổi luôn tên của Group hiển thị trong List cho đồng bộ
                group.groupName = $"{prefix}-Group-{i}";

                int spriteCount = 0;
                int quadCount = 0;

                foreach (Transform element in group.elements)
                {
                    if (element == null) continue;

                    bool isQuad = element.TryGetComponent<MeshRenderer>(out _);
                    string typeName = isQuad ? "Quad" : "sprite";

                    // Đặt tên chuẩn theo format: BG-0-sprite, FG-1-Quad...
                    string newName = $"{prefix}-{i}-{typeName}";

                    // Nếu có nhiều vật thể cùng loại trong 1 độ sâu, thêm hậu tố để tránh trùng tên tuyệt đối
                    int count = isQuad ? ++quadCount : ++spriteCount;
                    if (count > 1) newName += $" ({count})";

                    element.name = newName;
                }
            }
        }

        [ContextMenu("Sort Hierarchy Order")]
        public void SortHierarchyOrder()
        {
            int currentSiblingIndex = 0;

            // Đưa toàn bộ các phần tử Background lên trên cùng của Hierarchy
            foreach (var group in backgroundLayers)
            {
                foreach (var element in group.elements)
                {
                    if (element != null)
                    {
                        element.SetSiblingIndex(currentSiblingIndex);
                        currentSiblingIndex++;
                    }
                }
            }

            // Xếp các phần tử Foreground nối tiếp ngay bên dưới
            foreach (var group in foregroundLayers)
            {
                foreach (var element in group.elements)
                {
                    if (element != null)
                    {
                        element.SetSiblingIndex(currentSiblingIndex);
                        currentSiblingIndex++;
                    }
                }
            }

            Debug.Log("[ParallaxManager] Đã sắp xếp lại thứ tự trên Hierarchy cho gọn gàng!");
        }

        // ==========================================
        // UTILITY: AUTO GENERATE LAYER GROUPS
        // ==========================================

        public void AddBackgroundLayer(bool isQuad)
        {
            AddLayerGroup(backgroundLayers, "BG", isQuad);
        }

        public void AddForegroundLayer(bool isQuad)
        {
            AddLayerGroup(foregroundLayers, "FG", isQuad);
        }

        private void AddLayerGroup(List<ParallaxLayerGroup> list, string prefix, bool isQuad)
        {
            GameObject go;
            string typeName = isQuad ? "Quad" : "sprite";

            if (isQuad)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                DestroyImmediate(go.GetComponent<Collider>());
                if (defaultBackgroundMaterial != null) go.GetComponent<MeshRenderer>().sharedMaterial = defaultBackgroundMaterial;
            }
            else
            {
                go = new GameObject();
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                if (defaultBackgroundMaterial != null) sr.sharedMaterial = defaultBackgroundMaterial;
            }

            go.transform.SetParent(this.transform);
            go.transform.localScale = new Vector3(2f, 2f, 2f);

            // Gán tên tạm thời chuẩn format (Hàm SortLayersDepth gọi bên dưới sẽ xác nhận lại)
            int depthIndex = list.Count;
            go.name = $"{prefix}-{depthIndex}-{typeName}";

            ParallaxLayerGroup newGroup = new ParallaxLayerGroup();
            newGroup.groupName = $"{prefix}-Group-{depthIndex}";
            newGroup.elements.Add(go.transform);

            list.Add(newGroup);

            Debug.Log($"[ParallaxManager] Đã tạo mới: {go.name}");

            // Xếp Z và đồng thời kích hoạt luôn hàm Đổi tên đồng loạt
            SortLayersDepth();
        }
#endif
    }
}
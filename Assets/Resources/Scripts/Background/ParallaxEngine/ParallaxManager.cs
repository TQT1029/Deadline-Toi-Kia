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

        [Header("Background Setup")]
        [Min(0)] public int bgQuadCount = 1;
        [Min(0)] public int bgSpriteCount = 0;
        public Material defaultBackgroundMaterial;

        [Header("Foreground Setup")]
        public bool enableForeground = false;
        [Min(0)] public int fgQuadCount = 0;
        [Min(0)] public int fgSpriteCount = 0;

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
            // Phân loại object hiện tại
            List<Transform> sprites = new List<Transform>();
            List<Transform> quads = new List<Transform>();
            foreach (Transform child in transform)
            {
                if (child.TryGetComponent<SpriteRenderer>(out _)) sprites.Add(child);
                else if (child.TryGetComponent<MeshRenderer>(out _)) quads.Add(child);
            }

            // Tính toán số lượng hợp lệ thực tế đang có
            int actualBgSprites = Mathf.Min(bgSpriteCount, sprites.Count);
            int actualBgQuads = Mathf.Min(bgQuadCount, quads.Count);

            int fgSpritesTarget = enableForeground ? fgSpriteCount : 0;
            int fgQuadsTarget = enableForeground ? fgQuadCount : 0;
            int actualFgQuads = Mathf.Min(fgQuadsTarget, quads.Count - actualBgQuads);
            int actualFgSprites = Mathf.Min(fgSpritesTarget, sprites.Count - actualBgSprites);

            List<Transform> bgLayers = new List<Transform>();
            List<Transform> fgLayers = new List<Transform>();

            // --- ĐỔI TÊN VÀ GOM NHÓM LẠI ---
            int count = 0;
            for (int i = 0; i < actualBgSprites; i++) { sprites[i].name = $"Layer_BG_Sprite_{count:00}"; bgLayers.Add(sprites[i]); count++; }
            for (int i = 0; i < actualBgQuads; i++) { quads[i].name = $"Layer_BG_Quad_{count:00}"; bgLayers.Add(quads[i]); count++; }

            count = 0;
            for (int i = 0; i < actualFgSprites; i++) { int idx = actualBgSprites + i; sprites[idx].name = $"Layer_FG_Sprite_{count:00}"; fgLayers.Add(sprites[idx]); count++; }
            for (int i = 0; i < actualFgQuads; i++) { int idx = actualBgQuads + i; quads[idx].name = $"Layer_FG_Quad_{count:00}"; fgLayers.Add(quads[idx]); count++; }

            // --- TÍNH TOÁN Z-DEPTH MỚI ---
            Camera cam = mainCamera != null ? mainCamera : Camera.main;
            Transform target = targetSubject;
            if (target == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) target = playerObj.transform;
            }

            float cameraZ = cam != null ? cam.transform.position.z : -10f;
            float playerZ = target != null ? target.position.z : 0f;

            // Xếp Background (Xa ra ngoài)
            if (bgLayers.Count > 0)
            {
                float bgStartZ = playerZ + 5f;
                float bgEndZ = 100f;
                for (int i = 0; i < bgLayers.Count; i++)
                {
                    float t = (bgLayers.Count <= 1) ? 0f : (float)i / (bgLayers.Count - 1);
                    Vector3 pos = bgLayers[i].position;
                    pos.z = Mathf.Lerp(bgStartZ, bgEndZ, t);
                    bgLayers[i].position = pos;
                }
            }

            // Xếp Foreground (Gần Camera)
            if (fgLayers.Count > 0)
            {
                float fgStartZ = cameraZ + 1f;
                float fgEndZ = playerZ - 1f;
                for (int i = 0; i < fgLayers.Count; i++)
                {
                    float t = (fgLayers.Count <= 1) ? 0.5f : (float)i / (fgLayers.Count - 1);
                    Vector3 pos = fgLayers[i].position;
                    pos.z = Mathf.Lerp(fgStartZ, fgEndZ, t);
                    fgLayers[i].position = pos;
                }
            }

            if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(this);
            if (Application.isPlaying) InitializeLayers();
        }

        [ContextMenu("Generate Layers")]
        public void GenerateBackgroundLayers()
        {
            int targetSprites = bgSpriteCount + (enableForeground ? fgSpriteCount : 0);
            int targetQuads = bgQuadCount + (enableForeground ? fgQuadCount : 0);

            int currentQuads = 0, currentSprites = 0;
            foreach (Transform child in transform)
            {
                if (child.TryGetComponent<SpriteRenderer>(out _)) currentSprites++;
                else if (child.TryGetComponent<MeshRenderer>(out _)) currentQuads++;
            }

            int missingSprites = Mathf.Max(0, targetSprites - currentSprites);
            int missingQuads = Mathf.Max(0, targetQuads - currentQuads);

            for (int i = 0; i < missingSprites; i++)
            {
                GameObject go = new GameObject();
                go.transform.SetParent(this.transform);
                go.transform.localScale = new Vector3(2f, 2f, 2f);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                if (defaultBackgroundMaterial != null) sr.sharedMaterial = defaultBackgroundMaterial;
            }

            for (int i = 0; i < missingQuads; i++)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.transform.SetParent(this.transform);
                go.transform.localScale = new Vector3(2f, 2f, 2f);
                DestroyImmediate(go.GetComponent<Collider>());
                if (defaultBackgroundMaterial != null) go.GetComponent<MeshRenderer>().sharedMaterial = defaultBackgroundMaterial;
            }


            Debug.Log($"[ParallaxManager] Đã kiểm tra và tạo thêm: {missingQuads} Quads, {missingSprites} Sprites.");
            SortLayersDepth(); // Tự động đổi tên và xếp Z
        }
#endif
    }
}
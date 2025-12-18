using UnityEngine;
using System;
using System.Collections.Generic;

public class ObstacleGenerator : MonoBehaviour
{
    public static ObstacleGenerator Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // --- EVENTS (Gửi tín hiệu sang ItemGenerator) ---
    public event Action<Vector3> OnRequestSingleItem;
    // Param: Vị trí tâm, Số lượng tối đa, Chiều dài sàn
    public event Action<Vector3, int, float> OnRequestItemRow;
    // Param: Vị trí tâm sàn, Chiều dài sàn (Dùng cho mini platform để biến tấu)
    public event Action<Vector3, float> OnRequestItemOnPlatform;
    // Param: Mép trái, Mép phải, Độ cao cơ sở
    public event Action<float, float, float> OnRequestItemPattern;

    [Header("--- CORE REFERENCES ---")]
    public Transform playerTransform;
    public GameObject objectContainer;

    [Header("--- LIBRARIES ---")]
    public List<ObstacleData> obstacleLibrary;
    public List<PlatformData> miniPlatformLibrary;

    [Space(20)]
    [Header("--- SPAWN CHANCES ---")]
    [Tooltip("Tỉ lệ % xuất hiện vật cản/sàn bay. Nếu trượt sẽ là đất trống chứa Item Pattern.")]
    [Range(0, 100)] public int spawnContentChance = 65;

    [Tooltip("Trong số các lần spawn content, bao nhiêu % là Chuỗi Sàn Bay?")]
    [Range(0, 100)] public int miniPlatformChance = 50;

    [Space(20)]
    [Header("--- ITEM SETTINGS ---")]
    [Tooltip("Tỉ lệ có Item trên nóc Obstacle/Sàn bay")]
    [Range(0, 100)] public int itemOnTopChance = 90;
    [Tooltip("Tỉ lệ có Item dưới gầm sàn bay (Nếu đủ cao và CÒN ĐẤT)")]
    [Range(0, 100)] public int itemUnderPlatformChance = 70;

    [Space(20)]
    [Header("--- MINI PLATFORM CONFIG ---")]
    public int minChainLength = 2;
    public int maxChainLength = 5;

    [Tooltip("Khoảng cách ngang TỐI THIỂU giữa các bậc thang")]
    public float minMiniPlatformGap = 1.0f;
    [Tooltip("Khoảng cách ngang TỐI ĐA giữa các bậc thang")]
    public float maxMiniPlatformGap = 3.0f;

    [Header("Height Logic")]
    public float minFirstHeight = 1.5f;
    public float maxFirstHeight = 2.5f;
    public float minStepDiff = 0.5f;
    public float maxStepDiff = 2.0f;
    public float absoluteMaxHeight = 5.5f;
    [Tooltip("Sàn phải cao hơn mức này mới spawn coin ở dưới đất")]
    public float heightThresholdForUnderneath = 3.0f;

    [Space(20)]
    [Header("--- SPACING (Khoảng cách) ---")]
    public float destroyDistanceBehind = 20f;

    [Tooltip("Khoảng nghỉ tối thiểu giữa các cụm vật cản")]
    public float minGap = 5f;
    [Tooltip("Khoảng nghỉ tối đa giữa các cụm vật cản")]
    public float maxGap = 10f;

    [Header("Padding (Lề an toàn cho Pattern)")]
    public float minPadding = 1.5f;
    public float maxPadding = 3.0f;

    private float currentSpawnX;
    private Queue<GameObject> activeObjects = new Queue<GameObject>();

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (EndlessGameController.Instance != null)
        {
            EndlessGameController.Instance.OnBasePlatformSpawned += OnBasePlatformSpawned;
        }

        if (playerTransform != null) currentSpawnX = playerTransform.position.x + 10f;
    }

    private void OnDestroy()
    {
        if (EndlessGameController.Instance != null)
        {
            EndlessGameController.Instance.OnBasePlatformSpawned -= OnBasePlatformSpawned;
        }
    }

    private void Update()
    {
        RemoveOldObjects();
    }

    private void OnBasePlatformSpawned(float startX, float endX, float baseHeight)
    {
        if (currentSpawnX < startX) currentSpawnX = startX;

        // Trừ hao 2m cuối sàn để không spawn vật thể chênh vênh mép hố
        float safeLimitX = endX - 2.0f;

        while (currentSpawnX < safeLimitX)
        {
            // Nếu hàm trả về false (hết đất), thoát vòng lặp
            if (!SpawnLogicGroup(safeLimitX, baseHeight)) break;
        }
    }

    private bool SpawnLogicGroup(float limitX, float baseHeight)
    {
        // 1. TÍNH TOÁN GAP & PATTERN (Item trên đất trống)
        float gap = UnityEngine.Random.Range(minGap, maxGap);

        // Tính lề ngẫu nhiên
        float paddingFront = UnityEngine.Random.Range(minPadding, maxPadding);
        float paddingBack = UnityEngine.Random.Range(minPadding, maxPadding);

        // Khoảng trống khả dụng cho Pattern
        float availableSpace = gap - (paddingFront + paddingBack);

        if (availableSpace > 2.0f)
        {
            float patternStartX = currentSpawnX + paddingFront;
            float patternEndX = currentSpawnX + gap - paddingBack;

            // Check an toàn: Pattern trên mặt đất KHÔNG ĐƯỢC lòi ra hố
            if (patternEndX <= limitX)
            {
                OnRequestItemPattern?.Invoke(patternStartX, patternEndX, baseHeight);
            }
        }

        currentSpawnX += gap;

        // Nếu vị trí bắt đầu spawn đã vượt quá giới hạn đất -> Dừng spawn cho segment này
        if (currentSpawnX >= limitX) return false;

        // 2. SPAWN OBSTACLE HOẶC MINI PLATFORM
        bool doSpawnContent = UnityEngine.Random.Range(0, 100) < spawnContentChance;
        float addedWidth = 0;

        if (doSpawnContent)
        {
            bool isChain = UnityEngine.Random.Range(0, 100) < miniPlatformChance;

            if (isChain && miniPlatformLibrary.Count > 0)
            {
                // [THAY ĐỔI] Mini Platform được phép bay ra ngoài hố
                addedWidth = SpawnStaircaseChain(currentSpawnX, baseHeight, limitX);
            }
            else
            {
                ObstacleData obs = GetRandomObstacle();
                // [QUAN TRỌNG] Vật cản dưới đất (Thùng, Gai) BẮT BUỘC phải nằm trong đất
                if (obs != null && (currentSpawnX + obs.width) <= limitX)
                {
                    addedWidth = SpawnObstacle(currentSpawnX, baseHeight, obs);
                }
            }
        }

        currentSpawnX += addedWidth;
        return true;
    }

    private float SpawnStaircaseChain(float startX, float baseHeight, float limitX)
    {
        int length = UnityEngine.Random.Range(minChainLength, maxChainLength + 1);
        float currentHeight = 0f;
        float localX = startX;

        for (int i = 0; i < length; i++)
        {
            PlatformData miniData = GetRandomMiniPlatform();
            if (miniData == null) continue;

            // --- TÍNH ĐỘ CAO ---
            if (i == 0)
                currentHeight = UnityEngine.Random.Range(minFirstHeight, maxFirstHeight);
            else
                currentHeight += UnityEngine.Random.Range(minStepDiff, maxStepDiff);

            currentHeight = Mathf.Clamp(currentHeight, 1.0f, absoluteMaxHeight);

            // --- TÍNH VỊ TRÍ ---
            float halfWidth = miniData.length / 2f;
            float gap = (i == 0) ? 0 : UnityEngine.Random.Range(minMiniPlatformGap, maxMiniPlatformGap);

            // [ĐÃ SỬA] Bỏ check "break" ở đây để Mini Platform được phép bay ra hố
            // Chỉ cần đảm bảo localX tịnh tiến đúng hướng
            localX += gap + halfWidth;

            // --- SPAWN OBJECT ---
            Vector3 pos = new Vector3(localX, baseHeight + currentHeight, 0);
            GameObject plat = Instantiate(miniData.prefab, pos, Quaternion.identity);
            RegisterObject(plat);

            // 1. ITEM TRÊN SÀN (Luôn spawn vì item đi theo sàn)
            if (UnityEngine.Random.Range(0, 100) < itemOnTopChance)
            {
                float itemY = pos.y + miniData.itemHeightOffset;
                Vector3 centerItemPos = new Vector3(localX, itemY, 0);

                OnRequestItemOnPlatform?.Invoke(centerItemPos, miniData.length);
            }

            // 2. ITEM DƯỚI GẦM (Check kỹ: Chỉ spawn nếu BÊN DƯỚI CÒN ĐẤT)
            // Vì sàn có thể bay ra hố, nhưng coin dưới gầm (level mặt đất) thì không nên bay giữa hố
            if (currentHeight >= heightThresholdForUnderneath)
            {
                // Kiểm tra: Mép phải của item row có nằm trong giới hạn đất không?
                // Ước lượng chiều dài item row khoảng miniData.length
                float itemRowEnd = localX + (miniData.length / 2f);

                if (itemRowEnd <= limitX)
                {
                    if (UnityEngine.Random.Range(0, 100) < itemUnderPlatformChance)
                    {
                        Vector3 bottomCenterPos = new Vector3(localX, baseHeight + 0.5f, 0);
                        int maxItemsBottom = Mathf.FloorToInt(miniData.length - 1.0f);
                        if (maxItemsBottom < 1) maxItemsBottom = 1;

                        OnRequestItemRow?.Invoke(bottomCenterPos, maxItemsBottom, miniData.length);
                    }
                }
            }

            localX += halfWidth;
        }

        return localX - startX;
    }

    private float SpawnObstacle(float x, float y, ObstacleData obs)
    {
        float prefabY = obs.prefab.transform.position.y;
        GameObject o = Instantiate(obs.prefab, new Vector3(x, y + prefabY, 0), Quaternion.identity);
        RegisterObject(o);

        // Spawn item trên nóc vật cản
        if (UnityEngine.Random.Range(0, 100) < itemOnTopChance)
        {
            int count = UnityEngine.Random.Range(obs.minItemsOnTop, obs.maxItemsOnTop + 1);
            if (count > 0)
            {
                Vector3 topPos = new Vector3(x, y + obs.topHeightOffset, 0);
                OnRequestItemRow?.Invoke(topPos, count, obs.width);
            }
        }

        return obs.width;
    }

    // --- UTILITIES ---
    private void RegisterObject(GameObject obj)
    {
        activeObjects.Enqueue(obj);
        if (objectContainer) obj.transform.SetParent(objectContainer.transform);
        else obj.transform.SetParent(this.transform);
    }

    private void RemoveOldObjects()
    {
        if (activeObjects.Count > 0)
        {
            GameObject o = activeObjects.Peek();
            if (o == null) { activeObjects.Dequeue(); return; }
            if (playerTransform.position.x - o.transform.position.x > destroyDistanceBehind)
                Destroy(activeObjects.Dequeue());
        }
    }

    private ObstacleData GetRandomObstacle()
    {
        if (obstacleLibrary == null || obstacleLibrary.Count == 0) return null;
        return obstacleLibrary[UnityEngine.Random.Range(0, obstacleLibrary.Count)];
    }

    private PlatformData GetRandomMiniPlatform()
    {
        if (miniPlatformLibrary == null || miniPlatformLibrary.Count == 0) return null;
        float t = 0; foreach (var p in miniPlatformLibrary) t += p.spawnWeight;
        float r = UnityEngine.Random.Range(0, t);
        float c = 0; foreach (var p in miniPlatformLibrary) { c += p.spawnWeight; if (r < c) return p; }
        return miniPlatformLibrary[0];
    }
}
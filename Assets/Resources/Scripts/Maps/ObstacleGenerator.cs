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

    // --- EVENTS ---
    public event Action<Vector3> OnRequestSingleItem;
    public event Action<Vector3, int, float> OnRequestItemRow;
    public event Action<Vector3, float> OnRequestItemOnPlatform;
    public event Action<float, float, float> OnRequestItemPattern;

    [Header("--- CORE REFERENCES ---")]
    public Transform playerTransform;
    public GameObject objectContainer;

    [Header("--- LIBRARIES ---")]
    public List<ObstacleData> obstacleLibrary;
    public List<PlatformData> miniPlatformLibrary;

    [Space(20)]
    [Header("--- SPAWN CHANCES ---")]
    [Tooltip("Tỉ lệ % xuất hiện vật cản. Giảm xuống để có nhiều đất trống cho Pattern.")]
    [Range(0, 100)] public int spawnContentChance = 60;

    [Tooltip("Tỉ lệ % là Chuỗi Sàn Bay?")]
    [Range(0, 100)] public int miniPlatformChance = 50;

    [Space(20)]
    [Header("--- ITEM SETTINGS ---")]
    [Range(0, 100)] public int itemOnTopChance = 90;
    [Range(0, 100)] public int itemUnderPlatformChance = 80; // Tăng lên để dễ ra

    [Space(20)]
    [Header("--- MINI PLATFORM CONFIG ---")]
    public int minChainLength = 2;
    public int maxChainLength = 5;

    public float minMiniPlatformGap = 1.0f;
    public float maxMiniPlatformGap = 3.0f;

    [Header("Height Logic")]
    public float minFirstHeight = 1.8f; // Tăng nhẹ để bậc đầu tiên đã có thể chui lọt
    public float maxFirstHeight = 2.8f;
    public float minStepDiff = 0.5f;
    public float maxStepDiff = 2.0f;
    public float absoluteMaxHeight = 6.0f;

    [Tooltip("Hạ thấp ngưỡng này xuống để dễ spawn coin dưới gầm hơn")]
    public float heightThresholdForUnderneath = 2.2f;

    [Space(20)]
    [Header("--- SPACING (Khoảng cách) ---")]
    public float destroyDistanceBehind = 20f;

    [Tooltip("Tăng Gap để có chỗ vẽ Pattern chữ cái")]
    public float minGap = 6f;
    public float maxGap = 16f;

    [Header("Padding (Giảm lề để tận dụng chỗ trống)")]
    public float minPadding = 1.0f;
    public float maxPadding = 2.5f;

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
        float safeLimitX = endX - 2.0f;

        while (currentSpawnX < safeLimitX)
        {
            if (!SpawnLogicGroup(safeLimitX, baseHeight)) break;
        }
    }

    private bool SpawnLogicGroup(float limitX, float baseHeight)
    {
        // 1. TÍNH TOÁN GAP & PATTERN
        float gap = UnityEngine.Random.Range(minGap, maxGap);

        float paddingFront = UnityEngine.Random.Range(minPadding, maxPadding);
        float paddingBack = UnityEngine.Random.Range(minPadding, maxPadding);
        float availableSpace = gap - (paddingFront + paddingBack);

        // Pattern chữ cái cần khoảng 3-5m, nên availableSpace > 2.5m là thử vẽ được rồi
        if (availableSpace > 2.5f)
        {
            float patternStartX = currentSpawnX + paddingFront;
            float patternEndX = currentSpawnX + gap - paddingBack;

            if (patternEndX <= limitX)
            {
                OnRequestItemPattern?.Invoke(patternStartX, patternEndX, baseHeight);
            }
        }

        currentSpawnX += gap;

        if (currentSpawnX >= limitX) return false;

        // 2. SPAWN CONTENT
        bool doSpawnContent = UnityEngine.Random.Range(0, 100) < spawnContentChance;
        float addedWidth = 0;

        if (doSpawnContent)
        {
            bool isChain = UnityEngine.Random.Range(0, 100) < miniPlatformChance;

            if (isChain && miniPlatformLibrary.Count > 0)
            {
                addedWidth = SpawnStaircaseChain(currentSpawnX, baseHeight, limitX);
            }
            else
            {
                ObstacleData obs = GetRandomObstacle();
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

            // Check nếu đặt tấm này mà lòi ra ngoài đất -> Ngắt chuỗi (để không bay ra hố)
            if (localX + gap + miniData.length > limitX) break;

            localX += gap + halfWidth;

            // --- SPAWN OBJECT ---
            Vector3 pos = new Vector3(localX, baseHeight + currentHeight, 0);
            GameObject plat = Instantiate(miniData.prefab, pos, Quaternion.identity);
            RegisterObject(plat);

            // 1. ITEM TRÊN SÀN (Gọi Event đặc biệt để biến tấu hình dạng)
            if (UnityEngine.Random.Range(0, 100) < itemOnTopChance)
            {
                float itemY = pos.y + miniData.itemHeightOffset;
                Vector3 centerItemPos = new Vector3(localX, itemY, 0);

                OnRequestItemOnPlatform?.Invoke(centerItemPos, miniData.length);
            }

            // 2. ITEM DƯỚI GẦM
            if (currentHeight >= heightThresholdForUnderneath)
            {
                if (UnityEngine.Random.Range(0, 100) < itemUnderPlatformChance)
                {
                    // Nâng Y lên +1.0f để tránh bị IsPositionClear coi là dính đất
                    Vector3 bottomCenterPos = new Vector3(localX, baseHeight + 1.0f, 0);

                    int maxItemsBottom = Mathf.FloorToInt(miniData.length - 0.5f);
                    if (maxItemsBottom < 1) maxItemsBottom = 1;

                    OnRequestItemRow?.Invoke(bottomCenterPos, maxItemsBottom, miniData.length);
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
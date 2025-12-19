using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Header("References")]
    public Transform playerTransform;
    public GameObject objectContainer;

    [Header("Configurations")]
    public List<ObstacleData> obstacleLibrary;
    public List<ItemData> itemLibrary;

    [Header("Mini Platforms (Bậc thang bay)")]
    public List<PlatformData> miniPlatformLibrary;
    [Range(0, 100)] public int miniPlatformChance = 30;

    [Header("Staircase Logic (QUAN TRỌNG)")]
    public int minChainLength = 3;
    public int maxChainLength = 5;
    public float minChainGap = 2.0f; // Khoảng cách ngang giữa các bậc

    [Tooltip("Độ cao tối thiểu của bậc ĐẦU TIÊN so với mặt đất")]
    public float minFirstHeight = 1.5f;
    [Tooltip("Độ cao tối đa của bậc ĐẦU TIÊN so với mặt đất")]
    public float maxFirstHeight = 2.5f;

    [Tooltip("Chênh lệch độ cao tối thiểu so với bậc TRƯỚC")]
    public float minStepDiff = 0.5f;
    [Tooltip("Chênh lệch độ cao tối đa so với bậc TRƯỚC")]
    public float maxStepDiff = 2.0f;

    [Tooltip("Giới hạn trần độ cao tuyệt đối")]
    public float absoluteMaxHeight = 6.0f;

    [Tooltip("Nếu sàn bay cao hơn mức này, sẽ spawn obstacle/item ở dưới đất")]
    public float heightThresholdForUnderneath = 3.0f;

    [Header("General Settings")]
    public float destroyDistanceBehind = 20f;
    public LayerMask obstacleLayer;
    public float checkRadius = 0.4f;

    [Header("Spacing")]
    public float minGap = 6f;
    public float maxGap = 10f;
    [Range(0, 100)] public int spawnObstacleChance = 40;
    [Range(0, 100)] public int itemOnObstacleChance = 70;
    public float itemSpacing = 1.0f;

    private float currentSpawnX;
    private Queue<GameObject> activeObjects = new Queue<GameObject>();

    private enum ItemPattern
    {
        Line, Grid, Wave, ArrowComplex, Diamond, RectHollow, RectVertical, RectHorizontal,
        ShapeVLU, ShapeAPlus, Triangle, StairsUp, StairsDown, ZigZag, DoubleLine
    }

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

        if (playerTransform != null) currentSpawnX = playerTransform.position.x + 5f;
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
            SpawnGroup(safeLimitX, baseHeight);
        }
    }

    private void SpawnGroup(float limitX, float baseHeight)
    {
        float gap = Random.Range(minGap, maxGap);
        currentSpawnX += gap;

        if (currentSpawnX > limitX) return;

        bool doSpawnObstacle = Random.Range(0, 100) < spawnObstacleChance;
        float addedWidth = 0;

        if (doSpawnObstacle)
        {
            bool isChain = Random.Range(0, 100) < miniPlatformChance;

            if (isChain && miniPlatformLibrary.Count > 0)
            {
                // Spawn Mini Platform (Bậc thang bay)
                addedWidth = SpawnStaircaseChain(currentSpawnX, baseHeight);
            }
            else
            {
                // Spawn Obstacle thường
                ObstacleData obs = GetRandomObstacle();
                if (obs != null && (currentSpawnX + obs.width) <= limitX)
                {
                    addedWidth = SpawnObstacle(currentSpawnX, baseHeight, obs);
                }
                else
                {
                    addedWidth = SpawnItemPattern(currentSpawnX, baseHeight);
                }
            }
        }
        else
        {
            addedWidth = SpawnItemPattern(currentSpawnX, baseHeight);
        }

        currentSpawnX += addedWidth;
    }

    // --- [LOGIC MỚI] SPAWN BẬC THANG BAY ---
    private float SpawnStaircaseChain(float startX, float baseHeight)
    {
        int length = Random.Range(minChainLength, maxChainLength + 1);

        // Biến lưu độ cao hiện tại của sàn (tương đối so với mặt đất)
        float currentRelativeY = 0f;
        float localX = startX;

        for (int i = 0; i < length; i++)
        {
            PlatformData miniData = GetRandomMiniPlatform();
            if (miniData == null) continue;

            // --- TÍNH TOÁN ĐỘ CAO (Relative Height) ---
            if (i == 0)
            {
                // Bậc đầu tiên: Random độ cao khởi điểm
                currentRelativeY = Random.Range(minFirstHeight, maxFirstHeight);
            }
            else
            {
                // Các bậc sau: Random chênh lệch so với bậc trước
                // Chênh lệch từ minStepDiff đến maxStepDiff
                float diff = Random.Range(minStepDiff, maxStepDiff);

                // Quyết định hướng: 80% đi lên/ngang, 20% đi xuống (nếu muốn)
                // Theo yêu cầu của bạn là "cao hơn hoặc ngang", nên ta luôn cộng dương
                currentRelativeY += diff;
            }

            // Kẹp độ cao trần
            currentRelativeY = Mathf.Clamp(currentRelativeY, 1.0f, absoluteMaxHeight);

            // --- TÍNH TOÁN VỊ TRÍ X ---
            float halfWidth = miniData.length / 2f;
            float gap = (i == 0) ? 0 : minChainGap;
            localX += gap + halfWidth;

            // 1. Instantiate Mini Platform
            Vector3 pos = new Vector3(localX, baseHeight + currentRelativeY, 0);
            GameObject plat = Instantiate(miniData.prefab, pos, Quaternion.identity);
            SyncColliderSize(plat, miniData.length);

            if (objectContainer) plat.transform.SetParent(objectContainer.transform);
            RegisterObject(plat);

            // 2. Spawn Item trên sàn mini
            if (Random.value > 0.3f)
            {
                SpawnItem(new Vector3(localX, pos.y + miniData.itemHeightOffset, 0));
            }

            // 3. --- LOGIC QUAN TRỌNG: Spawn Dưới Gầm ---
            // Nếu sàn đủ cao, spawn chướng ngại vật/coin bên dưới
            if (currentRelativeY >= heightThresholdForUnderneath)
            {
                SpawnUnderneathObstacle(localX, baseHeight, minChainGap);
            }

            localX += halfWidth;
        }

        return localX - startX;
    }

    private void SpawnUnderneathObstacle(float x, float y, float safeWidth)
    {
        float roll = Random.Range(0f, 1f);
        if (roll < 0.5f) // 50% spawn obstacle
        {
            ObstacleData obs = GetRandomObstacle();
            // Chỉ spawn nếu vật cản nhỏ gọn, vừa gầm cầu
            if (obs != null && obs.width < safeWidth + 1f)
            {
                float obsY = obs.prefab.transform.position.y;
                GameObject o = Instantiate(obs.prefab, new Vector3(x, y + obsY, 0), Quaternion.identity);
                RegisterObject(o);
            }
        }
        else if (roll < 0.8f) // 30% spawn item
        {
            SpawnItem(new Vector3(x, y + 0.5f, 0));
        }
    }

    private float SpawnObstacle(float x, float y, ObstacleData obs)
    {
        float prefabY = obs.prefab.transform.position.y;
        GameObject o = Instantiate(obs.prefab, new Vector3(x, y + prefabY, 0), Quaternion.identity);
        RegisterObject(o);

        if (Random.Range(0, 100) < itemOnObstacleChance)
        {
            float topY = y + obs.topHeightOffset;
            int count = Random.Range(obs.minItemsOnTop, obs.maxItemsOnTop + 1);
            float startItemX = x - ((count - 1) * itemSpacing) / 2;
            for (int k = 0; k < count; k++) SpawnItem(new Vector3(startItemX + k * itemSpacing, topY, 0));
        }
        return obs.width;
    }

    private float SpawnItemPattern(float x, float y)
    {
        ItemPattern p = (ItemPattern)Random.Range(0, System.Enum.GetValues(typeof(ItemPattern)).Length);
        List<Vector2> pts = new List<Vector2>();

        switch (p)
        {
            case ItemPattern.Line: int c = Random.Range(3, 6); for (int i = 0; i < c; i++) pts.Add(new Vector2(i, 0)); break;
            case ItemPattern.Grid: for (int gx = 0; gx < 3; gx++) for (int gy = 0; gy < 3; gy++) pts.Add(new Vector2(gx, gy - 1)); break;
            case ItemPattern.Wave: for (int i = 0; i < 8; i++) pts.Add(new Vector2(i, Mathf.Sin(i * 0.8f) * 1.5f)); break;
            case ItemPattern.ArrowComplex: pts.Add(new Vector2(0, 1.5f)); pts.Add(new Vector2(0, -1.5f)); pts.Add(new Vector2(1, 0.8f)); pts.Add(new Vector2(1, -0.8f)); pts.Add(new Vector2(2, 0)); break;
            case ItemPattern.Diamond: pts.Add(new Vector2(1, 1.5f)); pts.Add(new Vector2(1, -1.5f)); pts.Add(new Vector2(0, 0)); pts.Add(new Vector2(2, 0)); break;
            case ItemPattern.RectHollow: int rw = 4, rh = 3; for (int rx = 0; rx < rw; rx++) for (int ry = 0; ry < rh; ry++) if (rx == 0 || rx == rw - 1 || ry == 0 || ry == rh - 1) pts.Add(new Vector2(rx, ry - (rh - 1) / 2f)); break;
            case ItemPattern.RectVertical: int vw = Random.Range(2, 4); int vh = Random.Range(3, 6); for (int vx = 0; vx < vw; vx++) for (int vy = 0; vy < vh; vy++) if (vx == 0 || vx == vw - 1 || vy == 0 || vy == vh - 1) pts.Add(new Vector2(vx, vy - (vh - 1) / 2f)); break;
            case ItemPattern.RectHorizontal: int hw = Random.Range(3, 6); int hh = Random.Range(2, 4); for (int hx = 0; hx < hw; hx++) for (int hy = 0; hy < hh; hy++) if (hx == 0 || hx == hw - 1 || hy == 0 || hy == hh - 1) pts.Add(new Vector2(hx, hy - (hh - 1) / 2f)); break;
            case ItemPattern.ShapeVLU: pts.AddRange(GetTextPoints("V", 0)); pts.AddRange(GetTextPoints("L", 4)); pts.AddRange(GetTextPoints("U", 8)); break;
            case ItemPattern.ShapeAPlus: pts.AddRange(GetTextPoints("A", 0)); pts.AddRange(GetTextPoints("+", 4)); break;
            case ItemPattern.Triangle: pts.Add(new Vector2(0, 0)); pts.Add(new Vector2(1, 0)); pts.Add(new Vector2(1, 1)); pts.Add(new Vector2(2, 0)); pts.Add(new Vector2(2, 1)); pts.Add(new Vector2(2, 2)); break;
            case ItemPattern.StairsUp: for (int i = 0; i < 5; i++) pts.Add(new Vector2(i, i * 0.5f)); break;
            case ItemPattern.StairsDown: for (int i = 0; i < 5; i++) pts.Add(new Vector2(i, 2.5f - (i * 0.5f))); break;
            case ItemPattern.ZigZag: for (int i = 0; i < 6; i++) pts.Add(new Vector2(i, (i % 2 == 0) ? 0 : 1.5f)); break;
            case ItemPattern.DoubleLine: for (int i = 0; i < 5; i++) { pts.Add(new Vector2(i, 0)); pts.Add(new Vector2(i, 1.5f)); } break;
        }

        float currentBaseY = y + 1.0f;
        float lift = CalculateSmartLift(x, currentBaseY, pts);
        currentBaseY += lift;

        foreach (Vector2 pt in pts)
        {
            SpawnItem(new Vector3(x + pt.x * itemSpacing, currentBaseY + pt.y * itemSpacing, 0));
        }
        return 5f;
    }

    private float CalculateSmartLift(float x, float y, List<Vector2> pts)
    {
        float maxLift = 0;
        foreach (Vector2 pt in pts)
        {
            Vector2 check = new Vector2(x + pt.x, y + pt.y);
            Collider2D hit = Physics2D.OverlapCircle(check, checkRadius, obstacleLayer);
            if (hit)
            {
                float d = (hit.bounds.max.y + 1f) - check.y;
                if (d > maxLift) maxLift = d;
            }
        }
        return maxLift;
    }

    private void SpawnItem(Vector3 pos)
    {
        ItemData d = GetRandomItem();
        if (d != null)
        {
            GameObject i = Instantiate(d.prefab, pos, Quaternion.identity);
            RegisterObject(i);
            i.GetComponent<Collectible>()?.Init(d.scoreValue);
        }
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

    private void SyncColliderSize(GameObject obj, float targetLength)
    {
        BoxCollider2D col = obj.GetComponent<BoxCollider2D>();
        if (col != null)
        {
            float scaleX = obj.transform.localScale.x;
            if (scaleX == 0) scaleX = 1;
            Vector2 s = col.size; s.x = targetLength / scaleX; col.size = s;
            col.offset = new Vector2(0, col.offset.y);
        }
    }

    private ItemData GetRandomItem()
    {
        if (itemLibrary == null || itemLibrary.Count == 0) return null;
        float t = 0; foreach (var i in itemLibrary) t += i.spawnWeight;
        float r = Random.Range(0, t);
        float c = 0; foreach (var i in itemLibrary) { c += i.spawnWeight; if (r < c) return i; }
        return itemLibrary[0];
    }

    private ObstacleData GetRandomObstacle()
    {
        if (obstacleLibrary == null || obstacleLibrary.Count == 0) return null;
        return obstacleLibrary[Random.Range(0, obstacleLibrary.Count)];
    }

    private PlatformData GetRandomMiniPlatform()
    {
        if (miniPlatformLibrary == null || miniPlatformLibrary.Count == 0) return null;
        float t = 0; foreach (var p in miniPlatformLibrary) t += p.spawnWeight;
        float r = Random.Range(0, t);
        float c = 0; foreach (var p in miniPlatformLibrary) { c += p.spawnWeight; if (r < c) return p; }
        return miniPlatformLibrary[0];
    }

    private List<Vector2> GetTextPoints(string charType, int xOffset)
    {
        List<Vector2> pts = new List<Vector2>();
        switch (charType)
        {
            case "V": pts.Add(new Vector2(0, 2)); pts.Add(new Vector2(2, 2)); pts.Add(new Vector2(0, 1)); pts.Add(new Vector2(2, 1)); pts.Add(new Vector2(1, 0)); break;
            case "L": pts.Add(new Vector2(0, 2)); pts.Add(new Vector2(0, 1)); pts.Add(new Vector2(0, 0)); pts.Add(new Vector2(1, 0)); pts.Add(new Vector2(2, 0)); break;
            case "U": pts.Add(new Vector2(0, 2)); pts.Add(new Vector2(2, 2)); pts.Add(new Vector2(0, 1)); pts.Add(new Vector2(2, 1)); pts.Add(new Vector2(0, 0)); pts.Add(new Vector2(1, 0)); pts.Add(new Vector2(2, 0)); break;
            case "A": pts.Add(new Vector2(1, 2)); pts.Add(new Vector2(0, 1)); pts.Add(new Vector2(2, 1)); pts.Add(new Vector2(1, 1)); pts.Add(new Vector2(0, 0)); pts.Add(new Vector2(2, 0)); break;
            case "+": pts.Add(new Vector2(1, 2)); pts.Add(new Vector2(0, 1)); pts.Add(new Vector2(1, 1)); pts.Add(new Vector2(2, 1)); pts.Add(new Vector2(1, 0)); break;
        }
        for (int i = 0; i < pts.Count; i++) pts[i] = new Vector2(pts[i].x + xOffset, pts[i].y);
        return pts;
    }
}
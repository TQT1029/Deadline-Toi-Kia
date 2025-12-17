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
    public GameObject obstacleObjs;
    public GameObject itemObjs;

    [Header("Data Config")]
    public List<ObstacleData> obstacles;
    public List<ItemData> itemLibrary;

    [Header("Mini Platform Settings")]
    public List<PlatformData> miniPlatformLibrary;
    [Range(0, 100)] public int chanceToSpawnPlatformChain = 30;

    [Header("Chain Settings")]
    public int minChainLength = 3;
    public int maxChainLength = 5;
    public float platformHeightStep = 1.2f;
    public float maxPlatformHeight = 4.0f;

    [Header("Settings")]
    public float destroyDistanceBehind = 20f;
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;
    public float checkRadius = 0.4f;

    [Header("Spacing")]
    public float minGap = 6f;
    public float maxGap = 10f;
    [Range(0, 100)] public int chanceToSpawnObstacle = 40;
    [Range(0, 100)] public int chanceItemOnObstacle = 70;
    public float itemSpacing = 1.0f;

    private float currentSpawnX;
    private Queue<GameObject> activeObjects = new Queue<GameObject>();

    private enum ItemPattern
    {
        Line, Grid, Wave, ArrowComplex, Diamond, RectHollow,
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
            EndlessGameController.Instance.OnPlatformSpawned += OnMapGenerated;
        }

        if (playerTransform != null) currentSpawnX = playerTransform.position.x + 5f;
    }

    private void OnDestroy()
    {
        if (EndlessGameController.Instance != null)
        {
            EndlessGameController.Instance.OnPlatformSpawned -= OnMapGenerated;
        }
    }

    private void Update()
    {
        RemoveOldObjects();
    }

    private void OnMapGenerated(float startX, float endX, float platformY)
    {
        if (currentSpawnX < startX) currentSpawnX = startX;
        float safeEndX = endX - 2.0f;

        while (currentSpawnX < safeEndX)
        {
            SpawnSmartGroup(safeEndX, platformY);
        }
    }

    private void SpawnSmartGroup(float limitX, float currentY)
    {
        float gap = Random.Range(minGap, maxGap);
        currentSpawnX += gap;

        if (currentSpawnX > limitX) return;

        bool spawnObstacle = Random.Range(0, 100) < chanceToSpawnObstacle;
        float addedWidth = 0;

        if (spawnObstacle)
        {
            bool isChain = Random.Range(0, 100) < chanceToSpawnPlatformChain;

            if (isChain && miniPlatformLibrary != null && miniPlatformLibrary.Count > 0)
            {
                addedWidth = SpawnPlatformChain(currentSpawnX, currentY);
            }
            else
            {
                ObstacleData obs = GetRandomObstacle();
                if (obs != null && (currentSpawnX + obs.width) <= limitX)
                {
                    addedWidth = SpawnObstacleLogic(currentSpawnX, currentY, obs);
                }
                else
                {
                    addedWidth = SpawnItemPatternLogic(currentSpawnX, currentY);
                }
            }
        }
        else
        {
            addedWidth = SpawnItemPatternLogic(currentSpawnX, currentY);
        }

        currentSpawnX += addedWidth;
    }

    private float SpawnPlatformChain(float startX, float baseY)
    {
        int length = Random.Range(minChainLength, maxChainLength + 1);
        float currentHeight = 1.5f;
        float xStep = 3.5f;
        int patternType = Random.Range(0, 3);

        for (int i = 0; i < length; i++)
        {
            float posX = startX + (i * xStep);

            if (patternType == 0) currentHeight += platformHeightStep;
            else if (patternType == 2)
            {
                if (i % 2 == 0) currentHeight += platformHeightStep;
                else currentHeight -= (platformHeightStep * 0.5f);
            }
            currentHeight = Mathf.Clamp(currentHeight, 1.5f, maxPlatformHeight);

            PlatformData miniData = GetRandomMiniPlatformData();
            if (miniData != null && miniData.prefab != null)
            {
                Vector3 platPos = new Vector3(posX, baseY + currentHeight, 0);
                GameObject plat = Instantiate(miniData.prefab, platPos, Quaternion.identity);
                if (obstacleObjs != null) plat.transform.SetParent(obstacleObjs.transform);
                RegisterObject(plat, true);

                if (Random.value > 0.3f) SpawnItem(new Vector3(posX, baseY + currentHeight + 1.2f, 0));
            }

            // Tầng dưới: Spawn Obstacle hoặc Item
            float roll = Random.Range(0f, 1f);
            if (roll < 0.5f)
            {
                ObstacleData obs = GetRandomObstacle();
                if (obs != null && obs.width < xStep - 0.5f)
                {
                    float obsY = obs.prefab.transform.position.y;
                    GameObject obsObj = Instantiate(obs.prefab, new Vector3(posX, baseY + obsY, 0), Quaternion.identity);
                    RegisterObject(obsObj, true);
                }
            }
            else if (roll < 0.8f) SpawnItem(new Vector3(posX, baseY + 0.5f, 0));
        }
        return (length * xStep) + 2.0f;
    }

    private float SpawnObstacleLogic(float posX, float baseY, ObstacleData obsData)
    {
        float prefabY = obsData.prefab.transform.position.y;
        Vector3 spawnPos = new Vector3(posX, baseY + prefabY, 0);
        GameObject obsObj = Instantiate(obsData.prefab, spawnPos, Quaternion.identity);
        RegisterObject(obsObj, true);

        if (Random.Range(0, 100) < chanceItemOnObstacle)
        {
            float topY = spawnPos.y + obsData.topHeightOffset;
            int count = Random.Range(obsData.minItemsOnTop, obsData.maxItemsOnTop + 1);
            float startXItem = posX - ((count - 1) * itemSpacing) / 2;
            for (int i = 0; i < count; i++) SpawnItem(new Vector3(startXItem + (i * itemSpacing), topY, 0));
        }
        return obsData.width;
    }

    private float SpawnItemPatternLogic(float startX, float baseY)
    {
        ItemPattern pattern = (ItemPattern)Random.Range(0, System.Enum.GetValues(typeof(ItemPattern)).Length);
        List<Vector2> localPoints = new List<Vector2>();

        switch (pattern)
        {
            case ItemPattern.Line: int c = Random.Range(3, 6); for (int i = 0; i < c; i++) localPoints.Add(new Vector2(i, 0)); break;
            case ItemPattern.Grid: for (int x = 0; x < 3; x++) for (int y = 0; y < 3; y++) localPoints.Add(new Vector2(x, y - 1)); break;
            case ItemPattern.Wave: for (int i = 0; i < 8; i++) localPoints.Add(new Vector2(i, Mathf.Sin(i * 0.8f) * 1.5f)); break;
            case ItemPattern.ArrowComplex: localPoints.Add(new Vector2(0, 1.5f)); localPoints.Add(new Vector2(0, -1.5f)); localPoints.Add(new Vector2(1, 0.8f)); localPoints.Add(new Vector2(1, -0.8f)); localPoints.Add(new Vector2(2, 0)); break;
            case ItemPattern.Diamond: localPoints.Add(new Vector2(1, 1.5f)); localPoints.Add(new Vector2(1, -1.5f)); localPoints.Add(new Vector2(0, 0)); localPoints.Add(new Vector2(2, 0)); break;
            case ItemPattern.RectHollow: int rw = 4, rh = 3; for (int x = 0; x < rw; x++) for (int y = 0; y < rh; y++) if (x == 0 || x == rw - 1 || y == 0 || y == rh - 1) localPoints.Add(new Vector2(x, y - (rh - 1) / 2f)); break;
            case ItemPattern.ShapeVLU: localPoints.AddRange(GetTextPoints("V", 0)); localPoints.AddRange(GetTextPoints("L", 4)); localPoints.AddRange(GetTextPoints("U", 8)); break;
            case ItemPattern.ShapeAPlus: localPoints.AddRange(GetTextPoints("A", 0)); localPoints.AddRange(GetTextPoints("+", 4)); break;
            case ItemPattern.Triangle: localPoints.Add(new Vector2(0, 0)); localPoints.Add(new Vector2(1, 0)); localPoints.Add(new Vector2(1, 1)); localPoints.Add(new Vector2(2, 0)); localPoints.Add(new Vector2(2, 1)); localPoints.Add(new Vector2(2, 2)); break;
            case ItemPattern.StairsUp: for (int i = 0; i < 5; i++) localPoints.Add(new Vector2(i, i * 0.5f)); break;
            case ItemPattern.StairsDown: for (int i = 0; i < 5; i++) localPoints.Add(new Vector2(i, 2.5f - (i * 0.5f))); break;
            case ItemPattern.ZigZag: for (int i = 0; i < 6; i++) localPoints.Add(new Vector2(i, (i % 2 == 0) ? 0 : 1.5f)); break;
            case ItemPattern.DoubleLine: for (int i = 0; i < 5; i++) { localPoints.Add(new Vector2(i, 0)); localPoints.Add(new Vector2(i, 1.5f)); } break;
        }

        float currentBaseY = baseY + 1.0f;
        float liftOffset = CalculateSmartLift(startX, currentBaseY, localPoints);
        currentBaseY += liftOffset;

        foreach (Vector2 p in localPoints)
        {
            SpawnItem(new Vector3(startX + (p.x * itemSpacing), currentBaseY + (p.y * itemSpacing), 0));
        }
        return 5f + (localPoints.Count > 0 ? localPoints[localPoints.Count - 1].x : 0);
    }

    private float CalculateSmartLift(float startX, float baseY, List<Vector2> points)
    {
        float maxLift = 0f;
        foreach (Vector2 p in points)
        {
            Vector2 checkPos = new Vector2(startX + (p.x * itemSpacing), baseY + (p.y * itemSpacing));
            Collider2D hit = Physics2D.OverlapCircle(checkPos, checkRadius, obstacleLayer);
            if (hit != null)
            {
                float diff = (hit.bounds.max.y + 1.0f) - checkPos.y;
                if (diff > maxLift) maxLift = diff;
            }
        }
        return maxLift;
    }

    private void SpawnItem(Vector3 pos)
    {
        ItemData data = GetRandomItemData();
        if (data != null)
        {
            GameObject item = Instantiate(data.prefab, pos, Quaternion.identity);
            RegisterObject(item, false);
            item.GetComponent<Collectible>()?.Init(data.scoreValue);
        }
    }

    private void RegisterObject(GameObject obj, bool isObs)
    {
        activeObjects.Enqueue(obj);
        Transform p = isObs ? (obstacleObjs ? obstacleObjs.transform : transform) : (itemObjs ? itemObjs.transform : transform);
        obj.transform.SetParent(p);
    }

    private void RemoveOldObjects()
    {
        if (activeObjects.Count > 0)
        {
            GameObject obj = activeObjects.Peek();
            if (obj == null) { activeObjects.Dequeue(); return; }
            if (playerTransform.position.x - obj.transform.position.x > destroyDistanceBehind)
                Destroy(activeObjects.Dequeue());
        }
    }

    private ItemData GetRandomItemData()
    {
        if (itemLibrary == null || itemLibrary.Count == 0) return null;
        float t = 0; foreach (var i in itemLibrary) t += i.spawnWeight;
        float r = Random.Range(0, t);
        float c = 0;
        foreach (var i in itemLibrary) { c += i.spawnWeight; if (r < c) return i; }
        return itemLibrary[0];
    }

    private ObstacleData GetRandomObstacle()
    {
        if (obstacles == null || obstacles.Count == 0) return null;
        return obstacles[Random.Range(0, obstacles.Count)];
    }

    private PlatformData GetRandomMiniPlatformData()
    {
        if (miniPlatformLibrary == null || miniPlatformLibrary.Count == 0) return null;
        float t = 0; foreach (var p in miniPlatformLibrary) t += p.spawnWeight;
        float r = Random.Range(0, t);
        float c = 0;
        foreach (var p in miniPlatformLibrary) { c += p.spawnWeight; if (r < c) return p; }
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
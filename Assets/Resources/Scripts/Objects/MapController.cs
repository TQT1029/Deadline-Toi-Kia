using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MapController : MonoBehaviour
{
    public static MapController Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Header("Data Config")]
    public List<ObstacleData> obstacles;
    public List<ItemData> itemLibrary;

    [Header("Map Settings")]
    public Transform startPoint;
    public Transform endPoint;
    public float groundY = -2f;
    public GameObject obstacleObjs;
    public GameObject itemObjs;

    [Header("Physics Settings")]
    public LayerMask obstacleLayer;
    public float checkRadius = 0.4f;

    [Header("Spacing Settings")]
    [Tooltip("Khoảng cách tối thiểu và tối đa giữa các phần tử trên map")]
    public float minGap = 6f;
    public float maxGap = 10f;

    [Header("Logic")]
    [Range(0, 100)] public int chanceToSpawnObstacle = 40;
    [Range(0, 100)] public int chanceItemOnObstacle = 70;
    public float itemSpacing = 1.0f;

    private enum ItemPattern { Line, Grid, Wave, ArrowComplex, Diamond, RectHollow, ShapeVLU, ShapeAPlus, RectVertical, RectHorizontal }

    [ContextMenu("Generate Level")]
    public async void GenerateLevel()
    {
        ClearMap();
        if (startPoint == null || endPoint == null) return;

        float currentX = startPoint.position.x;
        float endX = endPoint.position.x;
        int elementCount = 0;

        while (currentX < endX)
        {
            float gap = Random.Range(minGap, maxGap);
            currentX += gap;

            if (currentX >= endX) break;

            bool spawnObstacle = Random.Range(0, 100) < chanceToSpawnObstacle;

            if (spawnObstacle)
            {
                float addedWidth = SpawnObstacleLogic(currentX);
                currentX += addedWidth;
            }
            else
            {
                float addedWidth = SpawnItemPatternLogic(currentX);
                currentX += addedWidth;
            }

            elementCount++;
            if (elementCount % 5 == 0) await Task.Yield();
        }
        Debug.Log("Hoàn tất tạo màn chơi!");
    }

    // --- LOGIC 1: SPAWN OBSTACLE ---
    private float SpawnObstacleLogic(float posX)
    {
        if (obstacles == null || obstacles.Count == 0) return 0;
        ObstacleData obsData = obstacles[Random.Range(0, obstacles.Count)];

        float prefabY = obsData.prefab.transform.position.y;
        Vector3 spawnPos = new Vector3(posX, groundY + prefabY, 0);

        GameObject obsObj = Instantiate(obsData.prefab, spawnPos, Quaternion.identity);

        if (obstacleObjs != null) obsObj.transform.SetParent(obstacleObjs.transform);
        else obsObj.transform.SetParent(this.transform);

        if (Random.Range(0, 100) < chanceItemOnObstacle)
        {
            float topY = spawnPos.y + obsData.topHeightOffset;
            int count = Random.Range(obsData.minItemsOnTop, obsData.maxItemsOnTop + 1);
            float startXItem = posX - ((count - 1) * itemSpacing) / 2;

            for (int i = 0; i < count; i++)
            {
                SpawnItem(new Vector3(startXItem + (i * itemSpacing), topY, 0));
            }
        }

        return obsData.width;
    }

    // --- LOGIC 2: SPAWN ITEM PATTERNS ---
    private float SpawnItemPatternLogic(float startX)
    {
        ItemPattern pattern = (ItemPattern)Random.Range(0, System.Enum.GetValues(typeof(ItemPattern)).Length);

        List<Vector2> localPoints = new List<Vector2>();
        float patternWidth = 0;

        switch (pattern)
        {
            case ItemPattern.ShapeVLU:
                localPoints.AddRange(GetTextPoints("V", 0));
                localPoints.AddRange(GetTextPoints("L", 4));
                localPoints.AddRange(GetTextPoints("U", 8));
                patternWidth = 11 * itemSpacing;
                break;

            case ItemPattern.ShapeAPlus:
                localPoints.AddRange(GetTextPoints("A", 0));
                localPoints.AddRange(GetTextPoints("+", 4));
                patternWidth = 7 * itemSpacing;
                break;

            case ItemPattern.Line:
                int c = Random.Range(3, 6);
                for (int i = 0; i < c; i++) localPoints.Add(new Vector2(i, 0));
                patternWidth = c * itemSpacing;
                break;

            case ItemPattern.Grid:
                for (int x = 0; x < 3; x++)
                    for (int y = 0; y < 3; y++) localPoints.Add(new Vector2(x, y - 1));
                patternWidth = 3 * itemSpacing;
                break;

            case ItemPattern.Wave:
                for (int i = 0; i < 8; i++)
                    localPoints.Add(new Vector2(i, Mathf.Sin(i * 0.8f) * 1.5f));
                patternWidth = 8 * itemSpacing;
                break;

            case ItemPattern.ArrowComplex:
                localPoints.Add(new Vector2(0, 1.5f)); localPoints.Add(new Vector2(0, -1.5f));
                localPoints.Add(new Vector2(1, 0.8f)); localPoints.Add(new Vector2(1, -0.8f));
                localPoints.Add(new Vector2(2, 0));
                patternWidth = 3 * itemSpacing;
                break;

            case ItemPattern.Diamond:
                localPoints.Add(new Vector2(1, 1.5f)); localPoints.Add(new Vector2(1, -1.5f));
                localPoints.Add(new Vector2(0, 0)); localPoints.Add(new Vector2(2, 0));
                patternWidth = 3 * itemSpacing;
                break;

            case ItemPattern.RectHollow:
                int rw = 4, rh = 3;
                for (int x = 0; x < rw; x++)
                    for (int y = 0; y < rh; y++)
                        if (x == 0 || x == rw - 1 || y == 0 || y == rh - 1)
                            localPoints.Add(new Vector2(x, y - (rh - 1) / 2f));
                patternWidth = rw * itemSpacing;
                break;

            case ItemPattern.RectVertical:
                int vw = Random.Range(2, 4);
                int vh = Random.Range(3, 6);
                for (int x = 0; x < vw; x++)
                    for (int y = 0; y < vh; y++)
                        if (x == 0 || x == vw - 1 || y == 0 || y == vh - 1)
                            localPoints.Add(new Vector2(x, y - (vh - 1) / 2f));
                patternWidth = vw * itemSpacing;
                break;

            case ItemPattern.RectHorizontal:
                int hw = Random.Range(3, 6);
                int hh = Random.Range(2, 4);
                for (int x = 0; x < hw; x++)
                    for (int y = 0; y < hh; y++)
                        if (x == 0 || x == hw - 1 || y == 0 || y == hh - 1)
                            localPoints.Add(new Vector2(x, y - (hh - 1) / 2f));
                patternWidth = hw * itemSpacing;
                break;
        }

        float currentBaseY = groundY + 1.5f;
        float liftOffset = CalculateSmartLift(startX, currentBaseY, localPoints);
        currentBaseY += liftOffset;

        foreach (Vector2 p in localPoints)
        {
            SpawnItem(new Vector3(startX + (p.x * itemSpacing), currentBaseY + (p.y * itemSpacing), 0));
        }

        return patternWidth;
    }

    private float CalculateSmartLift(float startX, float baseY, List<Vector2> points)
    {
        float maxLiftNeeded = 0f;
        foreach (Vector2 p in points)
        {
            Vector2 checkPos = new Vector2(startX + (p.x * itemSpacing), baseY + (p.y * itemSpacing));
            Collider2D hit = Physics2D.OverlapCircle(checkPos, checkRadius, obstacleLayer);
            if (hit != null)
            {
                float diff = (hit.bounds.max.y + 1.5f) - checkPos.y;
                if (diff > maxLiftNeeded) maxLiftNeeded = diff;
            }
        }
        return maxLiftNeeded;
    }

    private List<Vector2> GetTextPoints(string charType, int xOffset)
    {
        List<Vector2> pts = new List<Vector2>();
        switch (charType)
        {
            case "V":
                pts.Add(new Vector2(0, 2)); pts.Add(new Vector2(2, 2));
                pts.Add(new Vector2(0, 1)); pts.Add(new Vector2(2, 1));
                pts.Add(new Vector2(1, 0));
                break;
            case "L":
                pts.Add(new Vector2(0, 2)); pts.Add(new Vector2(0, 1));
                pts.Add(new Vector2(0, 0)); pts.Add(new Vector2(1, 0)); pts.Add(new Vector2(2, 0));
                break;
            case "U":
                pts.Add(new Vector2(0, 2)); pts.Add(new Vector2(2, 2));
                pts.Add(new Vector2(0, 1)); pts.Add(new Vector2(2, 1));
                pts.Add(new Vector2(0, 0)); pts.Add(new Vector2(1, 0)); pts.Add(new Vector2(2, 0));
                break;
            case "A":
                pts.Add(new Vector2(1, 2));
                pts.Add(new Vector2(0, 1)); pts.Add(new Vector2(2, 1)); pts.Add(new Vector2(1, 1));
                pts.Add(new Vector2(0, 0)); pts.Add(new Vector2(2, 0));
                break;
            case "+":
                pts.Add(new Vector2(1, 2));
                pts.Add(new Vector2(0, 1)); pts.Add(new Vector2(1, 1)); pts.Add(new Vector2(2, 1));
                pts.Add(new Vector2(1, 0));
                break;
        }
        for (int i = 0; i < pts.Count; i++) pts[i] = new Vector2(pts[i].x + xOffset, pts[i].y);
        return pts;
    }

    private void SpawnItem(Vector3 pos)
    {
        if (pos.y < groundY + 0.5f) pos.y = groundY + 0.5f;

        ItemData data = GetRandomItemData();
        if (data != null && data.prefab != null)
        {
            GameObject item = Instantiate(data.prefab, pos, Quaternion.identity);
            if (itemObjs != null) item.transform.SetParent(itemObjs.transform);
            else item.transform.SetParent(this.transform);

            Collectible col = item.GetComponent<Collectible>();
            if (col != null) col.Init(data.scoreValue);
        }
    }

    // --- CẬP NHẬT LOGIC RANDOM FLOAT ---
    private ItemData GetRandomItemData()
    {
        if (itemLibrary == null || itemLibrary.Count == 0) return null;

        // Tính tổng trọng số (dùng float)
        float totalWeight = 0f;
        foreach (var item in itemLibrary) totalWeight += item.spawnWeight;

        // Random trong khoảng float
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var item in itemLibrary)
        {
            currentWeight += item.spawnWeight;
            if (randomValue < currentWeight) return item;
        }
        return itemLibrary[0];
    }

    private void ClearMap()
    {
        if (obstacleObjs) ClearChildren(obstacleObjs.transform);
        if (itemObjs) ClearChildren(itemObjs.transform);
        if (!obstacleObjs && !itemObjs) ClearChildren(transform);
    }

    private void ClearChildren(Transform p)
    {
        if (!p) return;
        for (int i = p.childCount - 1; i >= 0; i--)
        {
            GameObject c = p.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(c);
            else DestroyImmediate(c);
        }
    }
}
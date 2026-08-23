using System.Collections.Generic;
using UnityEngine;
using ProObstacleEngine;

public class ItemGenerator : MonoBehaviour
{
    public static ItemGenerator Instance;
    private void Awake() => Instance = this;
            
    [System.Serializable]
    public class BakedPattern
    {
        public string id;
        public Vector2 size;
        public List<Vector2> relativePoints = new List<Vector2>();
    }

    [Header("References")]
    public Transform itemContainer; // Container chua item tren map

    [Tooltip("Item Prefab (Coin/Star...)")]
    [SerializeField] private List<ItemData> commonItems = new List<ItemData>();

    // Danh sach cac mau pattern da duoc bake toa do tuong doi
    private readonly List<BakedPattern> bakedPatterns = new List<BakedPattern>();

    [Header("Settings")]
    [SerializeField] private LayerMask surfaceLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float itemSpacing = 1.0f;
    [SerializeField] private float patternPadding = 1.0f;
    [SerializeField] private float groundPadding = 1.0f;

    [SerializeField] private float minGap = 5.0f;
    [SerializeField] private float maxGap = 7.0f;

    [Space]
    [Tooltip("Xac suat spawn item tren vat can (%)")]
    [SerializeField, Range(0, 100)] private float obstacleChanceItems = 50f;
    [Tooltip("Xac suat spawn item tren san bay (%)")]
    [SerializeField, Range(0, 100)] private float platformChanceItems = 70f;

    [Header("Runtime Pacing")]
    public bool IsGenerationEnabled { get; set; } = true;
    public float DensityMultiplier { get; set; } = 1.0f;

    // ENUM
    public enum ItemPattern
    {
        Line,
        Grid,
        Wave,
        Diamond,
        RectHollow,
        RectVertical,
        RectHorizontal,
        ShapeVLU,
        ShapeAPlus,
        Triangle,
        StairsUp,
        StairsDown,
        ZigZag,
        DoubleLine,
        Cross,
        XShape,
        Pyramid,
        Checker,
        Spiral
    }

    private void Start()
    {
        BakePatterns();
    }

    public void ApplyConfig(MapProfile profile)
    {
        if (profile == null) return;
        if (profile.commonItems != null && profile.commonItems.Count > 0)
        {
            commonItems = profile.commonItems;
        }
        obstacleChanceItems = profile.obstacleChanceItems;
        platformChanceItems = profile.platformChanceItems;
        itemSpacing = profile.itemSpacing;
        patternPadding = profile.patternPadding;
        groundPadding = profile.groundPadding;
        minGap = profile.minGap;
        maxGap = profile.maxGap;

        BakePatterns();
    }

    public void Prewarm(int countPerPrefab = 20)
    {
        if (commonItems == null) return;
        foreach (var item in commonItems)
        {
            if (item != null && item.prefab != null)
            {
                GameObjectPool.Prewarm(item.prefab, countPerPrefab, itemContainer);
            }
        }
    }

    // --- GENERATE LOGIC (RUNTIME) ---

    public void GenerateItems(float startX, float endX)
    {
        if (!IsGenerationEnabled || DensityMultiplier <= 0f) return;
        if (commonItems == null || commonItems.Count == 0) return;

        float currentX = startX + RandomUtils.RandomWithSteps(2f, 4f);
        int safetyLoop = 0;

        while (currentX < endX - 10f)
        {
            if (safetyLoop++ > 1000) { Debug.LogWarning("Safety Break!"); break; }

            RaycastHit2D hit = GameUtils.GetSurfaceHit(currentX, surfaceLayer);

            if (hit.collider != null)
            {
                GameObject obj = GameUtils.GetObstacleRoot(hit.collider.transform);

                if (obj.CompareTag("Obstacle"))
                {
                    Bounds obsBounds = GameUtils.GetBounds(obj);
                    ObstacleMotionControl dynamicObsController = obj.GetComponent<ObstacleMotionControl>();

                    if (RandomUtils.ChancePercent(obstacleChanceItems))
                    {
                        if (dynamicObsController == null)
                            currentX = SpawnOnTop(obsBounds);
                        else
                        {
                            if (dynamicObsController.enableMove && dynamicObsController.moveOffset.y >= 5)
                                currentX = SpawnOnBottom(obsBounds);
                        }
                    }
                    else
                    {
                        currentX += 2f;
                    }
                }
                else if (obj.CompareTag("MiniPlatform"))
                {
                    Bounds obsBounds = GameUtils.GetBounds(obj);

                    if (RandomUtils.ChancePercent(platformChanceItems))
                    {
                        currentX = SpawnOnTop(obsBounds);
                    }
                    else
                    {
                        currentX += 2f;
                    }
                }
                else
                {
                    BakedPattern template = GetRandomTemplate();
                    if (template == null) break;

                    float spawnY = hit.point.y + groundPadding + (template.size.y / 2f);
                    Vector2 centerPos = new Vector2(currentX + (template.size.x / 2f), spawnY);

                    if (CheckFits(centerPos, template.size))
                    {
                        SpawnPattern(template, centerPos);
                        currentX += template.size.x + patternPadding + RandomUtils.RandomWithSteps(minGap, maxGap);
                    }
                    else
                    {
                        currentX += 1.0f;
                    }
                }
            }
            else
            {
                currentX += 2f;
            }
        }
    }

    private bool CheckFits(Vector2 centerPos, Vector2 size)
    {
        Collider2D hit = Physics2D.OverlapBox(centerPos, new Vector2(size.x * 1.1f, size.y * 0.9f), 0f, obstacleLayer);
        return hit == null;
    }

    private float SpawnOnTop(Bounds hitBounds)
    {
        BakedPattern template = GetRandomTemplate();
        if (template == null) return hitBounds.max.x + patternPadding;

        float spawnY = hitBounds.max.y + groundPadding + (template.size.y / 2f);
        Vector2 centerPos = new Vector2(hitBounds.center.x, spawnY);

        SpawnPattern(template, centerPos);
        return hitBounds.max.x + patternPadding + RandomUtils.RandomWithSteps(minGap, maxGap);
    }

    private float SpawnOnBottom(Bounds hitBounds)
    {
        BakedPattern template = GetRandomTemplate();
        if (template == null) return hitBounds.max.x + patternPadding;

        float spawnY = hitBounds.min.y + groundPadding + (template.size.y / 2f);
        Vector2 centerPos = new Vector2(hitBounds.center.x, spawnY);

        SpawnPattern(template, centerPos);
        return hitBounds.max.x + patternPadding + RandomUtils.RandomWithSteps(minGap, maxGap);
    }

    private void SpawnPattern(BakedPattern template, Vector2 centerPosition)
    {
        if (template == null || commonItems == null || commonItems.Count == 0) return;

        foreach (Vector2 relPt in template.relativePoints)
        {
            Vector3 worldPos = new Vector3(centerPosition.x + relPt.x, centerPosition.y + relPt.y, 0f);

            GameObject chosenPrefab = commonItems[0].prefab;
            if (commonItems.Count > 1)
            {
                for (int i = 1; i < commonItems.Count; i++)
                {
                    if (commonItems[i] != null && commonItems[i].prefab != null && RandomUtils.ChancePercent(commonItems[i].spawnChance))
                    {
                        chosenPrefab = commonItems[i].prefab;
                        break;
                    }
                }
            }

            if (chosenPrefab != null)
            {
                GameObjectPool.Get(chosenPrefab, worldPos, Quaternion.identity, itemContainer);
            }
        }
    }

    // --- PRE-BAKE LOGIC (SETUP) ---

    private void BakePatterns()
    {
        bakedPatterns.Clear();

        foreach (ItemPattern p in System.Enum.GetValues(typeof(ItemPattern)))
        {
            List<Vector2> points = Generatepts(p);
            if (points == null || points.Count == 0) continue;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            foreach (var pt in points)
            {
                float px = pt.x * itemSpacing;
                float py = pt.y * itemSpacing;
                if (px < minX) minX = px;
                if (px > maxX) maxX = px;
                if (py < minY) minY = py;
                if (py > maxY) maxY = py;
            }

            Vector2 size = new Vector2(maxX - minX + itemSpacing, maxY - minY + itemSpacing);
            Vector2 center = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);

            BakedPattern pattern = new BakedPattern
            {
                id = p.ToString(),
                size = size
            };

            foreach (var pt in points)
            {
                pattern.relativePoints.Add(new Vector2(pt.x * itemSpacing - center.x, pt.y * itemSpacing - center.y));
            }

            bakedPatterns.Add(pattern);
        }

        Debug.Log($"[ItemGenerator] Baked {bakedPatterns.Count} patterns successfully with 0 runtime GC.");
    }

    private List<Vector2> Generatepts(ItemPattern p)
    {
        List<Vector2> pts = new List<Vector2>();
        switch (p)
        {
            case ItemPattern.Line:
                int c = 6;
                for (int i = 0; i < c; i++)
                    pts.Add(new Vector2(i, 0));
                break;

            case ItemPattern.Grid:
                for (int x = 0; x < 3; x++)
                    for (int y = 0; y < 3; y++)
                        pts.Add(new Vector2(x, y));
                break;

            case ItemPattern.Wave:
                for (int i = 0; i < 8; i++)
                    pts.Add(new Vector2(i, Mathf.Sin(i * 0.8f) * 1.5f + 1.5f));
                break;

            case ItemPattern.Diamond:
                pts.Add(new Vector2(1, 2));
                pts.Add(new Vector2(0, 1));
                pts.Add(new Vector2(2, 1));
                pts.Add(new Vector2(1, 0));
                break;

            case ItemPattern.RectHollow:
                int rw = 4, rh = 3;
                for (int rx = 0; rx < rw; rx++)
                    for (int ry = 0; ry < rh; ry++)
                        if (rx == 0 || rx == rw - 1 || ry == 0 || ry == rh - 1)
                            pts.Add(new Vector2(rx, ry));
                break;

            case ItemPattern.RectVertical:
                int vw = 4;
                int vh = 5;
                for (int vx = 0; vx < vw; vx++)
                    for (int vy = 0; vy < vh; vy++)
                        pts.Add(new Vector2(vx, vy));
                break;

            case ItemPattern.RectHorizontal:
                int hw = 6;
                int hh = 4;
                for (int hx = 0; hx < hw; hx++)
                    for (int hy = 0; hy < hh; hy++)
                        pts.Add(new Vector2(hx, hy));
                break;

            case ItemPattern.ShapeVLU:
                pts.AddRange(GetTextPoints("V", 0));
                pts.AddRange(GetTextPoints("L", 4));
                pts.AddRange(GetTextPoints("U", 8));
                break;

            case ItemPattern.ShapeAPlus:
                pts.AddRange(GetTextPoints("A", 0));
                pts.AddRange(GetTextPoints("+", 4));
                break;

            case ItemPattern.Triangle:
                for (int y = 0; y < 3; y++)
                    for (int x = 0; x <= y; x++)
                        pts.Add(new Vector2(y, x));
                break;

            case ItemPattern.StairsUp:
                for (int i = 0; i < 5; i++)
                    pts.Add(new Vector2(i, i * 0.5f));
                break;

            case ItemPattern.StairsDown:
                for (int i = 0; i < 5; i++)
                    pts.Add(new Vector2(i, 2.5f - (i * 0.5f)));
                break;

            case ItemPattern.ZigZag:
                for (int i = 0; i < 6; i++)
                    pts.Add(new Vector2(i, (i % 2 == 0) ? 0 : 1.5f));
                break;

            case ItemPattern.DoubleLine:
                for (int i = 0; i < 5; i++)
                {
                    pts.Add(new Vector2(i, 0));
                    pts.Add(new Vector2(i, 1.5f));
                }
                break;

            case ItemPattern.Cross:
                for (int i = -2; i <= 2; i++)
                {
                    if (i != 0)
                        pts.Add(new Vector2(0, i));
                    pts.Add(new Vector2(i, 0));
                }
                break;

            case ItemPattern.XShape:
                for (int i = 0; i < 5; i++)
                {
                    pts.Add(new Vector2(i, i));
                    pts.Add(new Vector2(i, 4 - i));
                }
                break;

            case ItemPattern.Pyramid:
                for (int y = 0; y < 4; y++)
                    for (int x = -y; x <= y; x++)
                        pts.Add(new Vector2(x + 3, y));
                break;

            case ItemPattern.Checker:
                for (int x = 0; x < 6; x++)
                    for (int y = 0; y < 6; y++)
                        if ((x + y) % 2 == 0)
                            pts.Add(new Vector2(x, y));
                break;

            case ItemPattern.Spiral:
                int size = 5;
                int minX = 0, minY = 0, maxX = size - 1, maxY = size - 1;
                while (minX <= maxX && minY <= maxY)
                {
                    for (int x = minX; x <= maxX; x++) pts.Add(new Vector2(x, minY));
                    minY++;
                    for (int y = minY; y <= maxY; y++) pts.Add(new Vector2(maxX, y));
                    maxX--;
                    for (int x = maxX; x >= minX; x--) pts.Add(new Vector2(x, maxY));
                    maxY--;
                    for (int y = maxY; y >= minY; y--) pts.Add(new Vector2(minX, y));
                    minX++;
                }
                break;
        }
        return pts;
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

    private BakedPattern GetRandomTemplate()
    {
        if (bakedPatterns.Count == 0) return null;
        return bakedPatterns[Random.Range(0, bakedPatterns.Count)];
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
    }
#endif
}
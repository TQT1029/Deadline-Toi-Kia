using UnityEngine;
using System.Collections.Generic;

public class ItemGenerator : MonoBehaviour
{
    public static ItemGenerator Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Header("--- REFERENCES ---")]
    public Transform playerTransform;
    public GameObject itemContainer;

    [Header("--- CONFIG ---")]
    [Tooltip("Danh sách Item (Coin, Powerup...)")]
    public List<ItemData> itemLibrary;
    [Tooltip("Layer vật cản để tránh spawn trùng")]
    public LayerMask obstacleLayer;
    [Tooltip("Bán kính kiểm tra trùng vật cản")]
    public float checkRadius = 0.25f;
    [Tooltip("Khoảng cách giữa các item khi xếp hàng")]
    public float itemSpacing = 1.0f;
    public float destroyDistanceBehind = 20f;

    private Queue<GameObject> activeItems = new Queue<GameObject>();

    // Các Pattern lớn cho khoảng trống
    private enum ItemPattern
    {
        Line, Grid, Wave, ArrowComplex, Diamond, RectHollow, RectVertical, RectHorizontal,
        ShapeVLU, ShapeAPlus, Triangle, StairsUp, StairsDown, ZigZag, DoubleLine
    }

    private void Start()
    {
        if (ObstacleGenerator.Instance != null)
        {
            ObstacleGenerator.Instance.OnRequestSingleItem += SpawnSingleItem;
            ObstacleGenerator.Instance.OnRequestItemRow += SpawnItemRow;
            ObstacleGenerator.Instance.OnRequestItemPattern += SpawnPatternInGap;

            // Đăng ký thêm sự kiện mới cho Mini Platform
            ObstacleGenerator.Instance.OnRequestItemOnPlatform += SpawnItemOnPlatform;
        }
    }

    private void OnDestroy()
    {
        if (ObstacleGenerator.Instance != null)
        {
            ObstacleGenerator.Instance.OnRequestSingleItem -= SpawnSingleItem;
            ObstacleGenerator.Instance.OnRequestItemRow -= SpawnItemRow;
            ObstacleGenerator.Instance.OnRequestItemPattern -= SpawnPatternInGap;
            ObstacleGenerator.Instance.OnRequestItemOnPlatform -= SpawnItemOnPlatform;
        }
    }

    private void Update()
    {
        RemoveOldItems();
    }

    // --- EVENT HANDLERS ---

    private void SpawnSingleItem(Vector3 pos)
    {
        if (IsPositionClear(pos)) SpawnItem(pos);
    }

    // Spawn dạng hàng thẳng (Dùng cho dưới gầm hoặc nóc obstacle thường)
    private void SpawnItemRow(Vector3 centerPos, int count, float widthAvailable)
    {
        if (count <= 0) return;

        float totalWidth = (count - 1) * itemSpacing;
        // Co kéo số lượng nếu không đủ chỗ
        if (totalWidth > widthAvailable)
        {
            count = Mathf.FloorToInt(widthAvailable / itemSpacing);
            totalWidth = (count - 1) * itemSpacing;
        }

        float startX = centerPos.x - (totalWidth / 2f);

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(startX + (i * itemSpacing), centerPos.y, 0);
            if (IsPositionClear(pos)) SpawnItem(pos);
        }
    }

    // [MỚI] Spawn Item trên sàn bay với sự biến tấu (Cung, Zigzag...)
    private void SpawnItemOnPlatform(Vector3 centerPos, float platformLength)
    {
        // Tính số lượng item tối đa có thể nhét vào
        int maxItems = Mathf.FloorToInt(platformLength - 1.0f);
        if (maxItems < 1) maxItems = 1;

        // Random kiểu spawn:
        // 0-50%: Đường thẳng (Line) - Gọn gàng
        // 50-80%: Cung nhỏ (Arch) - Chỉ nếu sàn đủ dài (>3 item)
        // 80-100%: ZigZag nhỏ - Chỉ nếu sàn đủ dài (>3 item)
        float roll = Random.value;

        if (roll < 0.5f || maxItems < 3)
        {
            // Spawn đường thẳng truyền thống
            SpawnItemRow(centerPos, maxItems, platformLength);
        }
        else if (roll < 0.8f)
        {
            // Spawn hình cung nhỏ (Arch)
            SpawnMiniArch(centerPos, maxItems);
        }
        else
        {
            // Spawn Zigzag
            SpawnMiniZigZag(centerPos, maxItems);
        }
    }

    private void SpawnMiniArch(Vector3 center, int count)
    {
        float totalWidth = (count - 1) * itemSpacing;
        float startX = center.x - (totalWidth / 2f);
        float archHeight = 1.0f; // Độ cao đỉnh cung

        for (int i = 0; i < count; i++)
        {
            // Công thức Parabol: y = 4 * h * x * (1-x) (x đi từ 0 đến 1)
            float t = (float)i / (count - 1);
            float yOffset = 4 * archHeight * t * (1 - t);

            Vector3 pos = new Vector3(startX + i * itemSpacing, center.y + yOffset, 0);
            if (IsPositionClear(pos)) SpawnItem(pos);
        }
    }

    private void SpawnMiniZigZag(Vector3 center, int count)
    {
        float totalWidth = (count - 1) * itemSpacing;
        float startX = center.x - (totalWidth / 2f);

        for (int i = 0; i < count; i++)
        {
            // So le cao thấp: 0 -> 0.8 -> 0 -> 0.8
            float yOffset = (i % 2 == 0) ? 0 : 0.8f;
            Vector3 pos = new Vector3(startX + i * itemSpacing, center.y + yOffset, 0);
            if (IsPositionClear(pos)) SpawnItem(pos);
        }
    }

    // --- PATTERN LỚN TRONG KHOẢNG TRỐNG ---
    private void SpawnPatternInGap(float startX, float endX, float baseHeight)
    {
        float width = endX - startX;
        if (width < 2.0f) return;

        // Chọn pattern ngẫu nhiên
        ItemPattern p = (ItemPattern)Random.Range(0, System.Enum.GetValues(typeof(ItemPattern)).Length);
        List<Vector2> localPoints = new List<Vector2>();

        // [LOGIC PATTERN CŨ - GIỮ NGUYÊN]
        switch (p)
        {
            case ItemPattern.Line: int c = Random.Range(3, 6); for (int i = 0; i < c; i++) localPoints.Add(new Vector2(i, 0)); break;
            case ItemPattern.Grid: for (int x = 0; x < 3; x++) for (int y = 0; y < 3; y++) localPoints.Add(new Vector2(x, y - 1)); break;
            case ItemPattern.Wave: for (int i = 0; i < 8; i++) localPoints.Add(new Vector2(i, Mathf.Sin(i * 0.8f) * 1.5f)); break;
            case ItemPattern.ArrowComplex: localPoints.Add(new Vector2(0, 1.5f)); localPoints.Add(new Vector2(0, -1.5f)); localPoints.Add(new Vector2(1, 0.8f)); localPoints.Add(new Vector2(1, -0.8f)); localPoints.Add(new Vector2(2, 0)); break;
            case ItemPattern.Diamond: localPoints.Add(new Vector2(1, 1.5f)); localPoints.Add(new Vector2(1, -1.5f)); localPoints.Add(new Vector2(0, 0)); localPoints.Add(new Vector2(2, 0)); break;
            case ItemPattern.RectHollow: int rw = 4, rh = 3; for (int rx = 0; rx < rw; rx++) for (int ry = 0; ry < rh; ry++) if (rx == 0 || rx == rw - 1 || ry == 0 || ry == rh - 1) localPoints.Add(new Vector2(rx, ry - (rh - 1) / 2f)); break;
            case ItemPattern.RectVertical: int vw = Random.Range(2, 4); int vh = Random.Range(3, 6); for (int vx = 0; vx < vw; vx++) for (int vy = 0; vy < vh; vy++) if (vx == 0 || vx == vw - 1 || vy == 0 || vy == vh - 1) localPoints.Add(new Vector2(vx, vy - (vh - 1) / 2f)); break;
            case ItemPattern.RectHorizontal: int hw = Random.Range(3, 6); int hh = Random.Range(2, 4); for (int hx = 0; hx < hw; hx++) for (int hy = 0; hy < hh; hy++) if (hx == 0 || hx == hw - 1 || hy == 0 || hy == hh - 1) localPoints.Add(new Vector2(hx, hy - (hh - 1) / 2f)); break;
            case ItemPattern.ShapeVLU: localPoints.AddRange(GetTextPoints("V", 0)); localPoints.AddRange(GetTextPoints("L", 4)); localPoints.AddRange(GetTextPoints("U", 8)); break;
            case ItemPattern.ShapeAPlus: localPoints.AddRange(GetTextPoints("A", 0)); localPoints.AddRange(GetTextPoints("+", 4)); break;
            case ItemPattern.Triangle: localPoints.Add(new Vector2(0, 0)); localPoints.Add(new Vector2(1, 0)); localPoints.Add(new Vector2(1, 1)); localPoints.Add(new Vector2(2, 0)); localPoints.Add(new Vector2(2, 1)); localPoints.Add(new Vector2(2, 2)); break;
            case ItemPattern.StairsUp: for (int i = 0; i < 5; i++) localPoints.Add(new Vector2(i, i * 0.5f)); break;
            case ItemPattern.StairsDown: for (int i = 0; i < 5; i++) localPoints.Add(new Vector2(i, 2.5f - (i * 0.5f))); break;
            case ItemPattern.ZigZag: for (int i = 0; i < 6; i++) localPoints.Add(new Vector2(i, (i % 2 == 0) ? 0 : 1.5f)); break;
            case ItemPattern.DoubleLine: for (int i = 0; i < 5; i++) { localPoints.Add(new Vector2(i, 0)); localPoints.Add(new Vector2(i, 1.5f)); } break;
        }

        // Căn giữa Pattern vào khoảng trống
        float patternWidthEst = 5f;
        if (localPoints.Count > 0)
        {
            float maxX = 0;
            foreach (var pnt in localPoints) if (pnt.x > maxX) maxX = pnt.x;
            patternWidthEst = maxX * itemSpacing;
        }

        float midX = startX + (width / 2f);
        float spawnStartX = midX - (patternWidthEst / 2f);
        float currentBaseY = baseHeight + 1.0f;
        float lift = CalculateSmartLift(spawnStartX, currentBaseY, localPoints);
        currentBaseY += lift;

        foreach (Vector2 pt in localPoints)
        {
            Vector3 pos = new Vector3(spawnStartX + pt.x * itemSpacing, currentBaseY + pt.y * itemSpacing, 0);

            // Check biên an toàn (startX, endX được truyền vào đã tính padding bên ObstacleGenerator)
            if (pos.x >= startX && pos.x <= endX)
            {
                if (IsPositionClear(pos)) SpawnItem(pos);
            }
        }
    }

    private bool IsPositionClear(Vector3 pos)
    {
        Collider2D hit = Physics2D.OverlapCircle(pos, checkRadius, obstacleLayer);
        return hit == null;
    }

    private float CalculateSmartLift(float x, float y, List<Vector2> localPoints)
    {
        float maxLift = 0;
        foreach (Vector2 pt in localPoints)
        {
            Vector2 check = new Vector2(x + pt.x * itemSpacing, y + pt.y * itemSpacing);
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
            if (itemContainer) i.transform.SetParent(itemContainer.transform);
            else i.transform.SetParent(transform);

            activeItems.Enqueue(i);
            i.GetComponent<Collectible>()?.Init(d.scoreValue);
        }
    }

    private void RemoveOldItems()
    {
        if (activeItems.Count > 0)
        {
            GameObject o = activeItems.Peek();
            if (o == null) { activeItems.Dequeue(); return; }
            if (playerTransform != null && playerTransform.position.x - o.transform.position.x > destroyDistanceBehind)
                Destroy(activeItems.Dequeue());
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
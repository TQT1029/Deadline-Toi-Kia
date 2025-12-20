using UnityEngine;
using System.Collections.Generic;

public class ItemGenerator : MonoBehaviour
{
    public static ItemGenerator Instance;
    private void Awake() => Instance = this;

    [Header("References")]
    public Transform itemContainer;
    [Tooltip("Danh sách Coin thường")]
    public List<ItemData> commonItems;
    [Tooltip("Danh sách Power-up (Hiếm)")]
    public List<ItemData> rareItems;

    [Header("Settings")]
    public LayerMask surfaceLayer; // Gồm: Ground, Obstacle, MiniPlatform
    public float itemSpacing = 1.0f; // Khoảng cách giữa các item
    public float liftPadding = 1.0f; // Nâng lên bao nhiêu so với vật cản
    public float checkRadius = 0.2f; // Bán kính check va chạm

    // ENUM CÁC PATTERN TỪ SCRIPT GỐC
    public enum ItemPattern
    {
        Line, Grid, Wave, ArrowComplex, Diamond, RectHollow,
        RectVertical, RectHorizontal, ShapeVLU, ShapeAPlus,
        Triangle, StairsUp, StairsDown, ZigZag, DoubleLine
    }

    // Hàm chính gọi bởi MapGenerator
    public void GenerateItems(float startX, float endX)
    {
        float currentX = startX + 2f;

        while (currentX < endX - 2f)
        {
            // 1. Bắn tia xuống để xem bên dưới là gì
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(currentX, 20f), Vector2.down, 50f, surfaceLayer);

            if (hit.collider != null)
            {
                GameObject hitObj = hit.collider.gameObject;
                bool isObstacle = hitObj.CompareTag("Obstacle");
                bool isPlatform = hitObj.CompareTag("MiniPlatform");

                if (isObstacle || isPlatform)
                {
                    // --- TRÊN VẬT CẢN / SÀN BAY ---
                    // Chỉ spawn các hình đơn giản (Line, Cung nhỏ)
                    // Căn giữa theo vật thể đó
                    float objectWidth = hit.collider.bounds.size.x;
                    float objectTop = hit.collider.bounds.max.y;
                    float objectCenterX = hit.collider.bounds.center.x;

                    SpawnOnTop(objectCenterX, objectTop, objectWidth);

                    // Nhảy cóc qua vật cản này để không spawn đè lên nó nữa
                    currentX = hit.collider.bounds.max.x + Random.Range(1f, 3f);
                }
                else
                {
                    // --- TRÊN MẶT ĐẤT TRỐNG ---
                    // Spawn các hình phức tạp (Wave, Text, Grid...)
                    float groundY = hit.point.y;

                    // Random khoảng trống an toàn trước khi spawn
                    currentX += Random.Range(1f, 3f);

                    // Sinh Pattern và lấy về độ rộng thực tế của nó để cộng dồn X
                    float patternWidth = SpawnComplexPattern(currentX, groundY, endX);

                    currentX += patternWidth + Random.Range(3f, 6f); // Khoảng nghỉ sau pattern
                }
            }
            else
            {
                currentX += 2f; // Nếu không thấy đất thì đi tiếp
            }
        }
    }

    // --- LOGIC 1: SPAWN TRÊN ĐỈNH (Đơn giản) ---
    private void SpawnOnTop(float centerX, float topY, float widthAvailable)
    {
        // Tính số lượng item tối đa nhét vừa
        int maxItems = Mathf.FloorToInt(widthAvailable / itemSpacing);
        if (maxItems < 1) maxItems = 1;
        if (maxItems > 5) maxItems = 5; // Giới hạn cho đẹp

        float startItemX = centerX - ((maxItems - 1) * itemSpacing) / 2f;
        float spawnY = topY + liftPadding;

        // 50% là đường thẳng, 50% là hình cung (nếu đủ dài)
        bool isArch = (maxItems >= 3 && Random.value > 0.5f);

        for (int i = 0; i < maxItems; i++)
        {
            float x = startItemX + i * itemSpacing;
            float y = spawnY;

            if (isArch)
            {
                // Parabol: y = 4 * h * t * (1-t)
                float t = (float)i / (maxItems - 1);
                y += 1.5f * 4 * t * (1 - t);
            }

            SpawnSingleItem(new Vector3(x, y, 0));
        }
    }

    // --- LOGIC 2: SPAWN DƯỚI ĐẤT (Phức tạp - Pattern cũ) ---
    private float SpawnComplexPattern(float startX, float groundY, float limitX)
    {
        ItemPattern p = (ItemPattern)Random.Range(0, System.Enum.GetValues(typeof(ItemPattern)).Length);
        List<Vector2> localPoints = new List<Vector2>();

        // Tái sử dụng logic sinh điểm cũ của bạn
        switch (p)
        {
            case ItemPattern.Line: int c = Random.Range(3, 6); for (int i = 0; i < c; i++) localPoints.Add(new Vector2(i, 0)); break;
            case ItemPattern.Grid: for (int x = 0; x < 3; x++) for (int y = 0; y < 3; y++) localPoints.Add(new Vector2(x, y)); break;
            case ItemPattern.Wave: for (int i = 0; i < 8; i++) localPoints.Add(new Vector2(i, Mathf.Sin(i * 0.8f) * 1.5f + 1.5f)); break;
            case ItemPattern.ArrowComplex: localPoints.Add(new Vector2(0, 1.5f)); localPoints.Add(new Vector2(0, -1.5f)); localPoints.Add(new Vector2(1, 0)); localPoints.Add(new Vector2(2, 0)); break;
            case ItemPattern.Diamond: localPoints.Add(new Vector2(1, 2)); localPoints.Add(new Vector2(0, 1)); localPoints.Add(new Vector2(2, 1)); localPoints.Add(new Vector2(1, 0)); break;
            case ItemPattern.RectHollow: int rw = 4, rh = 3; for (int rx = 0; rx < rw; rx++) for (int ry = 0; ry < rh; ry++) if (rx == 0 || rx == rw - 1 || ry == 0 || ry == rh - 1) localPoints.Add(new Vector2(rx, ry)); break;
            case ItemPattern.RectVertical: int vw = Random.Range(2, 4); int vh = Random.Range(3, 5); for (int vx = 0; vx < vw; vx++) for (int vy = 0; vy < vh; vy++) localPoints.Add(new Vector2(vx, vy)); break;
            case ItemPattern.RectHorizontal: int hw = Random.Range(3, 6); int hh = Random.Range(2, 4); for (int hx = 0; hx < hw; hx++) for (int hy = 0; hy < hh; hy++) localPoints.Add(new Vector2(hx, hy)); break;
            case ItemPattern.ShapeVLU: localPoints.AddRange(GetTextPoints("V", 0)); localPoints.AddRange(GetTextPoints("L", 4)); localPoints.AddRange(GetTextPoints("U", 8)); break;
            case ItemPattern.ShapeAPlus: localPoints.AddRange(GetTextPoints("A", 0)); localPoints.AddRange(GetTextPoints("+", 4)); break;
            case ItemPattern.Triangle: for (int y = 0; y < 3; y++) for (int x = 0; x <= y; x++) localPoints.Add(new Vector2(y, x)); break;
            case ItemPattern.StairsUp: for (int i = 0; i < 5; i++) localPoints.Add(new Vector2(i, i * 0.5f)); break;
            case ItemPattern.StairsDown: for (int i = 0; i < 5; i++) localPoints.Add(new Vector2(i, 2.5f - (i * 0.5f))); break;
            case ItemPattern.ZigZag: for (int i = 0; i < 6; i++) localPoints.Add(new Vector2(i, (i % 2 == 0) ? 0 : 1.5f)); break;
            case ItemPattern.DoubleLine: for (int i = 0; i < 5; i++) { localPoints.Add(new Vector2(i, 0)); localPoints.Add(new Vector2(i, 1.5f)); } break;
        }

        // Tính toán vị trí thực tế & Nâng (Smart Lift)
        float maxX = 0;
        float baseLift = groundY + 1.0f; // Mặc định cách đất 1m

        // Kiểm tra xem pattern này có bị đè lên vật cản nào phía trước không
        // Nếu có vật cản chắn ngang pattern, ta nâng toàn bộ pattern lên cao hơn vật cản đó
        float maxObstacleHeight = baseLift;
        foreach (var pt in localPoints)
        {
            Vector2 checkPos = new Vector2(startX + pt.x * itemSpacing, baseLift + pt.y * itemSpacing);
            Collider2D hit = Physics2D.OverlapCircle(checkPos, checkRadius, surfaceLayer);
            if (hit != null && (hit.CompareTag("Obstacle") || hit.CompareTag("MiniPlatform")))
            {
                if (hit.bounds.max.y > maxObstacleHeight)
                    maxObstacleHeight = hit.bounds.max.y;
            }
            if (pt.x > maxX) maxX = pt.x;
        }

        // Cập nhật độ cao cơ sở nếu cần nâng
        if (maxObstacleHeight > baseLift) baseLift = maxObstacleHeight + liftPadding;

        // Spawn Pattern
        foreach (Vector2 pt in localPoints)
        {
            Vector3 spawnPos = new Vector3(startX + pt.x * itemSpacing, baseLift + pt.y * itemSpacing, 0);

            // Check lần cuối xem có vượt quá giới hạn map sinh ra không
            if (spawnPos.x < limitX)
            {
                SpawnSingleItem(spawnPos);
            }
        }

        return maxX * itemSpacing; // Trả về chiều dài của pattern để cộng dồn
    }

    private void SpawnSingleItem(Vector3 pos)
    {
        ItemData data = GetRandomItem();
        if (data != null && data.prefab != null)
        {
            GameObject obj = Instantiate(data.prefab, pos, Quaternion.identity, itemContainer);
            // Setup script Collectible nếu cần
        }
    }

    private ItemData GetRandomItem()
    {
        if (commonItems.Count == 0) return null;
        if (rareItems.Count > 0 && Random.value < 0.1f) return rareItems[Random.Range(0, rareItems.Count)];
        return commonItems[Random.Range(0, commonItems.Count)];
    }

    // Hàm vẽ chữ cái cũ của bạn
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
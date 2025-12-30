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

    [Header("Settings")]
    public LayerMask surfaceLayer; // Gồm: Ground, Obstacle, MiniPlatform
    public float itemSpacing = 1.0f; // Khoảng cách giữa các item
    public float liftPadding = .5f; // Nâng lên bao nhiêu so với vật cản
    public float checkRadius = 0.2f; // Bán kính check va chạm

    [SerializeField] private float minGap = 1.0f;
    [SerializeField] private float maxGap = 3.0f;


    [Space]
    [Tooltip("Xác suất spawn item trên vật cản (%)")]
    [SerializeField, Range(0, 100)] private float obstacleChanceItems = 50f;
    [Tooltip("Xác suất spawn item trên sàn bay (%)")]
    [SerializeField, Range(0, 100)] private float platformChanceItems = 70f;

    [Space]
    [Tooltip("Khoảng cách từ mặt đất đến vị trí spawn item")]
    [SerializeField] private float groundPadding = .5f;

    // ENUM CÁC PATTERN TỪ SCRIPT GỐC
    public enum ItemPattern
    {
        Line, Grid, Wave, Diamond, RectHollow,
        RectVertical, RectHorizontal, ShapeVLU, ShapeAPlus,
        Triangle, StairsUp, StairsDown, ZigZag, DoubleLine
    }

    // Hàm chính gọi bởi MapGenerator
    public void GenerateItems(float startX, float endX)
    {
        float currentX = startX + 2f;
        int loopSafety = 0;

        while (currentX < endX - 2f)
        {
            if (loopSafety++ > 500)
            {
                Debug.LogWarning("[ItemGenerator] Infinite Loop Detected! Breaking out.");
                break;
            }
            float nextX = currentX; // Giá trị X dự kiến cho vòng sau

            // 1. Bắn tia xuống để xem bên dưới là gì
            RaycastHit2D hit = GetBestSurfaceHit(currentX);


            // 2. Tìm tất cả collider trong vùng OverlapBox dưới điểm hit để xác định loại bề mặt
            Collider2D[] colliderOverLaps = Physics2D.OverlapBoxAll(hit.point, new Vector2(10f, 1f), 0f, surfaceLayer);
            Collider2D colliderOverLap = default;

            if ((colliderOverLaps != null && colliderOverLaps.Length > 0))
            {
                colliderOverLap = colliderOverLaps[Random.Range(0, colliderOverLaps.Length)];

                foreach (var onCollider in colliderOverLaps)
                {
                    if (onCollider.CompareTag("Obstacle") || onCollider.CompareTag("MiniPlatform"))
                    {
                        colliderOverLap = onCollider;
                        break;
                    }
                }

            }


            // 3. Xử lý tùy theo loại bề mặt
            if (colliderOverLap != null)
            {
                GameObject obj = colliderOverLap.gameObject;
                bool isObstacle = obj.CompareTag("Obstacle");
                bool isPlatform = obj.CompareTag("MiniPlatform");

                GameObject objParent = obj.transform.parent.gameObject;
                Bounds boundsParent = GetBound(objParent);

                if (isObstacle)
                {
                    // --- TRÊN VẬT CẢN ---
                    SpawnOnTopChance(obstacleChanceItems, boundsParent);
                    // Nhảy qua vật cản này để không spawn đè lên nó nữa
                    if (!objParent.CompareTag("Container"))
                        currentX = boundsParent.max.x + RandomUtilities.RandomWithSteps(minGap, maxGap);
                    currentX += 5f;
                }
                else if (isPlatform)
                {
                    // --- TRÊN SÀN BAY ---
                    SpawnOnTopChance(platformChanceItems, boundsParent);
                    // Nhảy qua vật cản này để không spawn đè lên nó nữa
                    if (!objParent.CompareTag("Container"))
                        currentX = boundsParent.max.x + RandomUtilities.RandomWithSteps(minGap, maxGap);
                    currentX += 2f;

                }
                else
                {
                    // --- TRÊN MẶT ĐẤT ---

                    float groundY = hit.point.y + groundPadding;
                    Vector2 spawnOrigin = new Vector2(currentX, groundY);


                    // Sinh Pattern
                    float patternWidth = SpawnComplexPattern(currentX, groundY, endX);
                    currentX += patternWidth + Random.Range(3f, 6f);
                }
            }
            else
            {
                currentX += 2f; // Nếu không thấy đất thì đi tiếp
            }
        }
    }

    private RaycastHit2D GetBestSurfaceHit(float xPos)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(xPos, 20f), Vector2.down, 50f, surfaceLayer);

        // Ưu tiên tìm Obstacle hoặc Platform trước
        foreach (var h in hits)
        {
            if (h.collider.CompareTag("Obstacle") || h.collider.CompareTag("MiniPlatform"))
                return h;
        }

        // Nếu không có, lấy bề mặt cao nhất (Ground)
        if (hits != null && hits.Length > 0) return hits[Random.Range(0, hits.Length)]; // RaycastAll thường trả về theo thứ tự distance, nhưng an toàn thì sort lại nếu cần

        return default;
    }

    private GameObject GetObstacleRoot(Transform child)
    {
        Transform current = child;

        // Duyệt ngược lên trên cho đến khi gặp object cha có Tag là Container 
        // Hoặc hết cha (null)
        // Object ngay dưới Container chính là Group Root
        while (current.parent != null)
        {
            if (current.parent.CompareTag("Container") || current.parent.name.Contains("Container"))
            {
                // Nếu cha là Container, thì current chính là Root của Obstacle
                return current.gameObject;
            }
            current = current.parent;
        }

        // Nếu không tìm thấy Container, trả về object ngoài cùng tìm được
        return current.gameObject;
    }

    private Bounds GetBound(GameObject obj)
    {
        // 1. Lấy tất cả Collider2D trong obj (bao gồm cả object cha và các con)
        var colliders = obj.GetComponentsInChildren<Collider2D>(true);

        if (colliders.Length > 0)
        {
            // Khởi tạo bounds bằng collider đầu tiên tìm thấy
            Bounds combinedBounds = colliders[0].bounds;

            // 2. Duyệt qua các collider còn lại và mở rộng bounds để bao trùm tất cả
            for (int i = 1; i < colliders.Length; i++)
            {
                combinedBounds.Encapsulate(colliders[i].bounds);
            }

            // 3. Trả về bounds tổng
            return combinedBounds;
        }

        // Fallback: Nếu không tìm thấy collider nào, trả về mặc định
        return default;

    }

    private void SpawnOnTopChance(float chance, Bounds bound)
    {
        if (!RandomUtilities.ChancePercent(chance)) return;

        // --- TRÊN VẬT CẢN / SÀN BAY ---
        // Căn giữa theo vật thể đó
        float objectMinX = bound.min.x;
        float objectTop = bound.max.y;
        float objectMaxX = bound.max.x;

        SpawnComplexPattern(objectMinX, objectTop + liftPadding, objectMaxX);
        //SpawnOnTop(objectCenterX, objectTop, objectWidth);
    }
    /*
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
    */
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

    // --- HÀM XỬ LÝ ĐẨY ITEM (SMART PUSH) ---
    private Vector3 ResolveObstacleCollision(Vector3 originalPos)
    {
        // Kiểm tra xem tại vị trí dự kiến có đè lên Obstacle nào không
        // Dùng OverlapCircle để bắt va chạm
        Collider2D hit = Physics2D.OverlapCircle(originalPos, checkRadius, surfaceLayer);

        // Nếu không đụng gì hoặc đụng đất thường -> Giữ nguyên vị trí
        if (hit == null || (!hit.CompareTag("Obstacle") && !hit.CompareTag("MiniPlatform")))
        {
            return originalPos;
        }

        // Nếu đụng Obstacle -> Tính toán đẩy ra
        Bounds b = hit.bounds;

        // Tính khoảng cách tới 3 mép: Trái, Phải, Trên
        float distToLeft = Mathf.Abs(originalPos.x - b.min.x);
        float distToRight = Mathf.Abs(originalPos.x - b.max.x);
        float distToTop = Mathf.Abs(originalPos.y - b.max.y);

        // Kiểm tra xem có nằm ở "Vùng Trung Tâm" không (chiếm 40% ở giữa vật thể)
        float distFromCenter = Mathf.Abs(originalPos.x - b.center.x);
        bool isNearCenter = distFromCenter < (b.size.x * 0.2f); // 0.2 mỗi bên = 40% giữa

        Vector3 newPos = originalPos;

        // --- QUY TẮC ĐẨY ---
        if (isNearCenter)
        {
            // 1. Nếu ở giữa -> Bắt buộc đẩy LÊN TRÊN
            newPos.y = b.max.y + liftPadding;
            // Giữ nguyên X để pattern không bị méo quá nhiều
        }
        else
        {
            // 2. Nếu không ở giữa -> Tìm hướng gần nhất để đẩy (Trái, Phải hoặc Trên)

            // Tìm khoảng cách nhỏ nhất trong 3 hướng
            float min = Mathf.Min(distToLeft, distToRight, distToTop);

            if (min == distToTop)
            {
                // Gần nắp nhất -> Đẩy lên
                newPos.y = b.max.y + liftPadding;
            }
            else if (min == distToLeft)
            {
                // Gần mép trái nhất -> Đẩy sang trái
                newPos.x = b.min.x - liftPadding;
                // Có thể cần chỉnh lại Y một chút cho đẹp nếu muốn, nhưng giữ nguyên Y pattern cũng được
            }
            else // min == distToRight
            {
                // Gần mép phải nhất -> Đẩy sang phải
                newPos.x = b.max.x + liftPadding;
            }
        }

        return newPos;
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
        if (commonItems.Count == 0 || commonItems == null) return null;

        float totalWeightValue = 0f;

        foreach (var item in commonItems) totalWeightValue += item.spawnWeight;

        for (int i = 0; i < commonItems.Count; i++)
        {
            if (RandomUtilities.ChanceWeight(commonItems[i].spawnWeight, totalWeightValue))
                return commonItems[i];
        }

        return commonItems[0];
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
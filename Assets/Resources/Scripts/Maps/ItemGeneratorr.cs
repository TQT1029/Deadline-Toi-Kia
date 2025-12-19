using UnityEngine;
using System.Collections.Generic;

public class ItemGenerator : MonoBehaviour
{
    public static ItemGenerator Instance;
    private void Awake() => Instance = this;

    [Header("References")]
    public Transform itemContainer;
    public List<ItemData> commonItems; // Coin, etc.
    public List<ItemData> rareItems;   // Powerups
    public List<PatternData> patterns;

    [Header("Settings")]
    public float itemSpacing = 1.0f;
    public LayerMask obstacleLayer; // Layer của Obstacle và MiniPlatform
    public float spawnInterval = 15f; // Khoảng cách giữa các pattern trên đường

    public void SpawnItems(float startX, float endX, float groundY)
    {
        float currentX = startX + 5f;

        while (currentX < endX - 5f)
        {
            PatternData pattern = GetRandomPattern();
            if (pattern != null)
            {
                Vector3 spawnPos = new Vector3(currentX, groundY + 1f, 0); // Mặc định spawn sát đất

                // 1. Tạo Pattern
                if (pattern.type == PatternData.PatternType.PrefabBased)
                {
                    SpawnPrefabPattern(pattern, spawnPos);
                }
                else
                {
                    SpawnCodePattern(pattern.codePattern, spawnPos);
                }

                // Ước lượng độ dài pattern để cộng dồn X
                currentX += 8f; // Giả sử độ dài trung bình
            }

            // Check xem có MiniPlatform ở đoạn này không để spawn dưới gầm
            CheckSpawnUnderPlatform(currentX, groundY);

            currentX += UnityEngine.Random.Range(5f, spawnInterval);
        }
    }

    // --- LOGIC 1: SPAWN PREFAB PATTERN + MUTATION ---
    private void SpawnPrefabPattern(PatternData data, Vector3 pos)
    {
        if (data.patternPrefab == null) return;

        // Instantiate tạm để xử lý
        GameObject patternObj = Instantiate(data.patternPrefab, pos, Quaternion.identity);

        // Xử lý Smart Positioning (Nâng lên nếu đụng Obstacle)
        Vector3 correctedPos = GetSmartPosition(patternObj, pos);
        patternObj.transform.position = correctedPos;

        // Xử lý Mutation (Biến đổi item con)
        foreach (Transform child in patternObj.transform)
        {
            if (UnityEngine.Random.value < data.mutationChance)
            {
                // Thay thế bằng item khác
                ItemData newItem = GetRandomItem();
                if (newItem != null)
                {
                    Vector3 childPos = child.position;
                    Destroy(child.gameObject);
                    GameObject newObj = Instantiate(newItem.prefab, childPos, Quaternion.identity, patternObj.transform);
                }
            }
            // Đảm bảo item con có Collectible script
        }

        // Ungroup (để quản lý dễ hơn hoặc giữ group tùy bạn)
        // Ở đây tôi giữ group nhưng set parent vào container chính
        patternObj.transform.SetParent(itemContainer);
    }

    // --- LOGIC 2: SPAWN CODE PATTERN ---
    private void SpawnCodePattern(CodePatternType type, Vector3 origin)
    {
        List<Vector2> localPoints = new List<Vector2>();

        // Tạo hình dáng
        switch (type)
        {
            case CodePatternType.Line:
                for (int i = 0; i < 5; i++) localPoints.Add(new Vector2(i, 0));
                break;
            case CodePatternType.Wave:
                for (int i = 0; i < 6; i++) localPoints.Add(new Vector2(i, Mathf.Sin(i) * 1.5f));
                break;
            case CodePatternType.Grid:
                for (int x = 0; x < 3; x++)
                    for (int y = 0; y < 2; y++) localPoints.Add(new Vector2(x, y));
                break;
                // Thêm các case khác: Parabola, ZigZag...
        }

        // Tính toán vị trí trung tâm thực tế sau khi check va chạm
        // Tạo một object ảo hoặc tính toán bounds để check va chạm cho toàn bộ pattern
        // Ở đây ta dùng cách đơn giản: Check từng điểm và lấy độ cao lớn nhất

        float maxLift = 0f;
        foreach (var p in localPoints)
        {
            float lift = GetLiftHeight(origin + (Vector3)p * itemSpacing);
            if (lift > maxLift) maxLift = lift;
        }

        // Spawn từng item
        foreach (var p in localPoints)
        {
            Vector3 finalPos = origin + (Vector3)p * itemSpacing;
            finalPos.y += maxLift; // Nâng toàn bộ pattern lên độ cao an toàn nhất

            // Kiểm tra biên MiniPlatform (nếu item này đang nằm trên mini platform)
            if (IsOnMiniPlatform(finalPos, out float platformLeft, out float platformRight))
            {
                // Clamp X để không lòi ra ngoài platform
                finalPos.x = Mathf.Clamp(finalPos.x, platformLeft, platformRight);
            }

            ItemData itemData = GetRandomItem();
            GameObject item = Instantiate(itemData.prefab, finalPos, Quaternion.identity);
            item.transform.SetParent(itemContainer);
        }
    }

    // --- UTILITIES & SMART LOGIC ---

    // Tính toán độ cao cần nâng lên nếu đụng vật cản
    private float GetLiftHeight(Vector3 pos)
    {
        Collider2D hit = Physics2D.OverlapPoint(pos, obstacleLayer);
        if (hit != null)
        {
            // Trả về khoảng cách từ pos.y đến mặt trên của vật cản + padding
            return (hit.bounds.max.y - pos.y) + 1.0f;
        }
        return 0f;
    }

    // Logic căn giữa cho Prefab Pattern
    private Vector3 GetSmartPosition(GameObject patternObj, Vector3 originalPos)
    {
        // Tính Bounds của toàn bộ pattern
        Bounds b = new Bounds(originalPos, Vector3.zero);
        foreach (Renderer r in patternObj.GetComponentsInChildren<Renderer>())
        {
            b.Encapsulate(r.bounds);
        }

        // Check va chạm khu vực này
        Collider2D hit = Physics2D.OverlapArea(b.min, b.max, obstacleLayer);
        if (hit != null)
        {
            // Nếu đụng, dời pattern lên trên đỉnh vật cản đó
            float newY = hit.bounds.max.y + (b.extents.y) + 1.0f;
            // Căng giữa theo X của vật cản
            float newX = hit.bounds.center.x;
            return new Vector3(newX, newY, 0);
        }
        return originalPos;
    }

    // Logic check xem item có đang lơ lửng trên mini platform không
    private bool IsOnMiniPlatform(Vector3 pos, out float left, out float right)
    {
        // Raycast xuống dưới để xem có phải MiniPlatform không
        RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.down, 5f, obstacleLayer);
        if (hit.collider != null && hit.collider.name.Contains("Mini")) // Hoặc dùng Tag/Layer cụ thể
        {
            left = hit.collider.bounds.min.x + 0.5f; // Padding
            right = hit.collider.bounds.max.x - 0.5f;
            return true;
        }
        left = 0; right = 0;
        return false;
    }

    // Spawn item dưới gầm
    private void CheckSpawnUnderPlatform(float x, float groundY)
    {
        // Bắn Raycast lên trên xem có platform nào ở trên không
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, groundY), Vector2.up, 10f, obstacleLayer);
        if (hit.collider != null)
        {
            float heightDiff = hit.collider.bounds.min.y - groundY;
            if (heightDiff > 2.5f) // Đủ cao để đi qua
            {
                if (UnityEngine.Random.value > 0.5f)
                {
                    ItemData coin = commonItems[0];
                    Instantiate(coin.prefab, new Vector3(x, groundY + 0.5f, 0), Quaternion.identity, itemContainer);
                }
            }
        }
    }

    private ItemData GetRandomItem()
    {
        // Logic random cơ bản
        return commonItems.Count > 0 ? commonItems[0] : null;
    }

    private PatternData GetRandomPattern()
    {
        if (patterns.Count == 0) return null;
        return patterns[UnityEngine.Random.Range(0, patterns.Count)];
    }
}
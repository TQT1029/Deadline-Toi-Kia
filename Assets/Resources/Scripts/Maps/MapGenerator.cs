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

    [Header("Infinite Settings")]
    [Tooltip("Khoảng cách phía sau để xóa vật thể")]
    public float destroyDistanceBehind = 20f;
    public float groundY = -2f;

    [Header("Physics Settings")]
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;
    public float checkRadius = 0.4f;

    [Header("Spacing & Logic")]
    public float minGap = 6f;
    public float maxGap = 10f;
    [Range(0, 100)] public int chanceToSpawnObstacle = 40;
    [Range(0, 100)] public int chanceItemOnObstacle = 70;
    public float itemSpacing = 1.0f;

    // Biến theo dõi vị trí spawn HIỆN TẠI (trong phạm vi đất mới)
    private float currentSpawnX;
    private Queue<GameObject> activeObjects = new Queue<GameObject>();

    private enum ItemPattern
    {
        Line, Grid, Wave, ArrowComplex, Diamond, RectHollow,
        ShapeVLU, ShapeAPlus, RectVertical, RectHorizontal,
        Triangle, StairsUp, StairsDown, ZigZag, DoubleLine
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        // --- ĐĂNG KÝ SỰ KIỆN ---
        // Khi EndlessGameController sinh ra sàn mới, hàm OnMapGenerated sẽ được gọi
        if (EndlessGameController.Instance != null)
        {
            EndlessGameController.Instance.OnPlatformSpawned += OnMapGenerated;
        }

        // Khởi tạo điểm spawn ban đầu (tránh lỗi nếu không có sự kiện nào lúc đầu)
        if (playerTransform != null)
            currentSpawnX = playerTransform.position.x + 5f;
    }

    private void OnDestroy()
    {
        // Hủy đăng ký để tránh lỗi bộ nhớ
        if (EndlessGameController.Instance != null)
        {
            EndlessGameController.Instance.OnPlatformSpawned -= OnMapGenerated;
        }
    }

    private void Update()
    {
        // Chỉ còn nhiệm vụ xóa vật thể cũ, không lo spawn nữa
        RemoveOldObjects();
    }

    // --- HÀM XỬ LÝ KHI NHẬN ĐƯỢC TIN NHẮN TỪ ENDLESS CONTROLLER ---
    private void OnMapGenerated(float startX, float endX)
    {
        // Đồng bộ hóa: Nếu con trỏ hiện tại đang ở phía sau miếng đất mới,
        // hãy kéo nó lên đầu miếng đất mới.
        if (currentSpawnX < startX) currentSpawnX = startX;

        // Sinh vật phẩm liên tục cho đến khi hết chiều dài miếng đất mới này
        // (Trừ đi 1 khoảng đệm nhỏ cuối sàn để tránh vật thể bị lòi ra mép)
        float safeEndX = endX - 2.0f;

        while (currentSpawnX < safeEndX)
        {
            SpawnSmartGroup(safeEndX);
        }
    }

    // --- LOGIC SPAWN ---
    private void SpawnSmartGroup(float limitX)
    {
        // Không kiểm tra Hố hay Đất nữa vì ta ĐÃ BIẾT đây là đất (do EndlessController gửi sang)

        float gap = Random.Range(minGap, maxGap);
        currentSpawnX += gap;

        // Nếu cộng gap xong mà vượt quá giới hạn miếng đất -> Dừng
        if (currentSpawnX > limitX) return;

        bool spawnObstacle = Random.Range(0, 100) < chanceToSpawnObstacle;
        float addedWidth = 0;

        if (spawnObstacle)
        {
            ObstacleData obsToSpawn = GetRandomObstacle();
            // Kiểm tra xem vật cản có nằm gọn trong miếng đất không
            if (obsToSpawn != null && (currentSpawnX + obsToSpawn.width) <= limitX)
            {
                addedWidth = SpawnObstacleLogic(currentSpawnX, obsToSpawn);
            }
            else
            {
                addedWidth = SpawnItemPatternLogic(currentSpawnX);
            }
        }
        else
        {
            addedWidth = SpawnItemPatternLogic(currentSpawnX);
        }

        currentSpawnX += addedWidth;
    }

    // --- CÁC HÀM SPAWN PATTERN GIỮ NGUYÊN ---
    private float SpawnItemPatternLogic(float startX)
    {
        ItemPattern pattern = (ItemPattern)Random.Range(0, System.Enum.GetValues(typeof(ItemPattern)).Length);
        List<Vector2> localPoints = new List<Vector2>();

        switch (pattern)
        {
            case ItemPattern.Line:
                int c = Random.Range(3, 6);
                for (int i = 0; i < c; i++) localPoints.Add(new Vector2(i, 0));
                break;
            case ItemPattern.Grid:
                for (int x = 0; x < 3; x++) for (int y = 0; y < 3; y++) localPoints.Add(new Vector2(x, y - 1));
                break;
            case ItemPattern.Wave:
                for (int i = 0; i < 8; i++) localPoints.Add(new Vector2(i, Mathf.Sin(i * 0.8f) * 1.5f));
                break;
            case ItemPattern.ArrowComplex:
                localPoints.Add(new Vector2(0, 1.5f)); localPoints.Add(new Vector2(0, -1.5f));
                localPoints.Add(new Vector2(1, 0.8f)); localPoints.Add(new Vector2(1, -0.8f));
                localPoints.Add(new Vector2(2, 0));
                break;
            case ItemPattern.Diamond:
                localPoints.Add(new Vector2(1, 1.5f)); localPoints.Add(new Vector2(1, -1.5f));
                localPoints.Add(new Vector2(0, 0)); localPoints.Add(new Vector2(2, 0));
                break;
            case ItemPattern.RectHollow:
                int rw = 4, rh = 3;
                for (int x = 0; x < rw; x++) for (int y = 0; y < rh; y++) if (x == 0 || x == rw - 1 || y == 0 || y == rh - 1) localPoints.Add(new Vector2(x, y - (rh - 1) / 2f));
                break;
            case ItemPattern.RectVertical:
                int vw = Random.Range(2, 4); int vh = Random.Range(3, 6);
                for (int x = 0; x < vw; x++) for (int y = 0; y < vh; y++) if (x == 0 || x == vw - 1 || y == 0 || y == vh - 1) localPoints.Add(new Vector2(x, y - (vh - 1) / 2f));
                break;
            case ItemPattern.RectHorizontal:
                int hw = Random.Range(3, 6); int hh = Random.Range(2, 4);
                for (int x = 0; x < hw; x++) for (int y = 0; y < hh; y++) if (x == 0 || x == hw - 1 || y == 0 || y == hh - 1) localPoints.Add(new Vector2(x, y - (hh - 1) / 2f));
                break;
            case ItemPattern.ShapeVLU:
                localPoints.AddRange(GetTextPoints("V", 0));
                localPoints.AddRange(GetTextPoints("L", 4));
                localPoints.AddRange(GetTextPoints("U", 8));
                break;
            case ItemPattern.ShapeAPlus:
                localPoints.AddRange(GetTextPoints("A", 0));
                localPoints.AddRange(GetTextPoints("+", 4));
                break;
            case ItemPattern.Triangle:
                localPoints.Add(new Vector2(0, 0));
                localPoints.Add(new Vector2(1, 0)); localPoints.Add(new Vector2(1, 1));
                localPoints.Add(new Vector2(2, 0)); localPoints.Add(new Vector2(2, 1)); localPoints.Add(new Vector2(2, 2));
                break;
            case ItemPattern.StairsUp:
                for (int i = 0; i < 5; i++) localPoints.Add(new Vector2(i, i * 0.5f));
                break;
            case ItemPattern.StairsDown:
                for (int i = 0; i < 5; i++) localPoints.Add(new Vector2(i, 2.5f - (i * 0.5f)));
                break;
            case ItemPattern.ZigZag:
                for (int i = 0; i < 6; i++) localPoints.Add(new Vector2(i, (i % 2 == 0) ? 0 : 1.5f));
                break;
            case ItemPattern.DoubleLine:
                for (int i = 0; i < 5; i++) { localPoints.Add(new Vector2(i, 0)); localPoints.Add(new Vector2(i, 1.5f)); }
                break;
        }

        float currentBaseY = groundY + 1.5f;
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
            case "V": pts.Add(new Vector2(0, 2)); pts.Add(new Vector2(2, 2)); pts.Add(new Vector2(0, 1)); pts.Add(new Vector2(2, 1)); pts.Add(new Vector2(1, 0)); break;
            case "L": pts.Add(new Vector2(0, 2)); pts.Add(new Vector2(0, 1)); pts.Add(new Vector2(0, 0)); pts.Add(new Vector2(1, 0)); pts.Add(new Vector2(2, 0)); break;
            case "U": pts.Add(new Vector2(0, 2)); pts.Add(new Vector2(2, 2)); pts.Add(new Vector2(0, 1)); pts.Add(new Vector2(2, 1)); pts.Add(new Vector2(0, 0)); pts.Add(new Vector2(1, 0)); pts.Add(new Vector2(2, 0)); break;
            case "A": pts.Add(new Vector2(1, 2)); pts.Add(new Vector2(0, 1)); pts.Add(new Vector2(2, 1)); pts.Add(new Vector2(1, 1)); pts.Add(new Vector2(0, 0)); pts.Add(new Vector2(2, 0)); break;
            case "+": pts.Add(new Vector2(1, 2)); pts.Add(new Vector2(0, 1)); pts.Add(new Vector2(1, 1)); pts.Add(new Vector2(2, 1)); pts.Add(new Vector2(1, 0)); break;
        }
        for (int i = 0; i < pts.Count; i++) pts[i] = new Vector2(pts[i].x + xOffset, pts[i].y);
        return pts;
    }

    private ItemData GetRandomItemData()
    {
        if (itemLibrary == null || itemLibrary.Count == 0) return null;
        float totalWeight = 0f;
        foreach (var item in itemLibrary) totalWeight += item.spawnWeight;
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        foreach (var item in itemLibrary) { currentWeight += item.spawnWeight; if (randomValue < currentWeight) return item; }
        return itemLibrary[0];
    }

    private ObstacleData GetRandomObstacle()
    {
        if (obstacles == null || obstacles.Count == 0) return null;
        return obstacles[Random.Range(0, obstacles.Count)];
    }

    private float SpawnObstacleLogic(float posX, ObstacleData obsData)
    {
        float prefabY = obsData.prefab.transform.position.y;
        Vector3 spawnPos = new Vector3(posX, groundY + prefabY, 0);

        GameObject obsObj = Instantiate(obsData.prefab, spawnPos, Quaternion.identity);
        RegisterObject(obsObj, true);

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

    private void RegisterObject(GameObject obj, bool isObstacle)
    {
        activeObjects.Enqueue(obj);
        if (isObstacle)
        {
            if (obstacleObjs != null) obj.transform.SetParent(obstacleObjs.transform);
            else obj.transform.SetParent(this.transform);
        }
        else
        {
            if (itemObjs != null) obj.transform.SetParent(itemObjs.transform);
            else obj.transform.SetParent(this.transform);
        }
    }

    private void SpawnItem(Vector3 pos)
    {
        if (pos.y < groundY + 0.5f) pos.y = groundY + 0.5f;
        ItemData data = GetRandomItemData();
        if (data != null && data.prefab != null)
        {
            GameObject item = Instantiate(data.prefab, pos, Quaternion.identity);
            RegisterObject(item, false);
            Collectible col = item.GetComponent<Collectible>();
            if (col != null) col.Init(data.scoreValue);
        }
    }

    private void RemoveOldObjects()
    {
        if (activeObjects.Count > 0)
        {
            GameObject oldestObj = activeObjects.Peek();
            if (oldestObj == null)
            {
                activeObjects.Dequeue();
                return;
            }
            if (playerTransform.position.x - oldestObj.transform.position.x > destroyDistanceBehind)
            {
                GameObject objToRemove = activeObjects.Dequeue();
                Destroy(objToRemove);
            }
        }
    }
}
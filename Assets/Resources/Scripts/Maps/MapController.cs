using UnityEngine;
using System.Collections.Generic;

public class MapController : MonoBehaviour
{
    public static MapController Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Header("References")]
    [Tooltip("Kéo Transform của nhân vật vào đây để script biết vị trí spawn")]
    public Transform playerTransform;
    public GameObject obstacleObjs; // Container chứa Obstacles
    public GameObject itemObjs;     // Container chứa Items

    [Header("Data Config")]
    public List<ObstacleData> obstacles;
    public List<ItemData> itemLibrary;

    [Header("Infinite Settings")]
    [Tooltip("Khoảng cách spawn trước mặt người chơi")]
    public float spawnDistanceAhead = 50f;
    [Tooltip("Khoảng cách phía sau để xóa vật thể")]
    public float destroyDistanceBehind = 20f;
    public float groundY = -2f;

    [Header("Physics Settings")]
    public LayerMask obstacleLayer;
    public float checkRadius = 0.4f;

    [Header("Spacing & Logic")]
    public float minGap = 6f;
    public float maxGap = 10f;
    [Range(0, 100)] public int chanceToSpawnObstacle = 40;
    [Range(0, 100)] public int chanceItemOnObstacle = 70;
    public float itemSpacing = 1.0f;

    // Biến theo dõi vị trí spawn hiện tại
    private float currentSpawnX;
    // Danh sách quản lý các vật thể đang tồn tại để xóa dần
    private Queue<GameObject> activeObjects = new Queue<GameObject>();

    private enum ItemPattern { Line, Grid, Wave, ArrowComplex, Diamond, RectHollow, ShapeVLU, ShapeAPlus, RectVertical, RectHorizontal }

    private void Start()
    {
        // Tự tìm Player nếu chưa gán
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        // Khởi tạo vị trí spawn bắt đầu (cách nhân vật một đoạn)
        if (playerTransform != null)
            currentSpawnX = playerTransform.position.x + 10f;
        else
            currentSpawnX = 0f;

        // Pre-warm: Spawn trước một đoạn dài để khi game start không bị trống
        SpawnChunk();
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // 1. SPAWN: Nếu nhân vật chạy gần đến điểm spawn hiện tại -> Spawn tiếp
        if (playerTransform.position.x > currentSpawnX - spawnDistanceAhead)
        {
            SpawnSingleGroup();
        }

        // 2. DESPAWN: Xóa vật thể phía sau lưng
        RemoveOldObjects();
    }

    // --- HÀM KHỞI TẠO ĐẦU GAME ---
    private void SpawnChunk()
    {
        // Spawn liên tục cho đến khi đủ độ dài ban đầu
        for (int i = 0; i < 10; i++)
        {
            SpawnSingleGroup();
        }
    }

    // --- LOGIC SINH 1 NHÓM (OBSTACLE HOẶC ITEM) ---
    private void SpawnSingleGroup()
    {
        // Cộng khoảng cách nghỉ ngẫu nhiên
        float gap = Random.Range(minGap, maxGap);
        currentSpawnX += gap;

        bool spawnObstacle = Random.Range(0, 100) < chanceToSpawnObstacle;
        float addedWidth = 0;

        if (spawnObstacle)
        {
            addedWidth = SpawnObstacleLogic(currentSpawnX);
        }
        else
        {
            addedWidth = SpawnItemPatternLogic(currentSpawnX);
        }

        // Cập nhật vị trí con trỏ X sau khi đã đặt vật thể
        currentSpawnX += addedWidth;
    }

    // --- LOGIC XÓA VẬT CŨ ---
    private void RemoveOldObjects()
    {
        if (activeObjects.Count > 0)
        {
            // Kiểm tra vật đầu tiên trong hàng đợi
            GameObject oldestObj = activeObjects.Peek();

            // Nếu vật thể bị hủy do va chạm trước đó -> Loại khỏi hàng đợi
            if (oldestObj == null)
            {
                activeObjects.Dequeue();
                return;
            }

            // Nếu vật thể đã trôi quá xa về phía sau -> Xóa
            if (playerTransform.position.x - oldestObj.transform.position.x > destroyDistanceBehind)
            {
                GameObject objToRemove = activeObjects.Dequeue();
                Destroy(objToRemove);
            }
        }
    }

    // --- LOGIC 1: SPAWN OBSTACLE ---
    private float SpawnObstacleLogic(float posX)
    {
        if (obstacles == null || obstacles.Count == 0) return 0;
        ObstacleData obsData = obstacles[Random.Range(0, obstacles.Count)];

        float prefabY = obsData.prefab.transform.position.y;
        Vector3 spawnPos = new Vector3(posX, groundY + prefabY, 0);

        GameObject obsObj = Instantiate(obsData.prefab, spawnPos, Quaternion.identity);
        RegisterObject(obsObj, true); // Đăng ký quản lý

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
                for (int x = 0; x < 3; x++) for (int y = 0; y < 3; y++) localPoints.Add(new Vector2(x, y - 1));
                patternWidth = 3 * itemSpacing;
                break;
            case ItemPattern.Wave:
                for (int i = 0; i < 8; i++) localPoints.Add(new Vector2(i, Mathf.Sin(i * 0.8f) * 1.5f));
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
                for (int x = 0; x < rw; x++) for (int y = 0; y < rh; y++) if (x == 0 || x == rw - 1 || y == 0 || y == rh - 1) localPoints.Add(new Vector2(x, y - (rh - 1) / 2f));
                patternWidth = rw * itemSpacing;
                break;
            case ItemPattern.RectVertical:
                int vw = Random.Range(2, 4); int vh = Random.Range(3, 6);
                for (int x = 0; x < vw; x++) for (int y = 0; y < vh; y++) if (x == 0 || x == vw - 1 || y == 0 || y == vh - 1) localPoints.Add(new Vector2(x, y - (vh - 1) / 2f));
                patternWidth = vw * itemSpacing;
                break;
            case ItemPattern.RectHorizontal:
                int hw = Random.Range(3, 6); int hh = Random.Range(2, 4);
                for (int x = 0; x < hw; x++) for (int y = 0; y < hh; y++) if (x == 0 || x == hw - 1 || y == 0 || y == hh - 1) localPoints.Add(new Vector2(x, y - (hh - 1) / 2f));
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

    // --- HỆ THỐNG QUẢN LÝ OBJECT ---
    private void RegisterObject(GameObject obj, bool isObstacle)
    {
        // Thêm vào hàng đợi để xóa sau này
        activeObjects.Enqueue(obj);

        // Gán cha để Hierarchy gọn gàng
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
            RegisterObject(item, false); // Đăng ký quản lý

            Collectible col = item.GetComponent<Collectible>();
            if (col != null) col.Init(data.scoreValue);
        }
    }

    // --- CÁC HÀM PHỤ TRỢ (GIỮ NGUYÊN) ---
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
}
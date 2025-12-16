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

    [Header("Physics & Detection (QUAN TRỌNG)")]
    [Tooltip("Layer của mặt đất (Bắt buộc phải set đúng để Raycast nhận diện hố)")]
    public LayerMask groundLayer;
    [Tooltip("Layer của vật cản để item né tránh (Smart Lift)")]
    public LayerMask obstacleLayer;
    public float checkRadius = 0.4f;

    [Header("Spacing & Logic")]
    public float minGap = 6f;
    public float maxGap = 10f;
    [Range(0, 100)] public int chanceToSpawnObstacle = 40;
    [Range(0, 100)] public int chanceItemOnObstacle = 70;
    public float itemSpacing = 1.0f;

    [Header("Pit Features (Hố & Cung Tiền)")]
    [Tooltip("Chiều cao của cung tiền khi bắc qua hố")]
    public float archHeight = 4.0f;
    [Tooltip("Độ rộng tối thiểu của hố để tạo cung tiền")]
    public float minPitWidthForArch = 2.0f;

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

        // Khởi tạo vị trí spawn bắt đầu
        if (playerTransform != null)
            currentSpawnX = playerTransform.position.x + 10f;
        else
            currentSpawnX = 0f;

        // Pre-warm: Spawn trước một đoạn dài
        SpawnChunk();
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // 1. SPAWN: Nếu nhân vật chạy gần đến điểm spawn hiện tại -> Spawn tiếp
        if (playerTransform.position.x > currentSpawnX - spawnDistanceAhead)
        {
            SpawnSmartGroup();
        }

        // 2. DESPAWN: Xóa vật thể phía sau lưng
        RemoveOldObjects();
    }

    // --- HÀM KHỞI TẠO ĐẦU GAME ---
    private void SpawnChunk()
    {
        for (int i = 0; i < 10; i++)
        {
            SpawnSmartGroup();
        }
    }

    // --- CORE LOGIC: SPAWN THÔNG MINH (CHECK HỐ) ---
    private void SpawnSmartGroup()
    {
        // Bước 1: Kiểm tra xem vị trí hiện tại có đất không?
        bool isGroundHere = IsGroundAt(currentSpawnX);

        if (!isGroundHere)
        {
            // === PHÁT HIỆN HỐ (PIT) ===
            float pitWidth = GetPitWidth(currentSpawnX);

            // Nếu hố đủ rộng -> Tạo Cung Tiền (Coin Arch)
            if (pitWidth >= minPitWidthForArch)
            {
                SpawnCoinArch(currentSpawnX, pitWidth);
            }

            // Dời điểm spawn sang bờ bên kia của hố (+ đệm an toàn 1m)
            currentSpawnX += pitWidth + 1f;
        }
        else
        {
            // === CÓ ĐẤT (GROUND) ===
            // Cộng khoảng cách nghỉ ngẫu nhiên
            float gap = Random.Range(minGap, maxGap);
            currentSpawnX += gap;

            // Kiểm tra lại sau khi cộng gap, lỡ cộng xong lại rơi xuống hố tiếp theo
            if (!IsGroundAt(currentSpawnX)) return;

            bool spawnObstacle = Random.Range(0, 100) < chanceToSpawnObstacle;
            float addedWidth = 0;

            if (spawnObstacle)
            {
                // Lấy random một vật cản
                ObstacleData obsToSpawn = GetRandomObstacle();

                // Kiểm tra an toàn: Đất có đủ rộng để đặt vật cản này không?
                if (obsToSpawn != null && IsSafeToSpawnObstacle(currentSpawnX, obsToSpawn.width))
                {
                    addedWidth = SpawnObstacleLogic(currentSpawnX, obsToSpawn);
                }
                else
                {
                    // Không an toàn (sắp hết đất) -> Chuyển sang spawn Item thôi
                    addedWidth = SpawnItemPatternLogic(currentSpawnX);
                }
            }
            else
            {
                addedWidth = SpawnItemPatternLogic(currentSpawnX);
            }

            // Cập nhật vị trí con trỏ X
            currentSpawnX += addedWidth;
        }
    }

    // --- TÍNH NĂNG MỚI: CUNG TIỀN QUA HỐ (PARABOLA) ---
    private void SpawnCoinArch(float startX, float width)
    {
        // Tính số lượng coin dựa trên độ rộng (mỗi mét 1 coin)
        int coinCount = Mathf.FloorToInt(width / itemSpacing);
        if (coinCount < 3) coinCount = 3; // Tối thiểu 3 coin cho đẹp

        float startY = groundY + 1.0f; // Bắt đầu cao hơn mặt đất 1 chút

        for (int i = 0; i <= coinCount; i++)
        {
            // t chạy từ 0 đến 1
            float t = (float)i / coinCount;

            // Công thức Parabol: y = 4 * h * (t - t^2)
            float yOffset = 4 * archHeight * (t - (t * t));

            float posX = startX + (t * width);
            float posY = startY + yOffset;

            SpawnItem(new Vector3(posX, posY, 0));
        }
    }

    // --- CÁC HÀM KIỂM TRA ĐỊA HÌNH (RAYCAST) ---
    private bool IsGroundAt(float xPos)
    {
        // Bắn tia từ trên cao (groundY + 10) xuống dưới
        // Raycast dài 20 đơn vị để chắc chắn chạm đất nếu có
        return Physics2D.Raycast(new Vector2(xPos, groundY + 10f), Vector2.down, 20f, groundLayer);
    }

    private float GetPitWidth(float startX)
    {
        float checkX = startX;
        float step = 0.5f; // Độ phân giải dò tìm (0.5m)
        float maxDist = 30f; // Giới hạn độ rộng hố tối đa để tránh vòng lặp vô tận
        float dist = 0;

        while (dist < maxDist)
        {
            if (IsGroundAt(checkX))
            {
                return dist; // Đã tìm thấy bờ bên kia
            }
            checkX += step;
            dist += step;
        }
        return dist; // Hố quá to hoặc không tìm thấy bờ
    }

    private bool IsSafeToSpawnObstacle(float startX, float width)
    {
        // Kiểm tra điểm đầu và điểm cuối của vật cản xem có đất không
        return IsGroundAt(startX) && IsGroundAt(startX + width);
    }

    // --- LOGIC CŨ: SPAWN OBSTACLE ---
    private float SpawnObstacleLogic(float posX, ObstacleData obsData)
    {
        float prefabY = obsData.prefab.transform.position.y;
        Vector3 spawnPos = new Vector3(posX, groundY + prefabY, 0);

        GameObject obsObj = Instantiate(obsData.prefab, spawnPos, Quaternion.identity);
        RegisterObject(obsObj, true);

        // Logic spawn item trên đầu vật cản (nếu có)
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

    // --- LOGIC CŨ: SPAWN ITEM PATTERNS ---
    private float SpawnItemPatternLogic(float startX)
    {
        ItemPattern pattern = (ItemPattern)Random.Range(0, System.Enum.GetValues(typeof(ItemPattern)).Length);
        List<Vector2> localPoints = new List<Vector2>();
        float patternWidth = 0;

        // Tái tạo lại logic switch case cũ
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

        // Tính toán nâng Item lên nếu bị trùng vật cản (Smart Lift)
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
        // Giới hạn không cho item spawn dưới lòng đất
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

    // --- CÁC HÀM PHỤ TRỢ ---
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
}
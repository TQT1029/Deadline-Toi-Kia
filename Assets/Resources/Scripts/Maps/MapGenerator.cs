using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance;
    private void Awake() => Instance = this;

    [Header("References")]
    public Transform basePlatformObjs;
    public Transform obstacleObjs;
    public Transform miniPlatformObjs;
    public LayerMask obstacleLayer;

    [Header("Libraries")]
    public List<BasePlatformData> baseLibrary;
    public List<ObstacleData> obstacleLibrary;
    public List<MiniPlatformData> miniPlatformLibrary;

    // [MỚI] Túi tráo bài cho MiniPlatform
    private RandomUtilities.ShuffleBag<MiniPlatformData> miniPlatformBag;

    [Header("Settings")]
    [SerializeField] private float groundY = -2f;
    [SerializeField] private float pitY = -7f;
    [Range(0, 100)] public int pitChance = 30;
    public float minPitWidth = 3f, maxPitWidth = 6f;

    [Header("Segment Logic")]
    [SerializeField] private float maxGroundSegmentLength = 75f;

    [Space]
    [SerializeField, Range(0, 100)] private int ratioObstacleToAerial = 70;

    [Header("Obstacle Logic")]
    [Range(0, 100)] public int obstacleChance = 60;
    [SerializeField] private float obstacleEdgePadding = 2f;
    [SerializeField] private float minObstacleGap = 7f;
    [SerializeField] private float maxObstacleGap = 12f;

    [Header("Mini Platform Bridge")]
    [SerializeField] private float minBridgeHeight = -1f;
    [SerializeField] private float maxBridgeHeight = 2f;
    [SerializeField] private float minGapBridge = 0.5f;
    [SerializeField] private float maxGapBridge = 1.5f;

    [Header("Mini Platform Aerial")]
    [SerializeField] private float aerialHeight = 3f;
    [SerializeField] private float minAerialHeight = -1f;
    [SerializeField] private float maxAerialHeight = 3f;
    [SerializeField] private float minGapAerial = 1f;
    [SerializeField] private float maxGapAerial = 3f;
    [SerializeField] private float maxHeightMap = 10f;

    // [MỚI] Tham số Noise để tạo độ cao tự nhiên
    [Header("Natural Randomness")]
    [Tooltip("Độ 'gắt' của địa hình. Thấp (0.1) = đồi thoải, Cao (0.5) = núi nhọn.")]
    [SerializeField] private float terrainRoughness = 0.15f;
    private float noiseOffsetX; // Để mỗi lần chơi là một map khác nhau

    private float currentGroundStart = 0f;

    // Giá trị biên cuối cùng đã được "populate" (có Obstacle/Platform)
    public float LastPopulatedEdge { get; private set; } = 0f;

    private void Start()
    {
        // Khởi tạo túi tráo bài
        if (miniPlatformLibrary != null && miniPlatformLibrary.Count > 0)
            miniPlatformBag = new RandomUtilities.ShuffleBag<MiniPlatformData>(miniPlatformLibrary);

        // Random offset cho noise
        noiseOffsetX = Random.Range(0, 10000);
        Debug.Log($"[MapGenerator] Noise Offset X: {noiseOffsetX}");
    }

    public float SpawnNextSegment(float currentX)
    {
        if (currentX == 0 && currentGroundStart == 0) currentGroundStart = 0;

        if (RandomUtilities.ChancePercent(pitChance))
        {
            return GeneratePit(currentX);
        }
        else
        {
            // --- TẠO ĐẤT ---
            return GenerateGround(currentX);
        }
    }

    private float GeneratePit(float currentX)
    {
        //--- Tạo Hố ---
        if (currentX > currentGroundStart)
        {
            PopulateSegment(currentGroundStart, currentX);
            // Debug.Log($"[MapGenerator] Đoạn đất xong: {currentX - currentGroundStart}m");
        }

        float pitWidth = RandomUtilities.RandomWithSteps(minPitWidth, maxPitWidth, 0.5f);
        float endPitX = currentX + pitWidth;

        if (pitWidth > 15)
            SpawnBridge(currentX, endPitX);
        else
            SpawnObstaclesOnPit(currentX, endPitX, pitY);
        ItemGenerator.Instance.GenerateItems(currentX, endPitX);

        LastPopulatedEdge = currentX;
        currentGroundStart = endPitX;



        return endPitX;
    }

    private float GenerateGround(float currentX)
    {
        BasePlatformData data = baseLibrary[Random.Range(0, baseLibrary.Count)];
        float estimatedLen = data.GetLength();
        Vector3 pos = new Vector3(currentX + estimatedLen / 2f, groundY, 0);

        GameObject obj = Instantiate(data.prefab, pos, Quaternion.identity, basePlatformObjs);

        float actualLen = estimatedLen;
        var col = obj.GetComponent<BoxCollider2D>();
        if (col != null) actualLen = col.size.x * obj.transform.localScale.x;

        if (Mathf.Abs(actualLen - estimatedLen) > 0.01f)
            obj.transform.position = new Vector3(currentX + actualLen / 2f, groundY, 0);

        float segmentEnd = currentX + actualLen;

        if (segmentEnd - currentGroundStart > maxGroundSegmentLength)
        {
            PopulateSegment(currentGroundStart, segmentEnd);
            LastPopulatedEdge = segmentEnd;
            currentGroundStart = segmentEnd;

            return GeneratePit(segmentEnd);
        }

        return segmentEnd;
    }

    private void PopulateSegment(float startX, float endX)
    {
        bool spawnObstacle = RandomUtilities.ChancePercent(ratioObstacleToAerial);

        if (spawnObstacle)
        {
            SpawnObstaclesOnSegment(startX, endX);
        }
        else
        {
            SpawnAerialOnSegment(startX, endX);
        }
        Physics2D.SyncTransforms();

        ItemGenerator.Instance.GenerateItems(startX, endX);

    }

    private void SpawnObstaclesOnSegment(float startX, float endX)
    {
        float currentX = startX + obstacleEdgePadding + RandomUtilities.RandomWithSteps(minObstacleGap, maxObstacleGap);
        float limitX = endX - obstacleEdgePadding;

        while (currentX < limitX)
        {
            if (RandomUtilities.ChancePercent(obstacleChance))
            {
                ObstacleData obs = obstacleLibrary[Random.Range(0, obstacleLibrary.Count)];
                Vector2 size = obs.GetSize();

                if (currentX + size.x <= limitX)
                {
                    Vector3 pos = new Vector3(currentX + size.x / 2f, groundY, 0);
                    Instantiate(obs.prefab, pos, Quaternion.identity, obstacleObjs);
                    currentX += size.x;
                }
            }
            currentX += RandomUtilities.RandomWithSteps(minObstacleGap, maxObstacleGap, 0.5f);
        }
    }
    private void SpawnObstaclesOnPit(float startX, float endX, float pitY)
    {
        float currentX = startX + obstacleEdgePadding / 2;
        float limitX = endX - obstacleEdgePadding / 2;

        // [FIX] Nếu hố quá nhỏ để dùng logic Gap thông thường, chuyển sang chế độ "Spawn 1 cái ở giữa"
        ObstacleData obs = obstacleLibrary[Random.Range(0, obstacleLibrary.Count)];

        // Chỉ spawn nếu vật cản nằm lọt trong hố
        if (obs.GetSize().x <= (limitX - currentX) + 1f) // +1f du di một chút
        {
            // Đặt ngay chính giữa hố
            Vector3 pos = new Vector3((startX + endX) / 2f, pitY, 0);
            Instantiate(obs.prefab, pos, Quaternion.identity, obstacleObjs);
        }
    }
    // --- LOGIC ĐÃ NÂNG CẤP: Dùng Perlin Noise & Shuffle Bag ---
    private void SpawnAerialOnSegment(float startX, float endX)
    {
        if (miniPlatformBag == null) return;

        float currentX = startX + RandomUtilities.RandomWithSteps(2f, 4f, 0.5f);
        float limitX = endX - 2f;

        while (currentX < limitX)
        {
            // 1. [TỐI ƯU] Dùng túi tráo bài để lấy data (tránh lặp lại 1 kiểu liên tục)
            MiniPlatformData data = miniPlatformBag.Next();
            float len = data.GetLength();

            // Nếu tấm lấy ra quá dài, thử lấy tấm khác từ túi (tối đa 3 lần)
            int attempts = 0;
            while (currentX + len > limitX && attempts < 3)
            {
                data = miniPlatformBag.Next(); // Rút tấm khác
                len = data.GetLength();
                attempts++;
            }
            if (currentX + len > limitX) break; // Hết cách, dừng lại

            // [THAY ĐỔI Ở ĐÂY] Sử dụng GetDynamicWaveHeight thay vì RandomWithSteps
            // - currentX + noiseOffsetX: Để đảm bảo vị trí X liên tục tạo ra sóng liên tục
            // - terrainRoughness: Độ gắt
            // - minAerialHeight / maxAerialHeight: Biên độ cực đại
            float noiseHeight = RandomUtilities.GetPerlinHeight(
                currentX + noiseOffsetX,
                terrainRoughness,   // Tần số sóng
                minAerialHeight,    // Đáy sóng (ví dụ -2)
                maxAerialHeight,    // Đỉnh sóng (ví dụ +5)
                2f                // Step (bước nhảy độ cao, ví dụ mỗi 1 mét)
            );

            float targetY = groundY + aerialHeight + noiseHeight;
            targetY = Mathf.Clamp(targetY, groundY + 2f, maxHeightMap);

            Vector3 pos = new Vector3(currentX + len / 2f, targetY, 0);

            // Check va chạm Obstacle
            Collider2D hit = Physics2D.OverlapBox(pos, new Vector2(len + 0.5f, 3f), 0, obstacleLayer);

            if (hit == null)
            {
                Instantiate(data.prefab, pos, Quaternion.identity, miniPlatformObjs);
            }
            else
            {
                // Nếu đụng Obstacle -> Nâng lên trên đỉnh nó
                float newY = hit.bounds.max.y + 2.5f;
                if (newY < maxHeightMap)
                {
                    pos.y = newY;
                    Instantiate(data.prefab, pos, Quaternion.identity, miniPlatformObjs);
                }
            }

            // Tịnh tiến X
            currentX += len + RandomUtilities.RandomWithSteps(minGapAerial, maxGapAerial, 1.5f);
        }
    }

    private void SpawnBridge(float startX, float endX)
    {
        float currentX = startX + 0.5f;
        float limit = endX - 0.5f;

        while (currentX < limit)
        {
            float remainingSpace = limit - currentX;

            // Lọc các tấm phù hợp
            List<MiniPlatformData> validCandidates = new List<MiniPlatformData>();
            foreach (var p in miniPlatformLibrary)
            {
                if (p.GetLength() <= remainingSpace) validCandidates.Add(p);
            }

            if (validCandidates.Count == 0) break;

            // Chọn ngẫu nhiên từ danh sách hợp lệ
            MiniPlatformData selectedData = validCandidates[Random.Range(0, validCandidates.Count)];
            float len = selectedData.GetLength();

            // [TỐI ƯU] Cầu cũng dùng Perlin Noise để có độ nhấp nhô nhẹ
            float noiseHeight = RandomUtilities.GetPerlinHeight(
                currentX + noiseOffsetX,
                terrainRoughness, // Cầu gồ ghề hơn chút
                minBridgeHeight,
                maxBridgeHeight,
                2f
            );

            Vector3 pos = new Vector3(currentX + len / 2f, groundY + noiseHeight, 0);
            Instantiate(selectedData.prefab, pos, Quaternion.identity, miniPlatformObjs);

            currentX += len + RandomUtilities.RandomWithSteps(minGapBridge, maxGapBridge, 0.5f);
        }
    }
}
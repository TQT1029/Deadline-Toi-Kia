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

    [Header("Settings")]
    public float groundY = -2f;
    [Range(0, 100)] public int pitChance = 30;
    public float minPitWidth = 3f, maxPitWidth = 6f;

    [Header("Segment Logic")]
    [Tooltip("Độ dài tối đa của một đoạn đất trước khi buộc phải spawn vật cản")]
    [SerializeField] private float maxGroundSegmentLength = 75f;

    [Space]
    [Tooltip("Tỷ lệ phân bổ Vật cản (Obstacle) so với Sàn bay (Aerial)")]
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
    [Range(0, 100)] public int miniPlatformChance = 50;
    public float aerialHeight = 3f;
    [SerializeField] private float minAerialHeight = -1f; // Chênh lệch tối thiểu
    [SerializeField] private float maxAerialHeight = 2f;  // Chênh lệch tối đa
    [SerializeField] private float minGapAerial = 1f;
    [SerializeField] private float maxGapAerial = 3f;
    [SerializeField] private int minAerialCount = 3;
    [SerializeField] private int maxAerialCount = 8;
    [SerializeField] private float maxHeightMap = 10f;

    private float currentGroundStart = 0f;
    public float LastPopulatedEdge { get; private set; } = 0f;

    public float SpawnNextSegment(float currentX)
    {
        if (currentX == 0 && currentGroundStart == 0) currentGroundStart = 0;

        bool createPit = Random.Range(0, 100) < pitChance;

        if (createPit)
        {
            // --- GẶP HỐ: CHỐT SỔ ---
            if (currentX > currentGroundStart)
            {
                PopulateSegment(currentGroundStart, currentX);
                Debug.Log($"[MapGenerator] Đoạn đất xong: {currentX - currentGroundStart}m");
            }

            // Tạo Hố & Cầu
            float pitWidth = RandomUtilities.RandomWithSteps(minPitWidth, maxPitWidth, 0.5f);
            float endPitX = currentX + pitWidth;

            SpawnBridge(currentX, endPitX);

            LastPopulatedEdge = endPitX;
            currentGroundStart = endPitX;

            return endPitX;
        }
        else
        {
            // --- TẠO ĐẤT ---
            BasePlatformData data = baseLibrary[Random.Range(0, baseLibrary.Count)];
            float estimatedLen = data.GetLength();
            Vector3 pos = new Vector3(currentX + estimatedLen / 2f, groundY, 0);

            GameObject obj = Instantiate(data.prefab, pos, Quaternion.identity, basePlatformObjs);

            // Tính lại length thực tế
            float actualLen = estimatedLen;
            var col = obj.GetComponent<BoxCollider2D>();
            if (col != null) actualLen = col.size.x * obj.transform.localScale.x;

            if (Mathf.Abs(actualLen - estimatedLen) > 0.01f)
                obj.transform.position = new Vector3(currentX + actualLen / 2f, groundY, 0);

            float segmentEnd = currentX + actualLen;

            // Nếu đất quá dài -> Buộc phải spawn
            if (segmentEnd - currentGroundStart >= maxGroundSegmentLength)
            {
                PopulateSegment(currentGroundStart, segmentEnd);
                LastPopulatedEdge = segmentEnd;
                currentGroundStart = segmentEnd;
            }

            return segmentEnd;
        }
    }

    // --- REFACTORED LOGIC ---

    private void PopulateSegment(float startX, float endX)
    {
        // Random chọn 1 trong 2 loại dựa trên tỷ lệ
        // (Giả sử bạn đã có hàm ChancePercent trong RandomUtilities như bạn nói)
        bool spawnObstacle = RandomUtilities.ChancePercent(ratioObstacleToAerial);

        if (spawnObstacle)
        {
            SpawnObstaclesOnSegment(startX, endX);
        }
        else
        {
            SpawnAerialOnSegment(startX, endX);
        }

        // Cập nhật vật lý để ItemGenerator thấy
        Physics2D.SyncTransforms();
    }

    private void SpawnObstaclesOnSegment(float startX, float endX)
    {
        float currentX = startX + obstacleEdgePadding + RandomUtilities.RandomWithSteps(minObstacleGap, maxObstacleGap);
        float limitX = endX - obstacleEdgePadding;

        while (currentX < limitX)
        {
            if (Random.Range(0, 100) < obstacleChance)
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

    // Đã Refactor gọn gàng, loại bỏ tính toán dư thừa và lặp code
    private void SpawnAerialOnSegment(float startX, float endX)
    {
        if (Random.Range(0, 100) >= miniPlatformChance) return;

        int count = Random.Range(minAerialCount, maxAerialCount + 1);
        float currentX = startX + RandomUtilities.RandomWithSteps(2f, 5f, 0.5f);
        float currentY = groundY + aerialHeight; // Độ cao bắt đầu

        for (int i = 0; i < count; i++)
        {
            MiniPlatformData data = miniPlatformLibrary[Random.Range(0, miniPlatformLibrary.Count)];
            float len = data.GetLength();

            // 1. Kiểm tra biên: Nếu hết đất thì dừng luôn
            if (currentX + len > endX - 1f) break;

            Vector3 spawnPos = new Vector3(currentX + len / 2f, currentY, 0);

            // 2. Kiểm tra va chạm với Obstacle (nếu lỡ có)
            Collider2D hit = Physics2D.OverlapBox(spawnPos, new Vector2(len + 1f, 10f), 0, obstacleLayer);

            if (hit != null)
            {
                // Logic né Obstacle: Đặt cao lên trên đầu nó
                float obstacleTop = hit.bounds.max.y;
                spawnPos.y = obstacleTop + 2.5f;
            }

            // 3. Kiểm tra trần map và Spawn
            if (spawnPos.y < maxHeightMap)
            {
                Instantiate(data.prefab, spawnPos, Quaternion.identity, miniPlatformObjs);

                // [GIẢI ĐÁP] Tại sao thẳng hàng?
                // Dòng dưới đây quyết định độ cao tiếp theo.
                // Nếu bạn muốn nó nhấp nhô nhiều, hãy tăng range của minAerialHeight/maxAerialHeight trong Inspector.
                // Nếu bạn muốn nó ngẫu nhiên hoàn toàn (không phụ thuộc tấm trước), hãy dùng logic khác (xem bên dưới).
                currentY = spawnPos.y + RandomUtilities.RandomWithSteps(minAerialHeight, maxAerialHeight, 0.5f);

                // Di chuyển tới vị trí tiếp theo
                currentX += len + RandomUtilities.RandomWithSteps(minGapAerial, maxGapAerial, 0.5f);
            }
            else
            {
                // Nếu quá cao thì bỏ qua tấm này, dời X đi tiếp để tìm chỗ khác
                currentX += len + 2f;
            }
        }
    }

    private void SpawnBridge(float startX, float endX)
    {
        float currentX = startX + 0.5f;
        float limit = endX - 0.5f;
        float lastY = groundY;
        int bridgeAttempts = 0;

        while (currentX < limit)
        {
            MiniPlatformData data = miniPlatformLibrary[Random.Range(bridgeAttempts, miniPlatformLibrary.Count)];
            float len = data.GetLength();

            // Logic chọn tấm cầu phù hợp
            while (currentX + len > limit && bridgeAttempts < miniPlatformLibrary.Count - 1)
            {
                bridgeAttempts++;
                data = miniPlatformLibrary[Random.Range(bridgeAttempts, miniPlatformLibrary.Count)];
                len = data.GetLength();
                if (currentX + len > limit && bridgeAttempts == miniPlatformLibrary.Count) return;
            }

            float nextY = lastY + RandomUtilities.RandomWithSteps(minBridgeHeight, maxBridgeHeight, 0.5f);
            Vector3 pos = new Vector3(currentX + len / 2f, nextY, 0);

            Instantiate(data.prefab, pos, Quaternion.identity, miniPlatformObjs);

            lastY = pos.y;
            currentX += len + RandomUtilities.RandomWithSteps(minGapBridge, maxGapBridge, 1);
        }
    }
}
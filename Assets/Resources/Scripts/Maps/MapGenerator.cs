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

    [Header("Obstacle Logic")]
    [Range(0, 100)] public int obstacleChance = 60;
    [SerializeField] private float obstacleEdgePadding = 2f;
    [SerializeField] private float minObstacleGap = 7f;
    [SerializeField] private float maxObstacleGap = 12f;

    [Header("Mini Platform Bridge")]
    [Tooltip("Độ cao so với platform trước đó")]
    [SerializeField] private float minBridgeHeight = -1f;
    [SerializeField] private float maxBridgeHeight = 2f;
    [SerializeField] private float minGapBridge = 0.5f;
    [SerializeField] private float maxGapBridge = 1.5f;

    [Header("Mini Platform Aerial")]
    [Range(0, 100)] public int miniPlatformChance = 50;
    public float aerialHeight = 3f;
    [SerializeField] private float minAerialHeight = -1f;
    [SerializeField] private float maxAerialHeight = 2f;
    [SerializeField] private float minGapAerial = 1f;
    [SerializeField] private float maxGapAerial = 2f;
    [SerializeField] private int minAerialCount = 3;
    [SerializeField] private int maxAerialCount = 6;
    [SerializeField] private float maxHeightMap = 10f;

    [Space]
    [Tooltip("Tỷ lệ phân bổ Vật cản và Sàn bay trong một đoạn đất")]
    [SerializeField, Range(0, 100)] private int ratioObstacleToAerial = 70;// tỉ lệ vật cản và sàn bay

    private float currentGroundStart = 0f;
    public float LastPopulatedEdge { get; private set; } = 0f;

    public float SpawnNextSegment(float currentX)
    {
        if (currentX == 0 && currentGroundStart == 0) currentGroundStart = 0;

        bool createPit = Random.Range(0, 100) < pitChance;

        if (createPit)
        {
            // --- GẶP HỐ: CHỐT SỔ ĐOẠN ĐẤT CŨ ---
            if (currentX > currentGroundStart)
            {
                PopulateSegment(currentGroundStart, currentX);

                Debug.Log($"[MapGnerator] Độ dài tích luỹ đoạn đất khi gặp hố {currentX - currentGroundStart}");
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

            // Fix size & pos
            float actualLen = estimatedLen;
            var col = obj.GetComponent<BoxCollider2D>();
            if (col != null) actualLen = col.size.x * obj.transform.localScale.x;

            if (Mathf.Abs(actualLen - estimatedLen) > 0.01f)
                obj.transform.position = new Vector3(currentX + actualLen / 2f, groundY, 0);


            float segmentEnd = currentX + actualLen;

            // Kiểm tra độ dài đất tích lũy
            // Nếu đoạn đất hiện tại (từ currentGroundStart đến segmentEnd) quá dài -> Spawn ngay
            if (segmentEnd - currentGroundStart >= maxGroundSegmentLength)
            {
                PopulateSegment(currentGroundStart, segmentEnd);

                Debug.Log($"[MapGnerator] Độ dài tích luỹ đoạn đất khi quá dài {currentX - currentGroundStart}");


                LastPopulatedEdge = segmentEnd;
                currentGroundStart = segmentEnd; // Reset điểm bắt đầu cho đoạn tiếp theo
            }

            return segmentEnd;
        }
    }

    // Hàm chung để sinh vật cản và sàn bay cho một đoạn đất
    private void PopulateSegment(float startX, float endX)
    {

        if (RandomUtilities.ChancePercent(ratioObstacleToAerial))
        {
            SpawnObstaclesOnSegment(startX, endX);
            Physics2D.SyncTransforms();
        }
        else
        {
            SpawnAerialOnSegment(startX, endX);
            Physics2D.SyncTransforms();
        }

    }

    private void SpawnObstaclesOnSegment(float startX, float endX)
    {
        float currentX = startX + obstacleEdgePadding + RandomUtilities.RandomWithSteps(minObstacleGap, maxObstacleGap);
        float limitX = endX - obstacleEdgePadding - RandomUtilities.RandomWithSteps(minObstacleGap, maxObstacleGap);

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

    private void SpawnAerialOnSegment(float startX, float endX)
    {
        int count = Random.Range(minAerialCount, maxAerialCount + 1);

        float minAvailableWidth = miniPlatformLibrary[miniPlatformLibrary.Count - 1].GetLength() * minAerialCount;
        float maxAvailableWidth = miniPlatformLibrary[0].GetLength() * maxAerialCount;
        float segmentWidth = endX - startX;

        if (minAvailableWidth < segmentWidth && segmentWidth < maxAvailableWidth)
        {
            if (Random.Range(0, 100) < miniPlatformChance)
            {

                float currentX = startX + RandomUtilities.RandomWithSteps(2f, 5f, 0.5f);
                float lastY = groundY + aerialHeight;

                for (int i = 0; i < count; i++)
                {
                    MiniPlatformData data = miniPlatformLibrary[Random.Range(0, miniPlatformLibrary.Count)];
                    float len = data.GetLength();

                    if (currentX + len > endX - 1f) break;

                    Vector3 pos = new Vector3(currentX + len / 2f, lastY, 0);

                    // Check va chạm Obstacle
                    // Dùng size Y lớn (10f) để quét toàn bộ chiều cao xem có vướng gì không
                    Collider2D hit = Physics2D.OverlapBox(pos, new Vector2(len + 1f, 10f), 0, obstacleLayer);

                    if (hit == null)
                    {
                        Instantiate(data.prefab, pos, Quaternion.identity, miniPlatformObjs);

                        // Chuẩn bị cho tấm tiếp theo
                        lastY = pos.y + RandomUtilities.RandomWithSteps(minAerialHeight, maxAerialHeight, 0.5f);
                        currentX += len + RandomUtilities.RandomWithSteps(minGapAerial, maxGapAerial, 0.5f);
                    }
                    else
                    {
                        // Vướng Obstacle -> Đặt cao lên trên đầu nó
                        // Cập nhật lại Y
                        float newY = hit.bounds.max.y + 2.5f;

                        // Chỉ cần kiểm tra độ cao trần
                        if (newY < maxHeightMap)
                        {
                            pos.y = newY;
                            Instantiate(data.prefab, pos, Quaternion.identity, miniPlatformObjs);

                            lastY = pos.y + RandomUtilities.RandomWithSteps(minAerialHeight, maxAerialHeight, 0.5f);
                            currentX += len + RandomUtilities.RandomWithSteps(minGapAerial, maxGapAerial, 0.5f);
                        }
                        else
                        {
                            // Nếu quá cao -> Bỏ qua tấm này, thử dời X ra xa hơn để tìm chỗ trống khác
                            currentX += len + 2f;
                        }
                    }
                }
            }
        }
        else SpawnObstaclesOnSegment(startX, endX);
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

            // Đảm bảo cầu không vượt quá giới hạn hố
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
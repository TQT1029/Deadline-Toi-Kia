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

    // --- LOGIC SINH SÀN BAY (ĐÃ TỐI ƯU) ---
    private void SpawnAerialOnSegment(float startX, float endX)
    {
        // Kiểm tra tỉ lệ xuất hiện
        if (Random.Range(0, 100) >= miniPlatformChance) return;
        if (miniPlatformLibrary == null || miniPlatformLibrary.Count == 0) return;

        // Bắt đầu cách mép trái một chút
        float currentX = startX + RandomUtilities.RandomWithSteps(2f, 4f, 0.5f);
        // Điểm dừng an toàn cách mép phải
        float limitX = endX - 2f;

        float lastY = groundY + aerialHeight;

        // Vòng lặp chạy xuyên suốt chiều dài đoạn đất
        while (currentX < limitX)
        {
            // 1. CHỌN TẤM TỐI ƯU (Tránh lòi ra ngoài)
            MiniPlatformData data = miniPlatformLibrary[Random.Range(0, miniPlatformLibrary.Count)];
            float len = data.GetLength();
            int attempts = 0;

            // Nếu tấm này dài quá so với đất còn lại, thử chọn tấm khác nhỏ hơn
            // Thử tối đa 10 lần để tránh treo vòng lặp vô tận
            while (currentX + len > limitX && attempts < 10)
            {
                data = miniPlatformLibrary[Random.Range(0, miniPlatformLibrary.Count)];
                len = data.GetLength();
                attempts++;
            }

            // Nếu sau khi thử mà vẫn không vừa, nghĩa là hết đất -> Dừng luôn
            if (currentX + len > limitX) break;

            // 2. TÍNH VỊ TRÍ & CHECK VA CHẠM
            Vector3 pos = new Vector3(currentX + len / 2f, lastY, 0);

            // Kiểm tra xem có đụng Obstacle nào bên dưới không (quét vùng rộng hơn tấm sàn một chút)
            Collider2D hit = Physics2D.OverlapBox(pos, new Vector2(len + 0.5f, 8f), 0, obstacleLayer);

            if (hit == null)
            {
                // Không vướng -> Spawn bình thường
                Instantiate(data.prefab, pos, Quaternion.identity, miniPlatformObjs);

                // Cập nhật Y cho tấm tiếp theo (lên/xuống ngẫu nhiên)
                lastY += RandomUtilities.RandomWithSteps(minAerialHeight, maxAerialHeight, 0.25f);
            }
            else
            {
                // Vướng Obstacle -> Đặt cao lên trên đầu nó
                // Tính toán độ cao mới: Đỉnh vật cản + khoảng cách an toàn
                float newY = hit.bounds.max.y + 2.5f;

                // Chỉ spawn nếu độ cao mới vẫn nằm trong giới hạn cho phép
                if (newY < maxHeightMap)
                {
                    pos.y = newY;
                    Instantiate(data.prefab, pos, Quaternion.identity, miniPlatformObjs);

                    // Cập nhật lastY theo vị trí mới này để tấm sau nối tiếp hợp lý
                    lastY = newY + RandomUtilities.RandomWithSteps(minAerialHeight, maxAerialHeight, 0.25f);
                }
                // Nếu quá cao (vượt trần) -> Bỏ qua tấm này, không spawn, nhưng vẫn tịnh tiến X
            }

            // Đảm bảo Y không quá thấp (sát đất) hoặc quá cao
            lastY = Mathf.Clamp(lastY, groundY + 2.5f, maxHeightMap);

            // 3. TỊNH TIẾN X
            // Cộng thêm chiều dài tấm vừa xét + khoảng nghỉ ngẫu nhiên
            currentX += len + RandomUtilities.RandomWithSteps(minGapAerial, maxGapAerial, 0.5f);
        }
    }
    private void SpawnBridge(float startX, float endX)
    {
        float currentX = startX + 0.5f; // Điểm bắt đầu (có padding nhỏ)
        float limit = endX - 0.5f;      // Điểm kết thúc an toàn
        float lastY = groundY;

        while (currentX < limit)
        {
            // Tính khoảng trống còn lại
            float remainingSpace = limit - currentX;

            // 1. TÌM KIẾM CÁC TẤM PHÙ HỢP (LỌC)
            // Duyệt qua thư viện để tìm tất cả các tấm có độ dài <= khoảng trống còn lại
            List<MiniPlatformData> validCandidates = new List<MiniPlatformData>();

            foreach (var p in miniPlatformLibrary)
            {
                if (p.GetLength() <= remainingSpace)
                {
                    validCandidates.Add(p);
                }
            }

            // 2. XỬ LÝ KẾT QUẢ
            MiniPlatformData selectedData = null;

            if (validCandidates.Count > 0)
            {
                // Nếu có tấm vừa vặn -> Chọn ngẫu nhiên một tấm trong số đó
                selectedData = validCandidates[Random.Range(0, validCandidates.Count)];
            }
            else
            {
                // YÊU CẦU CỦA BẠN: Nếu không còn tấm nào vừa -> Dừng spawn ngay lập tức
                break;
            }

            // 3. SPAWN
            float len = selectedData.GetLength();
            float nextY = lastY + RandomUtilities.RandomWithSteps(minBridgeHeight, maxBridgeHeight, 0.5f);

            Vector3 pos = new Vector3(currentX + len / 2f, nextY, 0);
            Instantiate(selectedData.prefab, pos, Quaternion.identity, miniPlatformObjs);

            // Cập nhật vị trí cho lần lặp sau
            lastY = pos.y;
            currentX += len + RandomUtilities.RandomWithSteps(minGapBridge, maxGapBridge, 0.5f);
        }
    }
}
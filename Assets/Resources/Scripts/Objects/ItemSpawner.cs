using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ItemSpawner : MonoBehaviour
{
    public static ItemSpawner Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Header("Cấu hình Chung")]
    public GameObject itemPrefab;
    public LayerMask obstacleLayer;
    public float checkRadius = 0.4f; // Giảm nhỏ chút để check chính xác hơn

    [Header("Phạm vi Spawn")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("Cấu hình Độ Cao (Lanes)")]
    [Tooltip("Độ cao tối thiểu (Mặt đất)")]
    public float minSpawnY = -2f;
    [Tooltip("Khoảng cách mỗi bậc độ cao (Vd: Nhảy thấp, Nhảy cao)")]
    public float laneHeightStep = 1.5f;

    [Header("Cấu hình Nhóm (Pattern)")]
    public float distanceBetweenGroups = 12f;
    public float itemSpacing = 1.2f;

    private enum PatternType { Line, Grid, SineWave, VShape }

    [ContextMenu("Generate Items")]
    public void GenerateItems()
    {
        // Xóa sạch item cũ (Hỗ trợ cả Editor mode và Play mode)
        int childCount = transform.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying) Destroy(transform.GetChild(i).gameObject);
            else DestroyImmediate(transform.GetChild(i).gameObject);
        }

        if (startPoint == null || endPoint == null || itemPrefab == null) return;

        float currentX = startPoint.position.x;
        float endX = endPoint.position.x;

        // Lấy Y chuẩn từ StartPoint
        float baseY = startPoint.position.y;

        while (currentX < endX)
        {
            // 1. Chọn Pattern ngẫu nhiên
            PatternType pattern = (PatternType)Random.Range(0, 4);

            // 2. Chọn Lane (Làn) cố định thay vì Random.Range lung tung
            // 0 = Thấp, 1 = Trung bình, 2 = Cao
            int laneIndex = Random.Range(0, 3);
            float groupY = baseY + (laneIndex * laneHeightStep);

            // Đảm bảo không thấp hơn minSpawnY
            if (groupY < minSpawnY) groupY = minSpawnY;

            Vector2 spawnOrigin = new Vector2(currentX, groupY);

            // 3. Tính độ dài nhóm để cộng vào currentX sau này
            float groupWidth = 0f;

            switch (pattern)
            {
                case PatternType.Line:
                    int lineCount = Random.Range(3, 6);
                    SpawnLine(spawnOrigin, lineCount);
                    groupWidth = lineCount * itemSpacing;
                    break;

                case PatternType.Grid:
                    int w = Random.Range(3, 5);
                    int h = Random.Range(2, 3);
                    SpawnGrid(spawnOrigin, w, h);
                    groupWidth = w * itemSpacing;
                    break;

                case PatternType.SineWave:
                    int waveCount = Random.Range(6, 12);
                    SpawnSineWave(spawnOrigin, waveCount);
                    groupWidth = waveCount * itemSpacing;
                    break;

                case PatternType.VShape: // Hình chữ V mới
                    SpawnVShape(spawnOrigin, 5);
                    groupWidth = 5 * itemSpacing;
                    break;
            }

            // Nhảy tới vị trí tiếp theo
            currentX += groupWidth + distanceBetweenGroups;
        }
    }

    // --- CÁC HÀM SPAWN HÌNH DÁNG ---

    void SpawnLine(Vector2 origin, int count)
    {
        for (int i = 0; i < count; i++)
        {
            TrySpawnItem(new Vector2(origin.x + (i * itemSpacing), origin.y));
        }
    }

    void SpawnGrid(Vector2 origin, int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TrySpawnItem(new Vector2(origin.x + (x * itemSpacing), origin.y + (y * itemSpacing)));
            }
        }
    }

    void SpawnSineWave(Vector2 origin, int count)
    {
        float frequency = 0.5f;
        float amplitude = 1.2f;

        for (int i = 0; i < count; i++)
        {
            float xOffset = i * itemSpacing;
            // Dùng Abs để sóng chỉ đi lên (như cầu vồng) hoặc để nguyên Sin tùy thích
            float yOffset = Mathf.Sin(xOffset * frequency) * amplitude;
            TrySpawnItem(new Vector2(origin.x + xOffset, origin.y + yOffset));
        }
    }

    void SpawnVShape(Vector2 origin, int size) // Hình chữ V
    {
        for (int i = 0; i < size; i++)
        {
            float yOffset = Mathf.Abs(i - (size / 2)) * 0.8f; // Cao ở 2 đầu, thấp ở giữa
            TrySpawnItem(new Vector2(origin.x + (i * itemSpacing), origin.y + yOffset));
        }
    }

    // --- HÀM QUAN TRỌNG: SMART SPAWN ---

    void TrySpawnItem(Vector2 targetPos)
    {
        // 1. Giới hạn không cho thấp hơn mặt đất
        if (targetPos.y < minSpawnY) targetPos.y = minSpawnY;

        // 2. Kiểm tra va chạm: Nếu vướng, thử nâng lên cao hơn (Smart Adjust)
        // Thử tối đa 3 tầng độ cao (để item leo lên đầu chướng ngại vật)
        for (int i = 0; i < 3; i++)
        {
            Collider2D hit = Physics2D.OverlapCircle(targetPos, checkRadius, obstacleLayer);

            if (hit == null)
            {
                // Không vướng -> Spawn luôn
                GameObject obj = Instantiate(itemPrefab, targetPos, Quaternion.identity);
                obj.transform.SetParent(this.transform);
                return; // Spawn xong thì thoát
            }
            else
            {
                // Vướng vật cản -> Nâng độ cao lên 1 nấc rồi thử lại ở vòng lặp sau
                // Điều này giúp tạo hiệu ứng item nằm trên nóc máy giặt/bàn ghế
                targetPos.y += laneHeightStep * 0.8f;
            }
        }

        // Nếu thử 3 lần mà vẫn vướng (ví dụ chướng ngại vật quá cao) -> Bỏ qua không spawn
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, checkRadius);

        // Vẽ đường giới hạn sàn
        if (startPoint != null && endPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(startPoint.position.x, minSpawnY, 0), new Vector3(endPoint.position.x, minSpawnY, 0));
        }
    }
}
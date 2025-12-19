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
    public LayerMask obstacleLayer; // Layer của Obstacle/MiniPlatform

    [Header("Libraries")]
    public List<BasePlatformData> baseLibrary;
    public List<ObstacleData> obstacleLibrary;
    public List<MiniPlatformData> miniPlatformLibrary;

    [Header("Settings")]
    public float groundY = -2f;
    [Range(0, 100)] public int pitChance = 30;
    public float minPitWidth = 3f, maxPitWidth = 6f;

    [Header("Obstacle Logic")]
    [Range(0, 100)] public int obstacleChance = 60;
    [SerializeField] private float obstacleEdgePadding = 2f; // Cách mép đất tối thiểu
    [SerializeField] private float minObstacleGap = 7f;
    [SerializeField] private float maxObstacleGap = 12f;

    [Header("Mini Platform Bridge")]
    [Range(0, 100)] public int miniPlatformChance = 50;
    [Tooltip("độ cao so với platform trước đó")]
    [SerializeField] float minBridgeHeight = -1f; // Chiều cao khi bắc qua hố
    [SerializeField] float maxBridgeHeight = 2f; // Chiều cao khi bắc qua hố

    [Tooltip("Khoảng cách chênh lệch giữa các tấm cầu")]
    [SerializeField] private float minGapBridge = 0.5f; // Khoảng cách nhỏ giữa các miếng cầu
    [SerializeField] private float maxGapBridge = 1.5f; // Khoảng cách nhỏ giữa các miếng cầu

    [Header("Mini Platform Aerial")]
    public float aerialHeight = 3f; // Chiều cao khi bay trên đất
    [SerializeField] private float maxHeightMap = 10f;

    // Struct lưu trữ thông tin đất để dùng cho các bước sau
    public struct GroundSegment { public float startX; public float endX; }
    public struct ObstacleSegment { public float startX; public float endX; }
    public struct PitSegment { public float startX; public float endX; }

    [SerializeField] private List<GroundSegment> currentGrounds = new List<GroundSegment>();
    [SerializeField] private List<PitSegment> currentPits = new List<PitSegment>();

    // --- MAIN FUNCTION: Gọi bởi Controller ---
    public void GenerateChunk(float startX, float endX)
    {
        currentGrounds.Clear();
        currentPits.Clear();

        // BƯỚC 1: TẠO ĐẤT VÀ HỐ
        GenerateBaseLayer(startX, endX);

        // Cập nhật Physics ngay để bước sau Raycast trúng được đất
        Physics2D.SyncTransforms();

        // BƯỚC 2: TẠO OBSTACLE (Chỉ trên đất)
        GenerateObstacles();

        // Cập nhật Physics để bước MiniPlatform tránh được Obstacle
        Physics2D.SyncTransforms();

        // BƯỚC 3: TẠO MINI PLATFORM (Qua hố & Trên cao)
        GenerateMiniPlatforms();

        // Cập nhật lần cuối cho ItemGenerator dùng
        Physics2D.SyncTransforms();
    }

    // --- STEP 1: BASE LAYER ---
    private void GenerateBaseLayer(float startGen, float endGen)
    {
        float currentX = startGen;
        float startLen = 0f;
        float endLen = 0f;

        while (currentX < endGen)
        {
            // Sinh Đất
            BasePlatformData data = baseLibrary[Random.Range(0, baseLibrary.Count)];
            float len = data.GetLength();

            Vector3 pos = new Vector3(currentX + len / 2f, groundY, 0);
            GameObject obj = Instantiate(data.prefab, pos, Quaternion.identity, basePlatformObjs);

            // Fix lại collider nếu cần (để đảm bảo chuẩn xác)
            var col = obj.GetComponent<BoxCollider2D>();
            if (col)
            {
                col.size = new Vector2(len / obj.transform.localScale.x, col.size.y);
                endLen = col.bounds.max.x;
            }

            currentX += len;

            // Quyết định Sinh Hố
            if (currentX < endGen && Random.Range(0, 100) < pitChance)
            {
                //Ghi nhận độ dài tổng liên tục không có hố
                currentGrounds.Add(new GroundSegment { startX = startLen, endX = endLen });
                Debug.Log($"[MapGenerator] Độ dài mặt đất {endLen - startLen}");

                // Sinh hố
                float pitW = Random.Range(minPitWidth, maxPitWidth);
                currentPits.Add(new PitSegment { startX = currentX, endX = currentX + pitW });
                currentX += pitW;


                startLen = currentX; // Cập nhật lại startX cho đoạn đất tiếp theo
            }
        }
    }

    // --- STEP 2: OBSTACLES ---
    private void GenerateObstacles()
    {
        foreach (var ground in currentGrounds)
        {
            float currentX = ground.startX + obstacleEdgePadding * 2;
            float endLimit = ground.endX - obstacleEdgePadding * 2;

            while (currentX < endLimit)
            {
                if (Random.Range(0, 100) < obstacleChance)
                {
                    ObstacleData obs = obstacleLibrary[Random.Range(0, obstacleLibrary.Count)];
                    Vector2 size = obs.GetSize();

                    // Kiểm tra đủ chỗ không
                    if (currentX + size.x <= endLimit)
                    {
                        Vector3 pos = new Vector3(currentX + size.x / 2f, groundY, 0);
                        //Vector3 pos = new Vector3(currentX + size.x / 2f, groundY + size.y / 2f + obs.heightOffset, 0);
                        Instantiate(obs.prefab, pos, Quaternion.identity, obstacleObjs);
                        currentX += size.x; // Nhảy qua vật cản
                    }
                }
                currentX += Random.Range(minObstacleGap, maxObstacleGap);
            }
        }
    }

    // --- STEP 3: MINI PLATFORMS ---
    private void GenerateMiniPlatforms()
    {
        // Ưu tiên 1: Bắc cầu qua hố
        foreach (var pit in currentPits)
        {
            if (pit.endX - pit.startX >= 10)
                SpawnBridge(pit.startX, pit.endX);
        }

        // Ưu tiên 2: Bay trên đất (Tránh Obstacle)
        for (int i = 0; i < currentGrounds.Count; i++)
        {
            if (Random.Range(0, 100) < miniPlatformChance)
            {
                SpawnAerialPlatform(currentGrounds[i].endX, currentGrounds[Random.Range(i, currentGrounds.Count)].startX);
            }
        }
    }

    private void SpawnBridge(float startX, float endX)
    {
        float currentX = startX - 1f; // Bắt đầu sớm hơn mép hố
        float limit = endX + 1f;

        float lastY = groundY;
        while (currentX < limit)
        {
            MiniPlatformData data = miniPlatformLibrary[Random.Range(0, miniPlatformLibrary.Count)];
            float len = data.GetLength();

            // Đặt ngay tại groundY độ cao chênh lệch ngẫu nhiên so với platform trước đó
            Vector3 pos = new Vector3(currentX + len / 2f, lastY + Random.Range(minBridgeHeight, maxBridgeHeight), 0);
            Instantiate(data.prefab, pos, Quaternion.identity, miniPlatformObjs);

            lastY = pos.y;
            currentX += len + Random.Range(minGapBridge, maxGapBridge); // Gap nhỏ giữa các miếng cầu
        }
    }

    private void SpawnAerialPlatform(float startX, float endX)
    {
        // Tìm khoảng trống bằng chọn random và check Overlap
        float tryX = Random.Range(startX + 2f, endX - 2f);
        // chỉ sử dụng 2 platform ngắn
        MiniPlatformData data = miniPlatformLibrary[Random.Range(1, miniPlatformLibrary.Count)];
        float len = data.GetLength();

        Vector3 pos = new Vector3(tryX, groundY + aerialHeight, 0);

        // Kiểm tra xem vị trí này có đụng Obstacle bên dưới không
        // Dùng OverlapBox: Center=pos, Size=(len, 10)
        Collider2D hit = Physics2D.OverlapBox(pos, new Vector2(len + 1, 6f), 0, obstacleLayer);

        // Nếu không đụng hoặc đụng nhưng ta chấp nhận đặt cao hơn hẳn
        if (hit == null)
        {
            Instantiate(data.prefab, pos, Quaternion.identity, miniPlatformObjs);
        }
        else
        {
            Debug.Log($"[MapGenerator] mini platform đụng vật thể {hit.name}");
            // Nếu đụng Obstacle, thử nâng cao hơn nữa (thành bậc 2)
            pos.y = hit.bounds.max.y + 2.5f; // Cao hơn đỉnh vật cản 2.5m
            if (pos.y < maxHeightMap) // Giới hạn trần
            {
                Instantiate(data.prefab, pos, Quaternion.identity, miniPlatformObjs);
            }
        }
    }
    //--- Phụ trợ ---

}
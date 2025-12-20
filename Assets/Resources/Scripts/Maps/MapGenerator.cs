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
    public int minPitWidth = 3, maxPitWidth = 6;

    [Header("Obstacle Logic")]
    [Range(0, 100)] public int obstacleChance = 60;
    [SerializeField] private float obstacleEdgePadding = 2f; // Cách mép đất tối thiểu
    [SerializeField] private float minObstacleGap = 7f;
    [SerializeField] private float maxObstacleGap = 12f;

    [Header("Mini Platform Bridge")]

    [Tooltip("độ cao so với platform trước đó")]
    [SerializeField] private int minBridgeHeight = -1; // Chiều cao khi bắc qua hố
    [SerializeField] private int maxBridgeHeight = 2; // Chiều cao khi bắc qua hố

    [Tooltip("Khoảng cách gần/xa giữa các tấm cầu")]
    [SerializeField] private int minGapBridge = 1; // Khoảng cách nhỏ giữa các miếng cầu
    [SerializeField] private int maxGapBridge = 2; // Khoảng cách nhỏ giữa các miếng cầu

    [Header("Mini Platform Aerial")]
    [Tooltip("Xác suất spawn mini platform trên đất liền")]
    [SerializeField, Range(0, 100)] private int miniPlatformChance = 50;

    [SerializeField] private float aerialHeight = 3f; // Chiều cao khi bay trên đất
    [Tooltip("Số lượng mini platform sẽ spawn")]
    [SerializeField] private int minAerialCount = 3;
    [SerializeField] private int maxAerialCount = 6;

    [Tooltip("độ cao so với platform trước đó")]
    [SerializeField] private int minAerialHeight = -1; // Chiều cao khi bắc qua hố
    [SerializeField] private int maxAerialHeight = 2; // Chiều cao khi bắc qua hố

    [Tooltip("Khoảng cách gần/xa giữa các tấm cầu")]
    [SerializeField] private int minGapAerial = 1; // Khoảng cách nhỏ giữa các miếng cầu
    [SerializeField] private int maxGapAerial = 2; // Khoảng cách nhỏ giữa các miếng cầu



    // Struct lưu trữ thông tin đất để dùng cho các bước sau
    public struct GroundSegment { public float startX; public float endX; }

    public Dictionary<int, GroundSegment> currentGrounds = new Dictionary<int, GroundSegment>();

    public int groundIDCounter = 0;
    private float startGroundX = 0f;
    private float endGroundX = 0f;


    // --- STEP 1: BASE LAYER ---
    // Trả về vị trí X kết thúc của đoạn vừa sinh (LastMaxEdge mới)
    public float SpawnNextSegment(float currentX)
    {
        // 1. Kiểm tra xem có tạo hố không
        // (Chỉ tạo hố nếu đoạn trước không phải là hố để tránh hố liên hoàn quá khó)
        bool createPit = Random.Range(0, 100) < pitChance;

        if (createPit)
        {

            groundIDCounter++;

            // --- XỬ LÝ HỐ ---
            float pitWidth = RandomUtilities.RandomWithSteps(minPitWidth, maxPitWidth, 0.5f);
            float endX = currentX + pitWidth;



            // Bắt buộc tạo cầu bắc qua hố nếu hố đủ rộng
            if (pitWidth >= 15)
                SpawnBridge(currentX, endX);

            startGroundX = endX;
            CheckCurrentGrounds(groundIDCounter, startGroundX, endGroundX);
            return endX; // Trả về mép bên kia hố
        }
        else
        {
            // --- XỬ LÝ ĐẤT LIỀN ---
            // A. Tạo Base Platform
            BasePlatformData data = baseLibrary[Random.Range(0, baseLibrary.Count)];

            // Tính vị trí tâm để đặt
            // Lưu ý: Chúng ta chưa biết length thực tế cho đến khi Instantiate xong nếu dùng auto-size
            // Nên ta tạm lấy length từ data để tính pos, sau đó fix lại nếu cần
            float estimatedLen = data.GetLength();
            Vector3 pos = new Vector3(currentX + estimatedLen / 2f, groundY, 0);

            GameObject obj = Instantiate(data.prefab, pos, Quaternion.identity, basePlatformObjs);

            //Lấy size chuẩn xác từ object vừa sinh ra
            float actualLen = estimatedLen;
            var col = obj.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                // Cập nhật lại size collider cho khớp data (nếu cần) hoặc lấy size từ collider
                // Ở đây ta ưu tiên lấy size thực tế của collider * scale
                actualLen = col.size.x * obj.transform.localScale.x;
            }

            // Nếu actualLen khác estimatedLen, ta cần dời vị trí obj lại cho đúng mép trái
            if (Mathf.Abs(actualLen - estimatedLen) > 0.01f)
            {
                obj.transform.position = new Vector3(currentX + actualLen / 2f, groundY, 0);
            }

            float endX = currentX + actualLen;


            endGroundX = endX;
            CheckCurrentGrounds(groundIDCounter, startGroundX, endGroundX);
            return endX; // Trả về mép cuối của miếng đất
        }

    }

    // --- LOGIC PHỤ: OBSTACLE ---
    public void SpawnObstaclesOnSegment(float startX, float endX)
    {
        float currentX = startX + obstacleEdgePadding;
        float limitX = endX - obstacleEdgePadding;

        while (currentX < limitX)
        {
            if (Random.Range(0, 100) < obstacleChance)
            {
                ObstacleData obs = obstacleLibrary[Random.Range(0, obstacleLibrary.Count)];
                Vector2 size = obs.GetSize();

                // Kiểm tra đủ chỗ không
                if (currentX + size.x <= limitX)
                {
                    Vector3 pos = new Vector3(currentX + size.x / 2f, groundY, 0);
                    Instantiate(obs.prefab, pos, Quaternion.identity, obstacleObjs);
                    currentX += size.x;
                }
            }
            // Cách ra một đoạn ngẫu nhiên
            currentX += RandomUtilities.RandomWithSteps(minObstacleGap, maxObstacleGap, 0.5f);
        }
    }

    // --- LOGIC PHỤ: MINI PLATFORM ---

    // 1. Cầu qua hố
    private void SpawnBridge(float startX, float endX)
    {
        float currentX = startX + 1f; // Lùi vào trong hố một chút
        float limit = endX - 1f;
        float lastY = groundY;

        while (currentX < limit)
        {
            MiniPlatformData data = miniPlatformLibrary[Random.Range(0, miniPlatformLibrary.Count)];
            float len = data.GetLength();

            // Tính Y mới
            float nextY = lastY + RandomUtilities.RandomWithSteps(minBridgeHeight, maxBridgeHeight, 0.5f);

            Vector3 pos = new Vector3(currentX + len / 2f, nextY, 0);
            Instantiate(data.prefab, pos, Quaternion.identity, miniPlatformObjs);

            lastY = pos.y;
            currentX += len + RandomUtilities.RandomWithSteps(minGapBridge, maxGapBridge, 0.5f);
        }
    }

    // 2. Sàn bay trên đất
    public void SpawnAerialOnSegment(float startX, float endX)
    {
        if (Random.Range(0, 100) < miniPlatformChance)
        {
            int count = Random.Range(minAerialCount, maxAerialCount + 1);

            // Chọn vị trí ngẫu nhiên trên mặt đất để bắt đầu chuỗi mini platform
            float tryX = Random.Range(startX + 2f, endX / 2f);
            float lastY = groundY + aerialHeight;
            for (int i = 0; i < count; i++)
            {
                MiniPlatformData data = miniPlatformLibrary[Random.Range(0, miniPlatformLibrary.Count)];
                float len = data.GetLength();

                Vector3 pos = new Vector3(tryX, lastY, 0);

                // Kiểm tra va chạm với Obstacle vừa tạo ở bước trước
                Collider2D hit = Physics2D.OverlapBox(pos, new Vector2(len + 1f, 6f), 0, obstacleLayer);

                if (hit == null)
                {
                    // Không vướng -> Spawn bình thường
                    Instantiate(data.prefab, pos, Quaternion.identity, miniPlatformObjs);
                    lastY = pos.y + RandomUtilities.RandomWithSteps(minAerialHeight, maxAerialHeight, 0.5f);
                    tryX += len + RandomUtilities.RandomWithSteps(minGapAerial, maxGapAerial, 0.5f);
                }
                else
                {
                    // Vướng Obstacle -> Đặt cao lên trên đầu nó và nhích tới
                    pos.y = hit.bounds.max.y + 2.5f;
                    pos.x = hit.bounds.max.x + 1f;

                    Instantiate(data.prefab, pos, Quaternion.identity, miniPlatformObjs);
                    lastY = pos.y + RandomUtilities.RandomWithSteps(minAerialHeight, maxAerialHeight, 0.5f);
                    tryX += len + RandomUtilities.RandomWithSteps(minGapAerial, maxGapAerial, 0.5f);
                }
            }
        }
    }

    //--- HELPER FUNCTIONS ---

    /// <summary>
    /// Hàm tính và lưu trữ thông tin các đoạn đất hiện có theo id
    /// </summary>
    /// <param name="groundID">id của đoạn đất</param>
    /// <param name="startGroundX">điểm bắt đầu</param>
    /// <param name="endGroundX">điểm kết thúc</param>
    private void CheckCurrentGrounds(int groundID, float startGroundX, float endGroundX)
    {
        if (!currentGrounds.ContainsKey(groundID))
        {
            currentGrounds.Add(groundID, new GroundSegment { startX = startGroundX, endX = endGroundX });
        }
        else
        {
            currentGrounds[groundID] = new GroundSegment { startX = startGroundX, endX = endGroundX };
        }

        Debug.Log($"Ground ID: {groundID}, StartX: {startGroundX}, EndX: {endGroundX}");
    }
}
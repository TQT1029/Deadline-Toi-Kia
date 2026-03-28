using UnityEngine;
using System.Collections.Generic;

public class PitObjectGenerator : MonoBehaviour
{
    public static PitObjectGenerator Instance;
    private void Awake() => Instance = this;

    [Header("References")]
    [field: SerializeField] public Transform obstacleObjs { get; private set; }
    [field: SerializeField] public Transform miniPlatformObjs { get; private set; }
    [field: SerializeField] public List<ObstacleData> obstacleLibrary { get; private set; }
    [field: SerializeField] public List<MiniPlatformData> miniPlatformLibrary { get; private set; }

    // [REMOVED] groundY, pitY, waveFrequency đã chuyển sang MapGlobalConfig

    [Header("Pit Logic ")]
    [SerializeField] private bool isSpawnObjectInPit = true;
    [SerializeField, Range(0, 100)] private float ratioBridgeToObstacle = 50f;
    [Tooltip("Khoảng trống (đệm) tính từ mép hố trở vào, dùng để check xem có đủ chỗ rải không")]
    [SerializeField] private float pitEdgePadding = 2f;

    [Header("Objects Settings ")]
    [SerializeField, Min(-2f)] private float minVerticalGap = -1f;
    [SerializeField, Min(0f)] private float maxVerticalGap = 2f;
    [SerializeField, Min(0.5f)] private float minHorizontalGap = 0.5f;
    [SerializeField, Min(1.5f)] private float maxHorizontalGap = 1.5f;
    [SerializeField, Min(0.5f)] private float stepGap = 0.5f;

    private float pitWidthNeedObjects = 7;
    private float noiseOffsetX;

    private void Start()
    {
        noiseOffsetX = Random.Range(0, 10000);
        if (obstacleLibrary != null)
            foreach (var obs in obstacleLibrary) obs.Initialize();
    }

    public void GenerateObjectsInPit(float startX, float endX)
    {
        if (!isSpawnObjectInPit) return;

        float pitWidth = endX - startX;

        // Nếu hố đủ rộng để cần vật thể lót chân
        if (pitWidth >= pitWidthNeedObjects)
        {
            // Bốc thăm xem ưu tiên rải Cầu hay rải Vật Cản
            bool wantBridge = RandomUtils.ChancePercent(ratioBridgeToObstacle);

            // Thử spawn loại đã chọn
            bool success = TrySpawn(wantBridge, startX, endX, pitWidth);

            // Nếu loại kia không phù hợp (ví dụ: hố 8m nhưng chọn trúng Cầu mà thư viện Cầu toàn 10m)
            // Thì thử đổi sang loại còn lại
            if (!success)
            {
                success = TrySpawn(!wantBridge, startX, endX, pitWidth);
            }

            // [FAIL-SAFE]: Kích hoạt nếu CẢ 2 LOẠI đều thất bại (Trường hợp cực hiếm)
            // Đảm bảo tối thiểu người chơi có một điểm đặt chân để nhảy qua hố
            if (!success)
            {
                ForceSpawnSafePlatform(startX, endX);
            }
        }
    }

    // --- HÀM HỖ TRỢ KIỂM TRA & SPAWN ---
    private bool TrySpawn(bool isBridge, float startX, float endX, float pitWidth)
    {
        if (isBridge)
        {
            if (miniPlatformLibrary == null || miniPlatformLibrary.Count == 0) return false;

            // 1. Lọc ra những tấm Cầu có chiều dài nhét LỌT VỪA vô hố
            List<MiniPlatformData> validCandidates = new List<MiniPlatformData>();
            foreach (var p in miniPlatformLibrary)
            {
                if (p.GetLength() <= pitWidth) validCandidates.Add(p);
            }

            if (validCandidates.Count == 0) return false; // Thất bại: Không có cầu nào lọt vừa

            // 2. Random 1 tấm hợp lệ trong danh sách đã lọc
            MiniPlatformData data = validCandidates[Random.Range(0, validCandidates.Count)];
            float len = data.GetLength();

            // 3. Phân loại rải
            if (pitWidth <= len + (pitEdgePadding*2))
                SpawnBridgeCenter(startX, endX, data);
            else
                SpawnBridges(startX, endX);

            return true;
        }
        else
        {
            if (obstacleLibrary == null || obstacleLibrary.Count == 0) return false;

            // 1. Lọc ra những Vật Cản có chiều dài nhét LỌT VỪA vô hố
            List<ObstacleData> validCandidates = new List<ObstacleData>();
            foreach (var obs in obstacleLibrary)
            {
                if (obs.GetSize().x <= pitWidth) validCandidates.Add(obs);
            }

            if (validCandidates.Count == 0) return false; // Thất bại: Không có vật cản nào lọt vừa

            // 2. Random 1 vật cản hợp lệ trong danh sách đã lọc
            ObstacleData data = validCandidates[Random.Range(0, validCandidates.Count)];
            float len = data.GetSize().x;

            // 3. Phân loại rải
            if (pitWidth <= len + (pitEdgePadding*2))
                SpawnObstacleCenter(startX, endX, data);
            else
                SpawnObstacles(startX, endX);

            return true;
        }
    }

    // --- HÀM BẢO HIỂM ---
    private void ForceSpawnSafePlatform(float startX, float endX)
    {
        // Nhặt tấm nhỏ nhất trong game và ép đặt vào giữa hố để cứu người chơi
        if (miniPlatformLibrary != null && miniPlatformLibrary.Count > 0)
        {
            MiniPlatformData smallest = miniPlatformLibrary[0];
            foreach (var p in miniPlatformLibrary)
            {
                if (p.GetLength() < smallest.GetLength()) smallest = p;
            }
            SpawnBridgeCenter(startX, endX, smallest);
        }
    }
    //----- Obstacle -----

    private void SpawnObstacles(float startX, float endX)
    {

        float currentX = startX + pitEdgePadding;
        float limitX = endX - pitEdgePadding;

        while (currentX < limitX)
        {
            ObstacleData obs = obstacleLibrary[Random.Range(0, obstacleLibrary.Count)];
            float pitY = obs.useGroundY ? MapGlobalConfig.Instance.groundY : MapGlobalConfig.Instance.pitY;
            Vector2 size = obs.GetSize();

            if (currentX + size.x <= limitX)
            {
                Vector3 pos = new Vector3(currentX + size.x / 2f, pitY, 0);
                Instantiate(obs.prefab, pos, Quaternion.identity, obstacleObjs);

                // Dùng chung Horizontal Gap với cầu cho nhất quán (hoặc bạn có thể tạo gap riêng)
                currentX += size.x + RandomUtils.RandomWithSteps(minHorizontalGap, maxHorizontalGap, stepGap);
            }
            else
            {
                break; // Hết chỗ cho vật cản này
            }
        }
    }

    private void SpawnObstacleCenter(float startX, float endX, ObstacleData obs)
    {
        float pitY = obs.useGroundY ? MapGlobalConfig.Instance.groundY : MapGlobalConfig.Instance.pitY;

        // Kiểm tra an toàn: Đảm bảo hố lọt nổi vật cản này
        if (obs.GetSize().x <= (endX - startX))
        {
            Vector3 pos = new Vector3((startX + endX) / 2f, pitY, 0);
            Instantiate(obs.prefab, pos, Quaternion.identity, obstacleObjs);
        }
    }

    //----- Bridge -----

    private void SpawnBridges(float startX, float endX)
    {
        float groundY = MapGlobalConfig.Instance.groundY;
        float waveFreq = MapGlobalConfig.Instance.waveFrequency;

        float currentX = startX + 0.5f;
        float limit = endX - 0.5f;

        while (currentX < limit)
        {
            float remainingSpace = limit - currentX;
            List<MiniPlatformData> validCandidates = new List<MiniPlatformData>();
            foreach (var p in miniPlatformLibrary)
            {
                if (p.GetLength() <= remainingSpace) validCandidates.Add(p);
            }

            if (validCandidates.Count == 0) break;

            MiniPlatformData selectedData = validCandidates[Random.Range(0, validCandidates.Count)];
            float len = selectedData.GetLength();

            // Sử dụng Wave Frequency chung
            float waveHeight = RandomUtils.GetPerlinHeight(
                currentX + noiseOffsetX,
                waveFreq,
                minVerticalGap,
                maxVerticalGap,
                1.5f
            );

            Vector3 pos = new Vector3(currentX + len / 2f, groundY + waveHeight, 0);
            Instantiate(selectedData.prefab, pos, Quaternion.identity, miniPlatformObjs);

            currentX += len + RandomUtils.RandomWithSteps(minHorizontalGap, maxHorizontalGap, stepGap);
        }


    }

    private void SpawnBridgeCenter(float startX, float endX, MiniPlatformData data)
    {
        float groundY = MapGlobalConfig.Instance.groundY;

        // Kiểm tra an toàn: Đảm bảo hố lọt nổi cái cầu này
        if (data.GetLength() <= (endX - startX))
        {
            // Độ cao ngẫu nhiên trong mức cho phép để tự nhiên hơn
            float targetY = groundY + Random.Range(minVerticalGap, maxVerticalGap);
            Vector3 pos = new Vector3((startX + endX) / 2f, targetY, 0);

            Instantiate(data.prefab, pos, Quaternion.identity, miniPlatformObjs);
        }
    }
}
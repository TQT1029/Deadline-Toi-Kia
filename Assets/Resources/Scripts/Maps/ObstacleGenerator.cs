using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class ObstacleGenerator : MonoBehaviour
{
    public static ObstacleGenerator Instance;
    private void Awake() => Instance = this;

    [Header("References")]
    [field: SerializeField] public Transform obstacleObjs { get; private set; }
    [field: SerializeField] public Transform miniPlatformObjs { get; private set; }
    [field: SerializeField] public LayerMask obstacleLayer { get; private set; }
    [field: SerializeField] public List<ObstacleData> obstacleLibrary { get; private set; }
    [field: SerializeField] public List<MiniPlatformData> miniPlatformLibrary { get; private set; }

    private RandomUtils.ShuffleBag<MiniPlatformData> miniPlatformBag;

    [Header("Obstacle Settings ")]
    [SerializeField, Range(0, 100)] private int ratioObstacleToAerial = 70;
    [SerializeField, Range(0, 100)] private int changeSpawnObstacle = 80;

    [Header("Layout Logic ")]
    [SerializeField] private float obstacleEdgePadding = 2f;
    [SerializeField] private float minObstacleGap = 7f;
    [SerializeField] private float maxObstacleGap = 12f;

    [Header("Aerial Logic ")]
    [SerializeField] private float aerialHeight = 3f;
    [SerializeField] private float minAerialHeight = -1f;
    [SerializeField] private float maxAerialHeight = 3f;
    [SerializeField] private float minGapAerial = 1f;
    [SerializeField] private float maxGapAerial = 3f;

    [Header("Zone Config")]
    [SerializeField] private float minZoneLength = 30f; // Độ dài tối thiểu của 1 khu vực (để tránh bị đổi qua lại liên tục)
    [SerializeField] private float maxZoneLength = 60f;
    private float noiseOffsetX = 0;
    private enum ZoneType { None, GroundObstacles, AerialPlatforms }
    private ZoneType currentZone = ZoneType.None;
    private float zoneEndX = 0f;
    private float nextSpawnX = 0f;

    private void Start()
    {
        if (miniPlatformLibrary != null && miniPlatformLibrary.Count > 0)
            miniPlatformBag = new RandomUtils.ShuffleBag<MiniPlatformData>(miniPlatformLibrary);

        noiseOffsetX = Random.Range(-100000f, 100000f);

        if (obstacleLibrary != null)
            foreach (var obs in obstacleLibrary) obs.Initialize();
    }

    public void GenerateObstaclesOnGround(float startX, float endX)
    {

        // 1. Kiểm tra xem đã cần chuyển Zone chưa (hết độ dài zone hoặc bị ngắt bởi hố)
        if (currentZone == ZoneType.None || startX >= zoneEndX)
        {
            // Random loại Zone mới
            currentZone = RandomUtils.ChancePercent(ratioObstacleToAerial) ? ZoneType.GroundObstacles : ZoneType.AerialPlatforms;

            // Đặt giới hạn cho Zone này (Zone này sẽ kéo dài qua nhiều mảnh đất)
            zoneEndX = startX + RandomUtils.RandomWithSteps(minZoneLength, maxZoneLength, 1f);

            // Chỉ thêm Padding ở ĐẦU của một Zone mới
            nextSpawnX = startX + obstacleEdgePadding;
        }

        // 2. Tiếp tục rải nối tiếp vào Zone hiện tại
        if (currentZone == ZoneType.GroundObstacles)
        {
            ContinueGroundObstacles(startX, endX);
        }
        else
        {
            ContinueAerialPlatforms(startX, endX);
        }
    }

    private void ContinueGroundObstacles(float startX, float endX)
    {
        float groundY = MapGlobalConfig.Instance.groundY;

        // Nếu điểm spawn tiếp theo bị tụt lại phía sau mảnh đất này (hiếm khi xảy ra), đẩy nó lên
        if (nextSpawnX < startX) nextSpawnX = startX + obstacleEdgePadding;

        // Không trừ padding ở endX nữa, vì mảnh đất sau có thể nối liền mảnh này
        float limitX = endX;

        while (nextSpawnX < limitX)
        {
            if (RandomUtils.ChancePercent(changeSpawnObstacle))
            {
                ObstacleData obs = obstacleLibrary[Random.Range(0, obstacleLibrary.Count)];
                Vector2 size = obs.GetSize();

                // NẾU VẬT CẢN VỪA KHÍT TRONG MẢNH ĐẤT NÀY -> RẢI
                if (nextSpawnX + size.x <= limitX)
                {
                    Vector3 pos = new Vector3(nextSpawnX + size.x / 2f, groundY, 0);
                    Instantiate(obs.prefab, pos, Quaternion.identity, obstacleObjs);

                    // Cộng dồn X cho vật tiếp theo
                    nextSpawnX += size.x + RandomUtils.RandomWithSteps(minObstacleGap, maxObstacleGap, 0.5f);
                }
                else
                {
                    // NẾU KHÔNG VỪA -> DỪNG LẠI (Break). 
                    // Tọa độ nextSpawnX vẫn được giữ nguyên. 
                    // Khi mảnh đất tiếp theo sinh ra, vòng lặp này sẽ chạy tiếp từ đúng điểm đang chờ!
                    break;
                }
            }
            else
            {
                nextSpawnX += RandomUtils.RandomWithSteps(minObstacleGap, maxObstacleGap, 0.5f);
            }
        }
    }

    private void ContinueAerialPlatforms(float startX, float endX)
    {
        if (miniPlatformBag == null || miniPlatformLibrary == null) return;

        float groundY = MapGlobalConfig.Instance.groundY;

        // Sử dụng waveFrequency như độ giãn của Perlin Noise. 
        // Giá trị càng nhỏ (ví dụ 0.05 - 0.1), địa hình càng thoai thoải.
        // Giá trị lớn (0.3 - 0.5) tạo ra đồi núi nhấp nhô liên tục.
        float noiseScale = MapGlobalConfig.Instance.waveFrequency;

        float maxH = MapGlobalConfig.Instance.maxHeightMap;

        if (nextSpawnX < startX) nextSpawnX = startX;
        float limitX = endX;

        while (nextSpawnX < limitX)
        {
            MiniPlatformData data = miniPlatformBag.Next();
            float len = data.GetLength();
            int attempts = 0;

            // Check coi có tấm nào ngắn vừa chỗ trống không
            while (nextSpawnX + len > limitX && attempts < 3)
            {
                data = miniPlatformBag.Next();
                len = data.GetLength();
                attempts++;
            }

            // Nếu tấm nhỏ nhất cũng không vừa -> Chờ mảnh đất tiếp theo
            if (nextSpawnX + len > limitX) break;

            // --- THAY ĐỔI CHÍNH NẰM Ở ĐÂY ---
            // Sử dụng GetPerlinHeight thay vì GetSineWaveHeight
            // Cộng thêm noiseOffsetX để mỗi lần chơi map sẽ có hình dáng đồi núi khác nhau
            float waveHeight = RandomUtils.GetPerlinHeight(
                nextSpawnX + noiseOffsetX, // xPosition: dùng tọa độ thực tế + khoảng dịch ngẫu nhiên ban đầu
                noiseScale,                // scale: độ gắt của địa hình
                minAerialHeight,           // minHeight
                maxAerialHeight,           // maxHeight
                1.0f                       // step: làm tròn từng 1 mét để dễ nhảy
            );

            float targetY = groundY + aerialHeight + waveHeight;
            targetY = Mathf.Clamp(targetY, groundY + 2f, maxH);
            Vector3 pos = new Vector3(nextSpawnX + len / 2f, targetY, 0);

            // Kiểm tra va chạm với vật cản bên dưới
            Collider2D hit = Physics2D.OverlapBox(pos, new Vector2(len + 0.5f, 3f), 0, obstacleLayer);

            if (hit == null)
            {
                Instantiate(data.prefab, pos, Quaternion.identity, miniPlatformObjs);
            }
            else
            {
                // Nếu đụng, đẩy lên trên đầu vật cản
                float newY = hit.bounds.max.y + 2.5f;
                if (newY < maxH)
                {
                    pos.y = newY;
                    Instantiate(data.prefab, pos, Quaternion.identity, miniPlatformObjs);
                }
            }

            // Cộng dồn X cho bệ đỡ tiếp theo
            nextSpawnX += len + RandomUtils.RandomWithSteps(minGapAerial, maxGapAerial, 1.5f);
        }
    }

    public void ResetZoneInterruption()
    {
        currentZone = ZoneType.None;
    }
}
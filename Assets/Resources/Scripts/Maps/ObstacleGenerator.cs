using UnityEngine;
using System.Collections.Generic;

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

    [Header("Obstacle Settings (Riêng biệt)")]
    [SerializeField, Range(0, 100)] private int ratioObstacleToAerial = 70;

    [Header("Layout Logic (Riêng biệt)")]
    [SerializeField] private float obstacleEdgePadding = 2f;
    [SerializeField] private float minObstacleGap = 7f;
    [SerializeField] private float maxObstacleGap = 12f;

    [Header("Aerial Logic (Riêng biệt)")]
    [SerializeField] private float aerialHeight = 3f;
    [SerializeField] private float minAerialHeight = -1f;
    [SerializeField] private float maxAerialHeight = 3f;
    [SerializeField] private float minGapAerial = 1f;
    [SerializeField] private float maxGapAerial = 3f;

    [Header("Zone Config")]
    [SerializeField] private float minZoneLength = 30f; // Độ dài tối thiểu của 1 khu vực (để tránh bị đổi qua lại liên tục)
    [SerializeField] private float maxZoneLength = 60f;
    private enum ZoneType { None, GroundObstacles, AerialPlatforms }
    private ZoneType currentZone = ZoneType.None;
    private float zoneEndX = 0f;
    private float nextSpawnX = 0f;

    private void Start()
    {
        if (miniPlatformLibrary != null && miniPlatformLibrary.Count > 0)
            miniPlatformBag = new RandomUtils.ShuffleBag<MiniPlatformData>(miniPlatformLibrary);

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
            zoneEndX = startX + Random.Range(minZoneLength, maxZoneLength);

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
    }

    private void ContinueAerialPlatforms(float startX, float endX)
    {
        if (miniPlatformBag == null || miniPlatformLibrary == null) return;

        float groundY = MapGlobalConfig.Instance.groundY;
        float waveFreq = MapGlobalConfig.Instance.waveFrequency;
        float maxH = MapGlobalConfig.Instance.maxHeightMap;

        if (nextSpawnX < startX) nextSpawnX = startX;
        float limitX = endX;
        float segmentPhase = Random.Range(0f, Mathf.PI * 2);

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

            float waveHeight = RandomUtils.GetSineWaveHeight(
                nextSpawnX, waveFreq, minAerialHeight, maxAerialHeight, segmentPhase, 1.0f
            );

            float targetY = groundY + aerialHeight + waveHeight;
            targetY = Mathf.Clamp(targetY, groundY + 2f, maxH);
            Vector3 pos = new Vector3(nextSpawnX + len / 2f, targetY, 0);

            Collider2D hit = Physics2D.OverlapBox(pos, new Vector2(len + 0.5f, 3f), 0, obstacleLayer);

            if (hit == null)
            {
                Instantiate(data.prefab, pos, Quaternion.identity, miniPlatformObjs);
            }
            else
            {
                float newY = hit.bounds.max.y + 2.5f;
                if (newY < maxH)
                {
                    pos.y = newY;
                    Instantiate(data.prefab, pos, Quaternion.identity, miniPlatformObjs);
                }
            }

            // Cộng dồn X
            nextSpawnX += len + RandomUtils.RandomWithSteps(minGapAerial, maxGapAerial, 1.5f);
        }
    }

    public void ResetZoneInterruption()
    {
        currentZone = ZoneType.None;
    }
}
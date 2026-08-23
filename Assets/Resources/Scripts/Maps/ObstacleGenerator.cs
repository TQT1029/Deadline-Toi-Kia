using System.Collections.Generic;
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
    [SerializeField] private float minimumHeight = 3f;
    [SerializeField] private float minVerticalGap = -1f;
    [SerializeField] private float maxVerticalGap = 3f;
    [SerializeField] private float minHorizontalGap = 1f;
    [SerializeField] private float maxHorizontalGap = 3f;

    [Header("Zone Config")]
    [SerializeField] private float minZoneObstacleLength = 30f; // Độ dài tối thiểu của 1 khu vực (để tránh bị đổi qua lại liên tục)
    [SerializeField] private float maxZoneObstacleLength = 60f;
    [SerializeField] private float minZoneAerialLength = 30f; // Độ dài tối thiểu của 1 khu vực (để tránh bị đổi qua lại liên tục)
    [SerializeField] private float maxZoneAerialLength = 60f;
    private float noiseOffsetX = 0;
    private enum ZoneType { None, GroundObstacles, AerialPlatforms }
    private ZoneType currentZone = ZoneType.None;
    private float zoneEndX = 0f;
    private float nextSpawnX = 0f;

    [Header("Runtime Pacing")]
    public bool IsGenerationEnabled { get; set; } = true;
    public float DensityMultiplier { get; set; } = 1.0f;

    private void Start()
    {
        InitializeLibraries();
        noiseOffsetX = Random.Range(-100000f, 100000f);
    }

    private void InitializeLibraries()
    {
        if (miniPlatformLibrary != null && miniPlatformLibrary.Count > 0)
            miniPlatformBag = new RandomUtils.ShuffleBag<MiniPlatformData>(miniPlatformLibrary);

        if (obstacleLibrary != null)
        {
            foreach (var obs in obstacleLibrary) obs.Initialize();
        }
    }

    public void ApplyConfig(MapProfile profile)
    {
        if (profile == null) return;

        if (profile.obstacleLibrary != null && profile.obstacleLibrary.Count > 0)
        {
            obstacleLibrary = profile.obstacleLibrary;
        }

        if (profile.miniPlatformLibrary != null && profile.miniPlatformLibrary.Count > 0)
        {
            miniPlatformLibrary = profile.miniPlatformLibrary;
        }

        ratioObstacleToAerial = profile.ratioObstacleToAerial;
        changeSpawnObstacle = profile.changeSpawnObstacle;
        obstacleEdgePadding = profile.obstacleEdgePadding;
        minObstacleGap = profile.minObstacleGap;
        maxObstacleGap = profile.maxObstacleGap;
        minimumHeight = profile.minimumHeight;
        minVerticalGap = profile.minVerticalGap;
        maxVerticalGap = profile.maxVerticalGap;
        minHorizontalGap = profile.minHorizontalGap;
        maxHorizontalGap = profile.maxHorizontalGap;
        minZoneObstacleLength = profile.minZoneObstacleLength;
        maxZoneObstacleLength = profile.maxZoneObstacleLength;
        minZoneAerialLength = profile.minZoneAerialLength;
        maxZoneAerialLength = profile.maxZoneAerialLength;

        InitializeLibraries();
    }

    public void Prewarm(int countPerPrefab = 3)
    {
        if (obstacleLibrary != null)
        {
            foreach (var obs in obstacleLibrary)
            {
                if (obs != null && obs.prefab != null)
                {
                    GameObjectPool.Prewarm(obs.prefab, countPerPrefab, obstacleObjs);
                }
            }
        }

        if (miniPlatformLibrary != null)
        {
            foreach (var plat in miniPlatformLibrary)
            {
                if (plat != null && plat.prefab != null)
                {
                    GameObjectPool.Prewarm(plat.prefab, countPerPrefab, miniPlatformObjs);
                }
            }
        }
    }

    public void GenerateObstaclesOnGround(float startX, float endX)
    {
        if (!IsGenerationEnabled || DensityMultiplier <= 0f) return;

        // 1. Kiểm tra xem đã cần chuyển Zone chưa (hết độ dài zone hoặc bị ngắt bởi hố)
        if (currentZone == ZoneType.None || startX >= zoneEndX)
        {
            // Random loại Zone mới
            currentZone = RandomUtils.ChancePercent(ratioObstacleToAerial) ? ZoneType.GroundObstacles : ZoneType.AerialPlatforms;

            // Đặt giới hạn cho Zone này (Zone này sẽ kéo dài qua nhiều mảnh đất)
            if (currentZone == ZoneType.GroundObstacles)
            {
                zoneEndX = startX + RandomUtils.RandomWithSteps(minZoneObstacleLength, maxZoneObstacleLength, 1f);
            }
            else
            {
                zoneEndX = startX + RandomUtils.RandomWithSteps(minZoneAerialLength, maxZoneAerialLength, 1f);
            }
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
        if (obstacleLibrary == null || obstacleLibrary.Count == 0) return;

        float groundY = (MapGlobalConfig.Instance != null) ? MapGlobalConfig.Instance.groundY : -5f;

        // Nếu điểm spawn tiếp theo bị tụt lại phía sau mảnh đất này, đẩy nó lên
        if (nextSpawnX < startX) nextSpawnX = startX + obstacleEdgePadding;

        float limitX = endX;
        int effectiveChance = Mathf.Clamp(Mathf.RoundToInt(changeSpawnObstacle * DensityMultiplier), 10, 100);
        float gapScale = 1f / Mathf.Clamp(DensityMultiplier, 0.4f, 2.0f);

        while (nextSpawnX < limitX)
        {
            if (RandomUtils.ChancePercent(effectiveChance))
            {
                ObstacleData obs = obstacleLibrary[Random.Range(0, obstacleLibrary.Count)];
                Vector2 size = obs.GetSize();

                // NẾU VẬT CẢN VỪA KHÍT TRONG MẢNH ĐẤT NÀY -> RẢI
                if (nextSpawnX + size.x <= limitX)
                {
                    Vector3 pos = new Vector3(nextSpawnX + size.x / 2f, groundY, 0);
                    // [OPTIMIZED POOLING] Lấy vật cản từ Pool
                    GameObjectPool.Get(obs.prefab, pos, Quaternion.identity, obstacleObjs);

                    // Cộng dồn X cho vật tiếp theo (nhân với gapScale để giãn khoảng cách khi giảm mật độ)
                    nextSpawnX += size.x + RandomUtils.RandomWithSteps(minObstacleGap * gapScale, maxObstacleGap * gapScale, 0.5f);
                }
                else
                {
                    break;
                }
            }
            else
            {
                nextSpawnX += RandomUtils.RandomWithSteps(minObstacleGap * gapScale, maxObstacleGap * gapScale, 0.5f);
            }
        }
    }

    private void ContinueAerialPlatforms(float startX, float endX)
    {
        if (miniPlatformBag == null || miniPlatformLibrary == null || miniPlatformLibrary.Count == 0) return;

        float groundY = (MapGlobalConfig.Instance != null) ? MapGlobalConfig.Instance.groundY : -5f;
        float noiseScale = (MapGlobalConfig.Instance != null) ? MapGlobalConfig.Instance.waveFrequency : 0.4f;
        float maxH = (MapGlobalConfig.Instance != null) ? MapGlobalConfig.Instance.maxHeightMap : 15f;

        if (nextSpawnX < startX) nextSpawnX = startX;
        float limitX = endX;
        float gapScale = 1f / Mathf.Clamp(DensityMultiplier, 0.4f, 2.0f);

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

            if (nextSpawnX + len > limitX) break;

            float waveHeight = RandomUtils.GetPerlinHeight(
                nextSpawnX + noiseOffsetX,
                noiseScale,
                minVerticalGap,
                maxVerticalGap,
                1.0f
            );

            float targetY = groundY + minimumHeight + waveHeight;
            targetY = Mathf.Clamp(targetY, groundY + 2f, maxH);
            Vector3 pos = new Vector3(nextSpawnX + len / 2f, targetY, 0);

            // Kiểm tra va chạm với vật cản bên dưới
            Collider2D hit = Physics2D.OverlapBox(pos, new Vector2(len + 0.5f, 3f), 0, obstacleLayer);

            if (hit == null)
            {
                // [OPTIMIZED POOLING] Lấy sàn bay từ Pool
                GameObjectPool.Get(data.prefab, pos, Quaternion.identity, miniPlatformObjs);
            }
            else
            {
                float newY = hit.bounds.max.y + 2.5f;
                if (newY < maxH)
                {
                    pos.y = newY;
                    GameObjectPool.Get(data.prefab, pos, Quaternion.identity, miniPlatformObjs);
                }
            }

            // Cộng dồn X cho bệ đỡ tiếp theo
            nextSpawnX += len + RandomUtils.RandomWithSteps(minHorizontalGap * gapScale, maxHorizontalGap * gapScale, 1.5f);
        }
    }

    public void ResetZoneInterruption()
    {
        currentZone = ZoneType.None;
    }
}
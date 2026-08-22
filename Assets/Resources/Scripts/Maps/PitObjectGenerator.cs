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

    [Header("Pit Logic ")]
    [SerializeField] private bool isSpawnObjectInPit = true;
    [SerializeField, Range(0, 100)] private float ratioBridgeToObstacle = 50f;
    [Tooltip("Khoảng trống (đệm) tính từ mép hố trở vào, dùng để check xem có đủ chỗ rải không")]
    [SerializeField] private float pitWidthNeedObjects = 7;
    [SerializeField] private float pitEdgePadding = 2f;

    [Header("Objects Settings ")]
    [SerializeField, Min(-2f)] private float minVerticalGap = -1f;
    [SerializeField, Min(0f)] private float maxVerticalGap = 2f;
    [SerializeField, Min(0.5f)] private float minHorizontalGap = 0.5f;
    [SerializeField, Min(1.5f)] private float maxHorizontalGap = 1.5f;
    [SerializeField, Min(0.5f)] private float stepGap = 0.5f;

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

        if (pitWidth >= pitWidthNeedObjects)
        {
            bool wantBridge = RandomUtils.ChancePercent(ratioBridgeToObstacle);

            bool success = TrySpawn(wantBridge, startX, endX, pitWidth);

            if (!success)
            {
                success = TrySpawn(!wantBridge, startX, endX, pitWidth);
            }

            // [FAIL-SAFE]: Kích hoạt nếu CẢ 2 LOẠI đều thất bại (Trường hợp cực hiếm)
            // Đảm bảo tối thiểu người chơi có một điểm đặt chân để nhảy qua hố
            if (!success)
            {
                ForceSpawnAbsoluteSmallest(startX, endX);
            }
        }
    }

    private bool TrySpawn(bool isBridge, float startX, float endX, float pitWidth)
    {
        float centerX = (startX + endX) / 2f;
        float usableSpace = pitWidth - (pitEdgePadding * 2);

        if (isBridge)
        {
            if (miniPlatformLibrary == null || miniPlatformLibrary.Count == 0) return false;

            // 1. CHỌN VÀ SPAWN VẬT THỂ Ở TÂM (Đảm bảo 100% rải ít nhất 1 cái)
            MiniPlatformData centerBridge = GetRandomBridge(usableSpace);
            if (centerBridge == null) centerBridge = GetSmallestBridge(); // Fallback nếu hố nhỏ hơn cả vật nhỏ nhất

            float centerLen = centerBridge.GetLength();
            SpawnSingleBridge(centerX, centerBridge);

            // 2. TỎA RA BÊN TRÁI
            float currentRightEdge = centerX - (centerLen / 2f) - GetRandomGap();
            while (currentRightEdge > startX + pitEdgePadding)
            {
                float availableSpace = currentRightEdge - (startX + pitEdgePadding);
                MiniPlatformData leftBridge = GetRandomBridge(availableSpace);

                if (leftBridge == null) break; // Hết chỗ nhét

                float len = leftBridge.GetLength();
                float spawnX = currentRightEdge - (len / 2f);
                SpawnSingleBridge(spawnX, leftBridge);

                currentRightEdge = spawnX - (len / 2f) - GetRandomGap();
            }

            // 3. TỎA RA BÊN PHẢI
            float currentLeftEdge = centerX + (centerLen / 2f) + GetRandomGap();
            while (currentLeftEdge < endX - pitEdgePadding)
            {
                float availableSpace = (endX - pitEdgePadding) - currentLeftEdge;
                MiniPlatformData rightBridge = GetRandomBridge(availableSpace);

                if (rightBridge == null) break; // Hết chỗ nhét

                float len = rightBridge.GetLength();
                float spawnX = currentLeftEdge + (len / 2f);
                SpawnSingleBridge(spawnX, rightBridge);

                currentLeftEdge = spawnX + (len / 2f) + GetRandomGap();
            }
            return true;
        }
        else // isObstacle
        {
            if (obstacleLibrary == null || obstacleLibrary.Count == 0) return false;

            // 1. CHỌN VÀ SPAWN VẬT CẢN Ở TÂM
            ObstacleData centerObs = GetRandomObstacle(usableSpace);
            if (centerObs == null) centerObs = GetSmallestObstacle();

            float centerLen = centerObs.GetSize().x;
            SpawnSingleObstacle(centerX, centerObs);

            // 2. TỎA RA BÊN TRÁI
            float currentRightEdge = centerX - (centerLen / 2f) - GetRandomGap();
            while (currentRightEdge > startX + pitEdgePadding)
            {
                float availableSpace = currentRightEdge - (startX + pitEdgePadding);
                ObstacleData leftObs = GetRandomObstacle(availableSpace);

                if (leftObs == null) break;

                float len = leftObs.GetSize().x;
                float spawnX = currentRightEdge - (len / 2f);
                SpawnSingleObstacle(spawnX, leftObs);

                currentRightEdge = spawnX - (len / 2f) - GetRandomGap();
            }

            // 3. TỎA RA BÊN PHẢI
            float currentLeftEdge = centerX + (centerLen / 2f) + GetRandomGap();
            while (currentLeftEdge < endX - pitEdgePadding)
            {
                float availableSpace = (endX - pitEdgePadding) - currentLeftEdge;
                ObstacleData rightObs = GetRandomObstacle(availableSpace);

                if (rightObs == null) break;

                float len = rightObs.GetSize().x;
                float spawnX = currentLeftEdge + (len / 2f);
                SpawnSingleObstacle(spawnX, rightObs);

                currentLeftEdge = spawnX + (len / 2f) + GetRandomGap();
            }
            return true;
        }
    }
    private void SpawnSingleBridge(float xPos, MiniPlatformData data)
    {
        float groundY = MapGlobalConfig.Instance.groundY;
        float waveFreq = MapGlobalConfig.Instance.waveFrequency;

        // Dùng Perlin Noise dựa theo tọa độ X để cầu nhấp nhô mượt mà
        float waveHeight = RandomUtils.GetPerlinHeight(xPos + noiseOffsetX, waveFreq, minVerticalGap, maxVerticalGap, 0.5f);

        Vector3 pos = new Vector3(xPos, groundY + waveHeight, 0);
        Instantiate(data.prefab, pos, Quaternion.identity, miniPlatformObjs);
    }

    private void SpawnSingleObstacle(float xPos, ObstacleData obs)
    {
        float pitY = obs.useGroundY ? MapGlobalConfig.Instance.groundY : MapGlobalConfig.Instance.pitY;
        Vector3 pos = new Vector3(xPos, pitY, 0);
        Instantiate(obs.prefab, pos, Quaternion.identity, obstacleObjs);
    }

    private void ForceSpawnAbsoluteSmallest(float startX, float endX)
    {
        float centerX = (startX + endX) / 2f;
        MiniPlatformData smallest = GetSmallestBridge();
        if (smallest != null) SpawnSingleBridge(centerX, smallest);
    }    //----- Obstacle -----

    private float GetRandomGap() => RandomUtils.RandomWithSteps(minHorizontalGap, maxHorizontalGap, stepGap);

    private MiniPlatformData GetRandomBridge(float maxWidth)
    {
        List<MiniPlatformData> valid = new List<MiniPlatformData>();
        foreach (var p in miniPlatformLibrary)
        {
            if (p.GetLength() <= maxWidth) valid.Add(p);
        }
        if (valid.Count == 0) return null;
        return valid[Random.Range(0, valid.Count)];
    }

    private ObstacleData GetRandomObstacle(float maxWidth)
    {
        List<ObstacleData> valid = new List<ObstacleData>();
        foreach (var obs in obstacleLibrary)
        {
            if (obs.GetSize().x <= maxWidth) valid.Add(obs);
        }
        if (valid.Count == 0) return null;
        return valid[Random.Range(0, valid.Count)];
    }

    private MiniPlatformData GetSmallestBridge()
    {
        if (miniPlatformLibrary == null || miniPlatformLibrary.Count == 0) return null;
        MiniPlatformData smallest = miniPlatformLibrary[0];
        foreach (var p in miniPlatformLibrary)
        {
            if (p.GetLength() < smallest.GetLength()) smallest = p;
        }
        return smallest;
    }

    private ObstacleData GetSmallestObstacle()
    {
        if (obstacleLibrary == null || obstacleLibrary.Count == 0) return null;
        ObstacleData smallest = obstacleLibrary[0];
        foreach (var obs in obstacleLibrary)
        {
            if (obs.GetSize().x < smallest.GetSize().x) smallest = obs;
        }
        return smallest;
    }
}
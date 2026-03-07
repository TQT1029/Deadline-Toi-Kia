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

    // [REMOVED] groundY, waveFrequency, maxHeightMap đã chuyển sang MapGlobalConfig

    [Header("Obstacle Settings (Riêng biệt)")]
    [SerializeField, Range(0, 100)] private int ratioObstacleToAerial = 70;
    [Range(0, 100)] public int obstacleChance = 60;

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

    private float noiseOffsetX;

    private void Start()
    {
        if (miniPlatformLibrary != null && miniPlatformLibrary.Count > 0)
            miniPlatformBag = new RandomUtils.ShuffleBag<MiniPlatformData>(miniPlatformLibrary);

        noiseOffsetX = Random.Range(0, 10000);
        if (obstacleLibrary != null)
            foreach (var obs in obstacleLibrary) obs.Initialize();
    }

    public void GenerateObstaclesOnGround(float startX, float endX)
    {
        if (RandomUtils.ChancePercent(ratioObstacleToAerial))
            SpawnGroundObstacles(startX, endX);
        else
            SpawnAerialPlatforms(startX, endX);
    }

    private void SpawnGroundObstacles(float startX, float endX)
    {
        float groundY = MapGlobalConfig.Instance.groundY;

        float currentX = startX + obstacleEdgePadding + RandomUtils.RandomWithSteps(minObstacleGap, maxObstacleGap, 1);
        float limitX = endX - obstacleEdgePadding;

        while (currentX < limitX)
        {
            if (RandomUtils.ChancePercent(obstacleChance))
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
            currentX += RandomUtils.RandomWithSteps(minObstacleGap, maxObstacleGap, 0.5f);
        }
    }

    private void SpawnAerialPlatforms(float startX, float endX)
    {
        if (miniPlatformBag == null || miniPlatformLibrary == null) return;

        float groundY = MapGlobalConfig.Instance.groundY;
        float waveFreq = MapGlobalConfig.Instance.waveFrequency;
        float maxH = MapGlobalConfig.Instance.maxHeightMap;

        float currentX = startX + RandomUtils.RandomWithSteps(2f, 4f, 0.5f);
        float limitX = endX - 2f;
        float segmentPhase = Random.Range(0f, Mathf.PI * 2);

        while (currentX < limitX)
        {
            MiniPlatformData data = miniPlatformBag.Next();
            float len = data.GetLength();
            int attempts = 0;
            while (currentX + len > limitX && attempts < 3)
            {
                data = miniPlatformBag.Next();
                len = data.GetLength();
                attempts++;
            }
            if (currentX + len > limitX) break;

            float waveHeight = RandomUtils.GetSineWaveHeight(
                currentX, waveFreq, minAerialHeight, maxAerialHeight, segmentPhase, 1.0f
            );

            float targetY = groundY + aerialHeight + waveHeight;
            targetY = Mathf.Clamp(targetY, groundY + 2f, maxH);
            Vector3 pos = new Vector3(currentX + len / 2f, targetY, 0);

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

            currentX += len + RandomUtils.RandomWithSteps(minGapAerial, maxGapAerial, 1.5f);
        }
    }
}
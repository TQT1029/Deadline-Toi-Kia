using UnityEngine;
using System.Collections.Generic;

public class PitObjectGenerator : MonoBehaviour
{
    public static PitObjectGenerator Instance;
    private void Awake() => Instance = this;

    [Header("References")]
    [field:SerializeField] public Transform obstacleObjs { get; private set; }
    [field: SerializeField] public Transform miniPlatformObjs { get; private set; }
    [field: SerializeField] public List<ObstacleData> obstacleLibrary { get; private set; }
    [field: SerializeField] public List<MiniPlatformData> miniPlatformLibrary { get; private set; }

    // [REMOVED] groundY, pitY, waveFrequency đã chuyển sang MapGlobalConfig

    [Header("Pit Logic (Riêng biệt)")]
    [SerializeField] private bool obstacleInPit = true;
    [SerializeField] private float pitWidthNeedBridge = 15;

    [Header("Bridge Settings (Riêng biệt)")]
    [SerializeField] private float minBridgeHeight = -1f;
    [SerializeField] private float maxBridgeHeight = 2f;
    [SerializeField] private float minGapBridge = 0.5f;
    [SerializeField] private float maxGapBridge = 1.5f;

    private float noiseOffsetX;

    private void Start()
    {
        noiseOffsetX = Random.Range(0, 10000);
        if (obstacleLibrary != null)
            foreach (var obs in obstacleLibrary) obs.Initialize();
    }

    public void GenerateObjectsInPit(float startX, float endX)
    {
        if (!obstacleInPit) return;

        float pitWidth = endX - startX;

        if (pitWidth > pitWidthNeedBridge)
            SpawnBridge(startX, endX);
        else
            SpawnObstacleInPitCenter(startX, endX);
    }

    private void SpawnObstacleInPitCenter(float startX, float endX)
    {
        float pitY = MapGlobalConfig.Instance.pitY;

        float padding = 2f;
        float limitX = endX - padding / 2;
        float currentX = startX + padding / 2;

        ObstacleData obs = obstacleLibrary[Random.Range(0, obstacleLibrary.Count)];

        if (obs.GetSize().x <= (limitX - currentX) + 1f)
        {
            Vector3 pos = new Vector3((startX + endX) / 2f, pitY, 0);
            Instantiate(obs.prefab, pos, Quaternion.identity, obstacleObjs);
        }
    }

    private void SpawnBridge(float startX, float endX)
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
            float waveHeight = RandomUtils.GetSineWaveHeight(
                currentX + noiseOffsetX,
                waveFreq,
                minBridgeHeight,
                maxBridgeHeight,
                1.5f
            );

            Vector3 pos = new Vector3(currentX + len / 2f, groundY + waveHeight, 0);
            Instantiate(selectedData.prefab, pos, Quaternion.identity, miniPlatformObjs);

            currentX += len + RandomUtils.RandomWithSteps(minGapBridge, maxGapBridge, 0.5f);
        }
    }
}
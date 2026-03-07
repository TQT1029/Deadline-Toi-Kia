using UnityEngine;
using System.Collections.Generic;

public class GroundGenerator : MonoBehaviour
{
    public static GroundGenerator Instance;
    private void Awake() => Instance = this;

    [Header("References")]
    [field: SerializeField] public Transform basePlatformObjs { get; private set; }
    [field: SerializeField] public List<BasePlatformData> baseLibrary { get; private set; }

    [Header("Segment Logic (Riêng biệt)")]
    // Các biến này chỉ dùng cho việc tính toán độ dài đoạn đất -> Giữ nguyên
    [SerializeField] private float minGroundSegmentLength = 30f;
    [SerializeField] private float maxGroundSegmentLength = 75f;
    [field: SerializeField] public float minPitWidth { get; private set; } = 3f;
    [field: SerializeField] public float maxPitWidth { get; private set; } = 6f;

    private float currentSegmentLength = 0f;

    public enum SegmentType { Ground, Pit }

    public struct GenerationResult
    {
        public SegmentType type;
        public float startX;
        public float endX;
    }

    public GenerationResult SpawnNextSegment(float currentX)
    {
        bool configHasPit = MapGlobalConfig.Instance.hasPit;
        int configPitChance = MapGlobalConfig.Instance.pitChance;

        if (!configHasPit) return GenerateGround(currentX);

        bool forcePit = currentSegmentLength > maxGroundSegmentLength;
        bool canPit = currentSegmentLength > minGroundSegmentLength;

        if (forcePit || (canPit && RandomUtils.ChancePercent(configPitChance)))
        {
            return GeneratePit(currentX);
        }
        else
        {
            return GenerateGround(currentX);
        }
    }

    private GenerationResult GeneratePit(float currentX)
    {
        float pitWidth = RandomUtils.RandomWithSteps(minPitWidth, maxPitWidth, 0.5f);
        float endX = currentX + pitWidth;

        currentSegmentLength = 0f;

        return new GenerationResult
        {
            type = SegmentType.Pit,
            startX = currentX,
            endX = endX
        };
    }

    private GenerationResult GenerateGround(float currentX)
    {
        // [SYNC] Lấy groundY từ Global Config
        float groundY = MapGlobalConfig.Instance.groundY;

        BasePlatformData data = baseLibrary[Random.Range(0, baseLibrary.Count)];
        float estimatedLen = data.GetLength();
        Vector3 pos = new Vector3(currentX + estimatedLen / 2f, groundY, 0);

        GameObject obj = Instantiate(data.prefab, pos, Quaternion.identity, basePlatformObjs);

        float actualLen = estimatedLen;
        var col = obj.GetComponent<BoxCollider2D>();
        if (col != null) actualLen = col.size.x * obj.transform.localScale.x;

        if (Mathf.Abs(actualLen - estimatedLen) > 0.01f)
            obj.transform.position = new Vector3(currentX + actualLen / 2f, groundY, 0);

        float endX = currentX + actualLen;
        currentSegmentLength += actualLen;

        return new GenerationResult
        {
            type = SegmentType.Ground,
            startX = currentX,
            endX = endX
        };
    }
}
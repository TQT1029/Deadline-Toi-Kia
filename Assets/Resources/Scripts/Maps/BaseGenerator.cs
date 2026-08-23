using UnityEngine;
using System.Collections.Generic;

public class BaseGenerator : MonoBehaviour
{
    public static BaseGenerator Instance;
    private void Awake() => Instance = this;

    [Header("References")]
    [field: SerializeField] public Transform basePlatformObjs { get; private set; }
    [field: SerializeField] public List<BasePlatformData> baseLibrary { get; private set; }

    [Header("Segment Logic ")]
    // Các biến này chỉ dùng cho việc tính toán độ dài đoạn đất -> Giữ nguyên
    [SerializeField] private float minGroundSegmentLength = 30f;
    [SerializeField] private float maxGroundSegmentLength = 75f;
    [field: SerializeField] public float minPitWidth { get; private set; } = 3f;
    [field: SerializeField] public float maxPitWidth { get; private set; } = 6f;

    private float currentSegmentLength = 0f;

    public float PitChanceMultiplier { get; set; } = 1.0f;

    public enum SegmentType { Ground, Pit }

    public struct GenerationResult
    {
        public SegmentType type;
        public float startX;
        public float endX;
        public float segmentLength;
    }

    public void ApplyConfig(MapProfile profile)
    {
        if (profile == null) return;

        if (profile.baseLibrary != null && profile.baseLibrary.Count > 0)
        {
            baseLibrary = profile.baseLibrary;
        }

        if (profile.minGroundSegmentLength > 0) minGroundSegmentLength = profile.minGroundSegmentLength;
        if (profile.maxGroundSegmentLength > 0) maxGroundSegmentLength = profile.maxGroundSegmentLength;
        if (profile.minPitWidth > 0) minPitWidth = profile.minPitWidth;
        if (profile.maxPitWidth > 0) maxPitWidth = profile.maxPitWidth;
    }

    public void Prewarm(int countPerPrefab = 3)
    {
        if (baseLibrary == null) return;
        foreach (var data in baseLibrary)
        {
            if (data != null && data.prefab != null)
            {
                GameObjectPool.Prewarm(data.prefab, countPerPrefab, basePlatformObjs);
            }
        }
    }

    public GenerationResult SpawnNextSegment(float currentX)
    {
        bool configHasPit = MapGlobalConfig.Instance != null && MapGlobalConfig.Instance.hasPit;
        int rawPitChance = MapGlobalConfig.Instance != null ? MapGlobalConfig.Instance.pitChance : 0;
        int effectivePitChance = Mathf.RoundToInt(rawPitChance * PitChanceMultiplier);

        // Nếu config cấm hố hoặc tỉ lệ hố = 0, tạo đất luôn
        if (!configHasPit || effectivePitChance <= 0) return GenerateGround(currentX);

        bool forcePit = currentSegmentLength > maxGroundSegmentLength;
        bool canPit = currentSegmentLength > minGroundSegmentLength;

        if (forcePit || (canPit && RandomUtils.ChancePercent(effectivePitChance)))
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
        // Tính độ rộng hố
        float pitWidth = RandomUtils.RandomWithSteps(minPitWidth, maxPitWidth, 0.5f);
        float endX = currentX + pitWidth;

        // Reset bộ đếm độ dài đất liền
        currentSegmentLength = 0f;

        return new GenerationResult
        {
            type = SegmentType.Pit,
            startX = currentX,
            endX = endX,
            segmentLength = pitWidth,
        };
    }

    private GenerationResult GenerateGround(float currentX)
    {
        float groundY = (MapGlobalConfig.Instance != null) ? MapGlobalConfig.Instance.groundY : -5f;

        if (baseLibrary == null || baseLibrary.Count == 0)
        {
            Debug.LogError("[BaseGenerator] baseLibrary is empty!");
            return new GenerationResult
            {
                type = SegmentType.Ground,
                startX = currentX,
                endX = currentX + 20f,
                segmentLength = 20f
            };
        }

        // Lấy data ngẫu nhiên
        BasePlatformData data = baseLibrary[Random.Range(0, baseLibrary.Count)];

        // Lấy length dự kiến từ config/data
        float length = data.GetLength();

        // Pivot ở tâm (Center), nên vị trí đặt = currentX + nửa chiều dài
        Vector3 spawnPos = new Vector3(currentX + length / 2f, groundY, 0);

        // [OPTIMIZED POOLING] Tái sử dụng GameObject từ Pool thay vì Instantiate
        GameObject obj = GameObjectPool.Get(data.prefab, spawnPos, Quaternion.identity, basePlatformObjs);

        // Kiểm tra lại bằng Collider thực tế để đảm bảo không bị hở map
        float actualLen = length;
        var col = obj.GetComponent<BoxCollider2D>();

        if (col != null)
        {
            actualLen = col.size.x * obj.transform.localScale.x;

            if (Mathf.Abs(actualLen - length) > 0.05f)
            {
                float correctedX = currentX + actualLen / 2f;
                obj.transform.position = new Vector3(correctedX, groundY, 0);
            }
        }

        // Điểm kết thúc của segment này
        float endX = currentX + actualLen;
        currentSegmentLength += actualLen;

        return new GenerationResult
        {
            type = SegmentType.Ground,
            startX = currentX,
            endX = endX,
            segmentLength = actualLen,
        };
    }
}
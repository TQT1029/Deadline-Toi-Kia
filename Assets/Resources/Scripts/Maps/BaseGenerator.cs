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

    public enum SegmentType { Ground, Pit }

    public struct GenerationResult
    {
        public SegmentType type;
        public float startX;
        public float endX;
        public float segmentLenght;
    }

    public GenerationResult SpawnNextSegment(float currentX)
    {
        bool configHasPit = MapGlobalConfig.Instance.hasPit;
        int configPitChance = MapGlobalConfig.Instance.pitChance;

        // Nếu config cấm hố, tạo đất luôn
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
            segmentLenght = currentSegmentLength,
        };
    }

    private GenerationResult GenerateGround(float currentX)
    {
        float groundY = MapGlobalConfig.Instance.groundY;

        // Lấy data ngẫu nhiên
        BasePlatformData data = baseLibrary[Random.Range(0, baseLibrary.Count)];

        // Lấy length dự kiến từ config/data
        float length = data.GetLength();

        // [LOGIC CHÍNH XÁC]: Pivot thường ở tâm (Center), nên vị trí đặt = currentX + nửa chiều dài
        Vector3 spawnPos = new Vector3(currentX + length / 2f, groundY, 0);

        GameObject obj = Instantiate(data.prefab, spawnPos, Quaternion.identity, basePlatformObjs);

        // [AUTO CORRECTION]: Kiểm tra lại bằng Collider thực tế để đảm bảo không bị hở map
        // Nếu file ảnh dài hơn collider hoặc ngược lại, ta tin vào Collider để tính va chạm
        float actualLen = length;
        var col = obj.GetComponent<BoxCollider2D>();

        if (col != null)
        {
            // Tính length thực tế dựa trên scale
            actualLen = col.size.x * obj.transform.localScale.x;

            // Nếu có sự chênh lệch lớn giữa config và thực tế, ta cần chỉnh lại vị trí obj
            // để mép trái của nó trùng khít với currentX
            if (Mathf.Abs(actualLen - length) > 0.05f)
            {
                // Dời vị trí sao cho mép trái (min X) = currentX
                // CenterX = MinX + HalfLength
                float correctedX = currentX + actualLen / 2f;
                obj.transform.position = new Vector3(correctedX, groundY, 0);
            }
        }

        // Điểm kết thúc của segment này (chính là điểm bắt đầu của segment sau)
        float endX = currentX + actualLen;

        currentSegmentLength += actualLen;

        return new GenerationResult
        {
            type = SegmentType.Ground,
            startX = currentX,
            endX = endX,
            segmentLenght = currentSegmentLength,
        };
    }
}
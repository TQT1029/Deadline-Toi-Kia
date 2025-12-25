using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class ObstacleMotionControl : MonoBehaviour
{
    public enum MotionType
    {
        None,
        MoveVertical,   // Lên xuống
        MoveHorizontal, // Trái phải
        PulseScale,     // Phồng to thu nhỏ
        CustomVector    // Theo vector tùy chỉnh
    }

    public enum GroupPattern
    {
        Sync,       // Tất cả chạy cùng lúc
        Wave,       // Chạy kiểu lượn sóng (tuần tự)
        Alternating // So le (Chẵn lẻ ngược nhau)
    }

    [Header("Configuration")]
    [SerializeField] private MotionType motionType = MotionType.MoveVertical;
    [SerializeField] private GroupPattern groupPattern = GroupPattern.Sync;

    [Header("Move Settings")]
    [SerializeField] private float moveDistance = 1.0f; // Khoảng cách di chuyển
    [SerializeField] private Vector3 customMoveOffset = Vector3.up; // Dùng cho CustomVector

    [Header("Scale Settings")]
    [SerializeField] private float scaleMultiplier = 1.2f; // Phóng to bao nhiêu lần

    [Header("Timing")]
    [SerializeField] private float duration = 1.0f; // Thời gian cho 1 nhịp
    [SerializeField] private Ease easeType = Ease.InOutSine; // Loại chuyển động (Mượt mà)

    [Tooltip("Độ trễ giữa các phần tử khi dùng Wave (càng lớn sóng càng rõ)")]
    [SerializeField] private float waveDelayStep = 0.2f;

    private List<DynamicObstaclePart> _parts = new List<DynamicObstaclePart>();

    private void Start()
    {
        SetupParts();
        ApplyMotion();
    }

    // Tương tự như BackgroundManager.FetchLayers()
    [ContextMenu("Fetch Parts")]
    private void SetupParts()
    {
        _parts.Clear();
        foreach (Transform child in transform)
        {
            // Tự động thêm script DynamicObstaclePart vào con nếu chưa có
            if (!child.TryGetComponent(out DynamicObstaclePart part))
            {
                part = child.gameObject.AddComponent<DynamicObstaclePart>();
            }
            _parts.Add(part);
        }
    }

    private void ApplyMotion()
    {
        if (_parts.Count == 0) SetupParts();

        for (int i = 0; i < _parts.Count; i++)
        {
            DynamicObstaclePart part = _parts[i];
            float delay = CalculateDelay(i);

            switch (motionType)
            {
                case MotionType.MoveVertical:
                    part.StartMove(Vector3.up * moveDistance, duration, easeType, delay);
                    break;

                case MotionType.MoveHorizontal:
                    part.StartMove(Vector3.right * moveDistance, duration, easeType, delay);
                    break;

                case MotionType.CustomVector:
                    part.StartMove(customMoveOffset, duration, easeType, delay);
                    break;

                case MotionType.PulseScale:
                    // Tính scale đích dựa trên scale hiện tại
                    Vector3 targetScale = part.transform.localScale * scaleMultiplier;
                    part.StartScale(targetScale, duration, easeType, delay);
                    break;
            }
        }
    }

    private float CalculateDelay(int index)
    {
        switch (groupPattern)
        {
            case GroupPattern.Sync:
                return 0f; // Không trễ, chạy cùng lúc

            case GroupPattern.Wave:
                return index * waveDelayStep; // Mỗi con chạy chậm hơn con trước 1 chút

            case GroupPattern.Alternating:
                // Con chẵn chạy ngay, con lẻ chạy một nửa chu kỳ sau (tạo cảm giác ngược nhau)
                return (index % 2 == 0) ? 0f : duration;

            default:
                return 0f;
        }
    }
}
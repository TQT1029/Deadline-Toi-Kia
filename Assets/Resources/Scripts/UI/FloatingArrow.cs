using UnityEngine;
using DG.Tweening; // Nhớ import thư viện DOTween

public class FloatingArrow : MonoBehaviour
{
    [Header("Cài đặt Hiệu ứng")]
    [Tooltip("Khoảng cách mũi tên di chuyển lên xuống")]
    public float moveDistance = 0.5f;

    [Tooltip("Thời gian để hoàn thành một nửa chu kỳ (giây)")]
    public float duration = 0.5f;

    private void Start()
    {
        // Lấy tọa độ Y hiện tại của mũi tên so với object cha (nhân vật)
        float targetY = transform.localPosition.y + moveDistance;

        // Tạo hiệu ứng di chuyển lên vị trí targetY, sau đó quay lại (Yoyo) và lặp vô hạn (-1)
        transform.DOLocalMoveY(targetY, duration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine); // Hiệu ứng làm mềm chuyển động ở 2 đầu
    }

    private void OnDestroy()
    {
        // Rất quan trọng: Hủy tween khi object bị xóa để tránh lỗi memory leak hoặc NullReferenceException
        transform.DOKill();
    }
}
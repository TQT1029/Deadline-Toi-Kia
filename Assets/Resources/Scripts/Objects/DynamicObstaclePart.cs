using UnityEngine;
using DG.Tweening; // Bắt buộc phải có DOTween

public class DynamicObstaclePart : MonoBehaviour
{
    private Tween _moveTween;
    private Tween _scaleTween;

    // Hàm nhận lệnh di chuyển
    public void StartMove(Vector3 offset, float duration, Ease easeType, float delay)
    {
        // Kill tween cũ nếu có
        _moveTween?.Kill();

        // Di chuyển qua lại (PingPong)
        // Sử dụng LocalMove để di chuyển tương đối so với cha
        _moveTween = transform.DOLocalMove(transform.localPosition + offset, duration)
            .SetEase(easeType)
            .SetLoops(-1, LoopType.Yoyo) // Lặp vô tận kiểu YoYo (đi rồi về)
            .SetDelay(delay); // Độ trễ để tạo hiệu ứng làn sóng/so le
    }

    // Hàm nhận lệnh biến đổi kích thước
    public void StartScale(Vector3 targetScale, float duration, Ease easeType, float delay)
    {
        _scaleTween?.Kill();

        _scaleTween = transform.DOScale(targetScale, duration)
            .SetEase(easeType)
            .SetLoops(-1, LoopType.Yoyo)
            .SetDelay(delay);
    }

    // Quan trọng: Dọn dẹp Tween khi object bị Destroy hoặc Disable
    private void OnDisable()
    {
        _moveTween?.Kill();
        _scaleTween?.Kill();
    }

    private void OnDestroy()
    {
        _moveTween?.Kill();
        _scaleTween?.Kill();
    }
}
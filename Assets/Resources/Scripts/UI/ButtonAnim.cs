using UnityEngine;
using UnityEngine.EventSystems; // Cần thiết để bắt sự kiện nhấn/thả
using DG.Tweening; // Bắt buộc có DOTween

public class ButtonAnim : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Animation Settings")]
    [SerializeField] private float pressScale = 0.9f; // Tỉ lệ khi nhấn xuống (nhỏ lại)
    [SerializeField] private float duration = 0.1f;   // Thời gian co giãn
    [SerializeField] private Ease easeType = Ease.OutQuad; // Kiểu chuyển động

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        // Đảm bảo khi bật lại nút thì nó về kích thước chuẩn
        transform.localScale = originalScale;
    }

    // Khi người chơi NHẤN chuột/ngón tay xuống
    public void OnPointerDown(PointerEventData eventData)
    {
        // Xóa các tween cũ đang chạy để tránh xung đột
        transform.DOKill();

        // Thu nhỏ nút lại
        // SetUpdate(true) cực quan trọng: Giúp nút vẫn nhún nhảy kể cả khi Time.timeScale = 0 (Game Pause)
        transform.DOScale(originalScale * pressScale, duration)
                 .SetEase(easeType)
                 .SetUpdate(true);
    }

    // Khi người chơi THẢ chuột/ngón tay ra
    public void OnPointerUp(PointerEventData eventData)
    {
        transform.DOKill();

        // Phóng to trở lại kích thước gốc với hiệu ứng đàn hồi nhẹ (OutElastic)
        transform.DOScale(originalScale, duration)
                 .SetEase(Ease.OutElastic) // Tạo cảm giác nảy như thạch
                 .SetUpdate(true);
    }

    // Phòng trường hợp nút bị tắt đột ngột khi đang nhấn -> reset scale
    private void OnDisable()
    {
        transform.DOKill();
        transform.localScale = originalScale;
    }
}
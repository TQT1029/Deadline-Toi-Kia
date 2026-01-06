using UnityEngine;
using DG.Tweening;

public class MoveObstacleBoss : MonoBehaviour
{
    // Chỉ thị di chuyển
    public void Initialize(Vector2 startViewportPos, Vector2 endViewportPos, float duration, float delayTime)
    {
        Camera cam = Camera.main;
        float depth = 10f; // Khoảng cách từ camera tới vật thể

        // 1. Tính toán vị trí World ban đầu dựa trên Viewport
        Vector3 startWorldPos = cam.ViewportToWorldPoint(new Vector3(startViewportPos.x, startViewportPos.y, depth));
        Vector3 endWorldPos = cam.ViewportToWorldPoint(new Vector3(endViewportPos.x, endViewportPos.y, depth));

        // 2. Set vị trí ban đầu
        transform.position = startWorldPos;

        // 3. Thực hiện di chuyển với DOTween
        // Dùng SetDelay để xử lý việc "thứ tự" (cái nào delay ít thì chạy trước)
        transform.DOMove(endWorldPos, duration)
            .SetDelay(delayTime)
            .SetEase(Ease.Linear)
            .OnStart(() => {
                // Có thể thêm logic bật hình ảnh hoặc âm thanh khi bắt đầu bay
                gameObject.SetActive(true);
            })
            .OnComplete(() => {
                // Bay xong ra khỏi màn hình thì hủy
                Destroy(gameObject);
            });
    }
}
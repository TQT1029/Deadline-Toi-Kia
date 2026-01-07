using UnityEngine;
using DG.Tweening;

public class MoveObstacleBoss : MonoBehaviour
{
    private Tween moveTween;
    private Tween rotateTween;

    // Hàm khởi tạo nhận vào Speed thay vì Duration
    public void Initialize(Vector2 startViewportPos, Vector2 endViewportPos, float speed, float rotateSpeed, float delayTime)
    {
        Camera cam = Camera.main;
        float depth = 10f;

        // 1. Chuyển đổi Viewport -> World
        // Lưu ý: Tôi đã bỏ +5 và -10 cứng để logic linh hoạt hơn cho các hướng khác nhau
        // Việc offset ngoài màn hình sẽ được tính toán bên Controller
        Vector3 startWorldPos = cam.ViewportToWorldPoint(new Vector3(startViewportPos.x, startViewportPos.y, depth));
        Vector3 endWorldPos = cam.ViewportToWorldPoint(new Vector3(endViewportPos.x - .1f, endViewportPos.y - .1f, depth));

        // 2. Đặt vị trí ban đầu
        transform.position = startWorldPos;

        // 3. Tính toán thời gian dựa trên tốc độ (Time = Distance / Speed)
        float distance = Vector3.Distance(startWorldPos, endWorldPos);
        float calculatedDuration = distance / speed;

        // 4. Di chuyển (Move)
        moveTween = transform.DOMove(endWorldPos, calculatedDuration)
            .SetDelay(delayTime)
            .SetEase(Ease.Linear)
            .OnStart(() =>
            {
                gameObject.SetActive(true);
                // Bắt đầu xoay khi bắt đầu di chuyển
                StartRotation(rotateSpeed);
            })
            .OnComplete(() =>
            {
                Destroy(gameObject);
            });
    }

    // Tách riêng logic xoay để dễ quản lý
    private void StartRotation(float rotateSpeed)
    {
        if (rotateSpeed == 0) return;

        // Xoay quanh trục Z (cho 2D) hoặc Random trục (cho 3D)
        // Ở đây tôi dùng xoay trục Z 360 độ liên tục
        rotateTween = transform.DORotate(new Vector3(0, 0, 360), 1f / rotateSpeed, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Incremental) // Lặp vô tận dạng cộng dồn
            .SetEase(Ease.Linear);
    }

    private void OnDestroy()
    {
        // Dọn dẹp Tween khi object bị hủy để tránh lỗi
        moveTween?.Kill();
        rotateTween?.Kill();
    }
}
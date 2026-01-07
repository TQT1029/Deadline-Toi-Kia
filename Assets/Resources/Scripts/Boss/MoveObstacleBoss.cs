using UnityEngine;
using DG.Tweening;

public class MoveObstacleBoss : MonoBehaviour
{
    private Tween moveTween;
    private Tween rotateTween;

    public void Initialize(Vector2 startViewportPos, Vector2 endViewportPos, float speed, float rotateSpeed, float delayTime)
    {
        Camera cam = Camera.main;
        float depth = 10f;

        // 1. Chuyển đổi Viewport -> World
        // Controller đã tính toán margin (lề) rồi nên ở đây ta chuyển đổi trực tiếp
        Vector3 startWorldPos = cam.ViewportToWorldPoint(new Vector3(startViewportPos.x, startViewportPos.y, depth));
        Vector3 endWorldPos = cam.ViewportToWorldPoint(new Vector3(endViewportPos.x, endViewportPos.y, depth));

        // 2. Đặt vị trí
        transform.position = startWorldPos;

        // Đảm bảo Z luôn đúng (phòng hờ Z bị lệch do ViewportToWorldPoint lấy Z của Cam)
        startWorldPos.z = 0;
        endWorldPos.z = 0;

        // 3. Tính Duration chuẩn xác
        float distance = Vector3.Distance(startWorldPos, endWorldPos);

        // Bảo vệ chia cho 0
        if (speed <= 0) speed = 1f;
        float calculatedDuration = distance / speed;

        // 4. Di chuyển
        // Ẩn trước khi delay xong
        gameObject.SetActive(false);

        moveTween = transform.DOMove(endWorldPos, calculatedDuration)
            .SetDelay(delayTime)
            .SetEase(Ease.Linear)
            .OnStart(() =>
            {
                gameObject.SetActive(true);
                StartRotation(rotateSpeed);
            })
            .OnComplete(() =>
            {
                // Tự hủy khi đến đích (đích đã được tính là nằm ngoài màn hình)
                Destroy(gameObject);
            });
    }

    private void StartRotation(float rotateSpeed)
    {
        if (rotateSpeed <= 0) return;

        rotateTween = transform.DORotate(new Vector3(0, 0, 360), 1f / rotateSpeed, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Incremental)
            .SetEase(Ease.Linear);
    }

    private void OnDestroy()
    {
        moveTween?.Kill();
        rotateTween?.Kill();
    }
}
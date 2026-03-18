using UnityEngine;
using DG.Tweening; // Giữ lại chỉ để dùng cho Rotate (Xoay)

public class TargetProjecties : MonoBehaviour
{
    private Tween rotateTween;

    [SerializeField] private Transform targetPoint;

    private Vector3 moveDirection;
    private float moveSpeed;
    private bool isMoving = false;

    // Thời gian tối đa tồn tại (để tránh rác bộ nhớ nếu vật bay ra khỏi màn hình quá xa)
    private float maxLifetime = 3f;

    private void Awake()
    {
        if (targetPoint == null) { targetPoint = ReferenceManager.Instance.PlayerTransform; }
    }

    /// <summary>
    /// Khởi tạo vật thể với logic Vector.
    /// Lưu ý: Đầu vào nên là World Position (đã được convert ở Controller) để tối ưu.
    /// </summary>
    public void Initialize(Vector3 startWorldPos, Vector3 targetWorldPos, float speed, float rotateSpeed, float delayTime)
    {
        // 1. Đặt vị trí xuất phát ngay lập tức
        transform.position = startWorldPos;

        // 2. Tính toán Hướng di chuyển (Normalized để chỉ lấy hướng, độ dài = 1)
        // Công thức: Đích - Đầu
        moveDirection = (targetWorldPos - startWorldPos).normalized;
        moveSpeed = speed;

        // 3. Bắt đầu quy trình (Delay -> Hiện -> Bay & Xoay)
        StartCoroutine(StartMovingProcess(delayTime, rotateSpeed));
    }

    private System.Collections.IEnumerator StartMovingProcess(float delay, float rotateSpeed)
    {
        // Chờ delay
        if (delay > 0) yield return new WaitForSeconds(delay);

        // Kích hoạt vật thể
        gameObject.SetActive(true);
        isMoving = true;

        // Bắt đầu xoay (Dùng Dotween cho việc này rất tốt vì nó mượt)
        StartRotation(rotateSpeed);

        // Tự hủy sau 10s (Cơ chế dọn rác an toàn)
        Destroy(gameObject, maxLifetime);
    }

    // --- LOGIC DI CHUYỂN MỚI ---
    private void Update()
    {
        // Chỉ di chuyển khi đã hết thời gian delay
        if (!isMoving) return;

        // Công thức Vector: Vị trí mới = Vị trí cũ + (Hướng * Tốc độ * Thời gian)
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }

    private void StartRotation(float rotateSpeed)
    {
        if (rotateSpeed <= 0) return;

        // Logic xoay giữ nguyên, nhưng thêm SetEase(Ease.Linear) để xoay đều
        rotateTween = transform.DORotate(new Vector3(0, 0, 360), 1f / rotateSpeed, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Incremental)
            .SetEase(Ease.Linear);
    }

    private void OnDisable()
    {
        // Khi object bị tắt hoặc hủy, phải giết tween xoay để tránh lỗi
        rotateTween?.Kill();
    }
}
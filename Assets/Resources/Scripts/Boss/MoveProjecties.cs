using UnityEngine;

public class MoveProjecties : MonoBehaviour
{
    private Vector3 moveDirection;
    private float moveSpeed;

    private Rigidbody2D rb;
    private float currentRotateSpeed;

    // Thời gian tối đa tồn tại (để tránh rác bộ nhớ nếu vật bay ra khỏi màn hình quá xa)
    private float maxLifetime = 3f;

    /// <summary>
    /// Khởi tạo vật thể với logic Vector.
    /// Lưu ý: Đầu vào nên là World Position (đã được convert ở Controller) để tối ưu.
    /// </summary>
    public void Initialize(Vector3 startWorldPos, Vector3 targetWorldPos, float speed, float rotateSpeed, float delayTime)
    {
        rb = GetComponent<Rigidbody2D>(); // Nên cache ở Awake nếu pool object
        transform.position = startWorldPos;

        moveDirection = (targetWorldPos - startWorldPos).normalized;
        moveSpeed = speed;
        currentRotateSpeed = rotateSpeed;

        StartCoroutine(StartMovingProcess(delayTime));
    }

    private System.Collections.IEnumerator StartMovingProcess(float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        gameObject.SetActive(true);

        // Dùng linearVelocity và angularVelocity của Unity Physics
        rb.linearVelocity = moveDirection * moveSpeed;

        // Code cũ: xoay 360 độ trong (1/rotateSpeed) giây -> Tương đương rotateSpeed vòng/giây
        rb.angularVelocity = currentRotateSpeed * 360f;

        Destroy(gameObject, maxLifetime);
    }
}
using UnityEngine;

public class BotController : BaseRunner
{
    [Header("AI Radar (Sweeping Raycast)")]
    public Transform sensorPoint;
    public float viewDistance = 5.0f;
    public LayerMask obstacleLayer;

    [Tooltip("Góc quét tối đa (Lên/Xuống). VD: 30 độ")]
    public float maxSweepAngle = 30f;

    [Tooltip("Tốc độ quét (Radar quay nhanh hay chậm). Cao = Chính xác hơn")]
    public float sweepSpeed = 10f;

    [Header("Rubber Banding")]
    public Transform targetPlayer;
    public float catchUpMult = 1.3f;
    public float slowDownMult = 0.8f;

    private bool isJumpCooldown = false;

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        PerformRadarScan(); // Quét liên tục
        AdjustSpeed();
    }

    // --- 1. LOGIC RADAR QUÉT (LÊN XUỐNG) ---
    private void PerformRadarScan()
    {
        if (!isGrounded || isJumpCooldown) return;

        // Tạo góc dao động hình sin theo thời gian: Từ -Max đến +Max
        float currentAngle = Mathf.Sin(Time.time * sweepSpeed) * maxSweepAngle;

        // Tính hướng vector dựa trên góc
        Vector2 direction = Quaternion.Euler(0, 0, currentAngle) * Vector2.right;

        // Bắn tia
        RaycastHit2D hit = Physics2D.Raycast(sensorPoint.position, direction, viewDistance, obstacleLayer);

        // Vẽ màu để debug: Đỏ = Trúng, Xanh = An toàn
        Debug.DrawRay(sensorPoint.position, direction * viewDistance, hit.collider ? Color.red : Color.green);

        if (hit.collider != null)
        {
            // Phát hiện vật cản -> Nhảy
            Jump();
            isJumpCooldown = true;
            Invoke(nameof(ResetJumpCooldown), 0.5f);
        }
    }

    // --- 2. LOGIC TỰ THÁO KẸT (BOT) ---
    protected override void OnStuck()
    {
        base.OnStuck();
        // Khi Bot bị kẹt, nó sẽ thử nhảy "Panic Jump" để thoát ra
        if (isGrounded)
        {
            Jump();
            // Nếu vẫn kẹt quá lâu, có thể teleport nhẹ lên trước (Cheat)
            transform.position += Vector3.right * 1.5f + Vector3.up * 0.5f;
        }
    }

    // --- 3. LOGIC ĐUỔI THEO ---
    private void AdjustSpeed()
    {
        if (targetPlayer == null) return;
        float dist = transform.position.x - targetPlayer.position.x;

        if (dist < -10f) currentSpeed = baseRunSpeed * catchUpMult;
        else if (dist > 10f) currentSpeed = baseRunSpeed * slowDownMult;
        else currentSpeed = baseRunSpeed;
    }

    private void ResetJumpCooldown() => isJumpCooldown = false;
}
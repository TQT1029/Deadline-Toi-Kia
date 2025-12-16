using UnityEngine;

public class BotController : BaseRunner
{
    [Header("Bot Personality (Randomized)")]
    [Tooltip("Độ trễ phản xạ khi thấy vật cản (0.05s - 0.2s)")]
    private float reactionTime;
    private float speedNoiseSeed;

    [Header("Rubber Banding Config")]
    [SerializeField] private float adjustDist = 10f;
    public Transform targetPlayer;

    private float myCatchUpMult;
    private float mySlowDownMult;
    private float myAccelerationRate;

    [Header("AI Radar (Sweeping Raycast)")]
    public Transform sensorPoint;
    public float viewDistance = 5.0f;
    public LayerMask obstacleLayer;

    public float maxSweepAngle = 30f;
    private float phiDelta;
    private float mySweepSpeed;

    [Header("High Jump Logic (Nhảy cao thông minh)")]
    [Tooltip("Lực cộng thêm khi cần nhảy cao (Giống Player)")]
    public float jumpHoldForce = 5f;
    [Tooltip("Thời gian tối đa được phép 'giữ nút' ảo")]
    public float maxJumpHoldTime = 0.35f;

    private bool isHighJumping = false; // Bot có đang cố nhảy cao không?
    private float highJumpTimer;        // Thời gian còn lại để giữ nhảy

    [Header("Map Safety (Chống rơi khỏi map)")]
    public float fallThresholdY = -10f;
    public float respawnHeightOffset = 5f;

    private bool isJumpCooldown = false;
    private float targetRunSpeed;

    protected override void Awake()
    {
        base.Awake();

        // --- RANDOM HÓA TÍNH CÁCH BOT ---
        speedNoiseSeed = Random.Range(0f, 100f);
        phiDelta = Random.Range(0f, 180f);
        mySweepSpeed = Random.Range(8f, 15f);
        myCatchUpMult = Random.Range(1.2f, 1.5f);
        mySlowDownMult = Random.Range(0.7f, 0.9f);
        myAccelerationRate = Random.Range(1.5f, 3.0f);
        reactionTime = Random.Range(0.05f, 0.15f);
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        // 1. Quét Radar và lấy kết quả xem có thấy vật cản không
        bool seesObstacle = PerformRadarScan();

        // 2. Xử lý Logic Nhảy Cao (Nếu đang nhảy mà vẫn thấy vật cản -> Bơm thêm lực)
        HandleHighJumpLogic(seesObstacle);

        AdjustSpeedTarget();
        CheckMapFallSafety();
    }

    protected override void Move()
    {
        float scoreBonus = (GameStatsController.Instance != null) ? GameStatsController.Instance.resultDistance / 150f : 0f;
        float desiredSpeed = targetRunSpeed + scoreBonus;
        float noise = (Mathf.PerlinNoise(Time.time * 0.5f, speedNoiseSeed) - 0.5f) * 2f;
        desiredSpeed += noise;

        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, myAccelerationRate * Time.fixedDeltaTime);

        base.Move();
    }

    // --- LOGIC 1: RADAR QUÉT (Trả về true nếu thấy vật cản) ---
    private bool PerformRadarScan()
    {
        // Vẫn quét kể cả khi đang nhảy để phục vụ logic High Jump
        float currentAngle = Mathf.Sin(Time.time * mySweepSpeed + phiDelta) * maxSweepAngle;
        Vector2 direction = Quaternion.Euler(0, 0, currentAngle) * Vector2.right;

        RaycastHit2D hit = Physics2D.Raycast(sensorPoint.position, direction, viewDistance, obstacleLayer);

        // Debug vẽ tia (Đỏ = Thấy, Xanh = Không)
        Debug.DrawRay(sensorPoint.position, direction * viewDistance, hit.collider ? Color.red : Color.green);

        // Logic kích hoạt nhảy ban đầu (Chỉ khi ở dưới đất)
        if (hit.collider != null && isGrounded && !isJumpCooldown)
        {
            if (!IsInvoking(nameof(PerformJumpAction)))
                Invoke(nameof(PerformJumpAction), reactionTime);
        }

        return hit.collider != null;
    }

    // --- LOGIC 2: BẮT ĐẦU NHẢY ---
    private void PerformJumpAction()
    {
        if (isGrounded && !isJumpCooldown)
        {
            Jump(); // Nhảy cơ bản (Force Impulse)

            // Bắt đầu trạng thái High Jump (Giả lập việc nhấn giữ nút)
            isHighJumping = true;
            highJumpTimer = maxJumpHoldTime;

            isJumpCooldown = true;
            Invoke(nameof(ResetJumpCooldown), 0.5f);
        }
    }

    // --- LOGIC 3: XỬ LÝ NHẢY CAO (GIỮ NÚT ẢO) ---
    private void HandleHighJumpLogic(bool seesObstacle)
    {
        // Nếu Bot đang trong trạng thái nhảy cao
        if (isHighJumping)
        {
            // Điều kiện để tiếp tục bơm lực:
            // 1. Còn thời gian giữ (highJumpTimer > 0)
            // 2. VẪN CÒN NHÌN THẤY VẬT CẢN (seesObstacle == true)
            //    -> Nghĩa là vật cản cao, nhảy thường chưa qua được đỉnh của nó.
            if (highJumpTimer > 0 && seesObstacle)
            {
                _rb.AddForce(Vector2.up * jumpHoldForce, ForceMode2D.Force);
                highJumpTimer -= Time.fixedDeltaTime;
            }
            else
            {
                // Hết giờ hoặc đã vượt qua vật cản (không thấy nữa) -> Ngắt lực
                isHighJumping = false;
            }
        }
    }

    private void AdjustSpeedTarget()
    {
        if (targetPlayer == null)
        {
            targetRunSpeed = baseRunSpeed;
            return;
        }

        float dist = transform.position.x - targetPlayer.position.x;

        if (dist < -adjustDist) targetRunSpeed = baseRunSpeed * myCatchUpMult;
        else if (dist > adjustDist) targetRunSpeed = baseRunSpeed * mySlowDownMult;
        else targetRunSpeed = baseRunSpeed;
    }

    // --- Map Safety ---
    private void CheckMapFallSafety()
    {
        if (transform.position.y < fallThresholdY)
        {
            if (targetPlayer != null)
            {
                float keepX = transform.position.x;
                float safeY = Mathf.Max(targetPlayer.position.y, -2f) + respawnHeightOffset;
                transform.position = new Vector3(keepX, safeY, 0);

#if UNITY_6000_0_OR_NEWER
                _rb.linearVelocity = Vector2.zero;
#else
                _rb.velocity = Vector2.zero;
#endif
                currentSpeed = baseRunSpeed;
            }
        }
    }

    protected override void OnStuck()
    {
        base.OnStuck();
        if (isGrounded)
        {
            Jump();
            transform.position += new Vector3(1.0f, 0.5f, 0);
            currentSpeed = baseRunSpeed;
        }
    }

    private void ResetJumpCooldown() => isJumpCooldown = false;
}
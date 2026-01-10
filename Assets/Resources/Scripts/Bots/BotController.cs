using UnityEngine;

public class BotController : BaseRunner
{
    [Header("Bot AI Config")]
    private float reactionTime;
    private float speedNoiseSeed;
    [SerializeField] private float adjustDist = 10f;
    public Transform targetPlayer;

    // AI Params
    private float myCatchUpMult;
    private float mySlowDownMult;
    private float myAccelerationRate;

    [Header("AI Sensors")]
    public Transform sensorPoint;
    public float viewDistance = 5.0f;
    public LayerMask obstacleLayer;
    public float maxSweepAngle = 30f;

    // Pit Check
    public float pitCheckDistance = 1.5f;
    public float pitRayLength = 5.0f;

    [Header("High Jump")]
    public float jumpHoldForce = 5f;
    public float maxJumpHoldTime = 0.35f;
    private bool isHighJumping = false;
    private float highJumpTimer;

    // Internal State
    private bool isJumpCooldown = false;
    private float targetRunSpeed;
    private float phiDelta;
    private float mySweepSpeed;

    protected override void Awake()
    {
        base.Awake();
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
        // QUAN TRỌNG: Nếu bị choáng (bị đạn boss bắn), Bot cũng phải ngừng suy nghĩ
        if (isControlLocked)
        {
            base.FixedUpdate(); // Vẫn gọi base để check ground/pit fall
            return;
        }

        base.FixedUpdate();

        // 1. Logic cảm biến
        bool seesObstacle = PerformRadarScan();
        bool seesPit = false;

        if (!seesObstacle) seesPit = ScanForPits();

        // 2. Logic giữ nút nhảy (High Jump)
        HandleHighJumpLogic(seesObstacle || seesPit);

        // 3. Logic đuổi theo Player
        AdjustSpeedTarget();
    }

    protected override void Move()
    {
        if (isControlLocked) return;

        // Tính toán vận tốc mong muốn (Rubber Banding)
        float distanceBonus = (GameStatsController.Instance != null) ? GameStatsController.Instance.resultDistance / 150f : 0f;
        float desiredSpeed = targetRunSpeed + distanceBonus;

        // Thêm chút nhiễu (Noise) để bot chạy không quá đều
        float noise = (Mathf.PerlinNoise(Time.time * 0.5f, speedNoiseSeed) - 0.5f) * 2f;
        desiredSpeed += noise;

        // Lerp speed
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, myAccelerationRate * Time.fixedDeltaTime);

        base.Move();
    }

    // --- SENSORS & AI LOGIC ---

    private bool ScanForPits()
    {
        if (!isGrounded || isJumpCooldown) return false;

        Vector2 origin = (Vector2)transform.position + (Vector2.right * pitCheckDistance) + Vector2.up;
        RaycastHit2D groundHit = Physics2D.Raycast(origin, Vector2.down, pitRayLength, groundLayer);

        // Nếu không chạm gì -> Hố
        if (groundHit.collider == null)
        {
            if (!IsInvoking(nameof(PerformJumpAction))) Invoke(nameof(PerformJumpAction), 0f); // Phản xạ ngay
            return true;
        }
        return false;
    }

    private bool PerformRadarScan()
    {
        float currentAngle = Mathf.Sin(Time.time * mySweepSpeed + phiDelta) * maxSweepAngle;
        Vector2 direction = Quaternion.Euler(0, 0, currentAngle) * Vector2.right;

        // Debug tia quét để dễ nhìn trong Scene
        Debug.DrawRay(sensorPoint.position, direction * viewDistance, Color.red);

        RaycastHit2D hit = Physics2D.Raycast(sensorPoint.position, direction, viewDistance, obstacleLayer);

        if (hit.collider != null && isGrounded && !isJumpCooldown)
        {
            // Delay phản xạ tùy vào loại vật cản
            if (!IsInvoking(nameof(PerformJumpAction)))
                Invoke(nameof(PerformJumpAction), reactionTime);
            return true;
        }
        return false;
    }

    private void PerformJumpAction()
    {
        if (isGrounded && !isJumpCooldown && !isControlLocked)
        {
            Jump();
            isHighJumping = true;
            highJumpTimer = maxJumpHoldTime;
            isJumpCooldown = true;
            Invoke(nameof(ResetJumpCooldown), 0.5f);
        }
    }

    private void HandleHighJumpLogic(bool needsHighJump)
    {
        if (isHighJumping && !isControlLocked)
        {
            if (highJumpTimer > 0 && needsHighJump)
            {
                _rb.AddForce(Vector2.up * jumpHoldForce, ForceMode2D.Force);
                highJumpTimer -= Time.fixedDeltaTime;
            }
            else isHighJumping = false;
        }
    }

    private void AdjustSpeedTarget()
    {
        if (targetPlayer == null) { targetRunSpeed = baseRunSpeed; return; }

        float dist = transform.position.x - targetPlayer.position.x;

        // Nếu bị bỏ xa (-adjustDist) -> Tăng tốc
        if (dist < -adjustDist) targetRunSpeed = baseRunSpeed * myCatchUpMult;
        // Nếu chạy quá xa (+adjustDist) -> Giảm tốc
        else if (dist > adjustDist) targetRunSpeed = baseRunSpeed * mySlowDownMult;
        // Bình thường -> Tốc độ gốc
        else targetRunSpeed = baseRunSpeed;
    }

    private void ResetJumpCooldown() => isJumpCooldown = false;

    // Reset Bot khi Respawn
    protected override void OnRespawn()
    {
        base.OnRespawn();
        if (targetPlayer != null)
        {
            // Logic phạt: Đặt Bot ra sau Player một đoạn
            float safeY = Mathf.Max(targetPlayer.position.y, -2f) + 5f;
            float punishX = targetPlayer.position.x - 5f;

            transform.position = new Vector3(punishX, safeY, 0);
            isHighJumping = false;
            highJumpTimer = 0;
            isJumpCooldown = false;
        }
    }
}
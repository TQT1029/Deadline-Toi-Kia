using UnityEngine;

public class BotController : BaseRunner
{
    [Header("Bot Personality")]
    private float reactionTime;
    private float speedNoiseSeed;

    [Header("Rubber Banding")]
    [SerializeField] private float adjustDist = 10f;
    public Transform targetPlayer;

    private float myCatchUpMult;
    private float mySlowDownMult;
    private float myAccelerationRate;

    [Header("AI Radar (Vật cản)")]
    public Transform sensorPoint;
    public float viewDistance = 5.0f;
    public LayerMask obstacleLayer;
    public float maxSweepAngle = 30f;
    private float phiDelta;
    private float mySweepSpeed;

    [Header("Pit Avoidance (Cảm biến né hố - NEW)")]
    [Tooltip("Khoảng cách dò hố phía trước mặt (Nên để ngắn hơn viewDistance)")]
    public float pitCheckDistance = 1.5f;
    [Tooltip("Độ sâu tối đa để tia dò tìm đất")]
    public float pitRayLength = 5.0f;

    [Header("High Jump Logic")]
    public float jumpHoldForce = 5f;
    public float maxJumpHoldTime = 0.35f;
    private bool isHighJumping = false;
    private float highJumpTimer;

    [Header("Bot Respawn Config")]
    public float respawnHeightOffset = 5f;
    public float penaltyDistance = 5f;

    private bool isJumpCooldown = false;
    private float targetRunSpeed;

    protected override void Awake()
    {
        base.Awake();
        // Random chỉ số
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

        // 1. Quét vật cản (Logic cũ)
        bool seesObstacle = PerformRadarScan();

        // 2. Quét hố (Logic MỚI)
        // Nếu không thấy vật cản thì mới check hố để tránh nhảy loạn xạ
        bool seesPit = false;
        if (!seesObstacle)
        {
            seesPit = ScanForPits();
        }

        // 3. Xử lý nhảy cao nếu gặp vật cản hoặc hố quá rộng
        // (Nếu thấy hố, ta cũng kích hoạt logic High Jump để bot bay xa hơn)
        HandleHighJumpLogic(seesObstacle || seesPit);

        AdjustSpeedTarget();
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

    // --- LOGIC MỚI: QUÉT HỐ (SCAN FOR PITS) ---
    private bool ScanForPits()
    {
        // Chỉ quét khi đang ở dưới đất và không trong thời gian hồi chiêu nhảy
        if (!isGrounded || isJumpCooldown) return false;

        // Điểm bắt đầu tia: Từ vị trí Bot + Dịch lên trước một đoạn (pitCheckDistance)
        // Cộng thêm Vector3.up * 1f để tia bắn từ trên cao xuống (ngang bụng/đầu)
        Vector2 origin = (Vector2)transform.position + (Vector2.right * pitCheckDistance) + Vector2.up;

        // Bắn tia thẳng xuống dưới
        // Lưu ý: groundLayer được lấy từ script cha BaseRunner
        RaycastHit2D groundHit = Physics2D.Raycast(origin, Vector2.down, pitRayLength, groundLayer);

        // Vẽ Debug: Màu Xanh = Có đất (An toàn), Màu Đỏ = Không có đất (Hố!)
        Debug.DrawRay(origin, Vector2.down * pitRayLength, groundHit.collider != null ? Color.blue : Color.magenta);

        // Nếu KHÔNG chạm gì (collider == null) -> Có nghĩa là phía trước không có đất -> Hố
        if (groundHit.collider == null)
        {
            // Kích hoạt nhảy ngay lập tức
            if (!IsInvoking(nameof(PerformJumpAction)))
            {
                // Nhảy hố cần phản xạ nhanh hơn nhảy vật cản một chút
                Invoke(nameof(PerformJumpAction), 0f);
            }
            return true; // Báo là có hố
        }

        return false; // An toàn
    }

    // --- CÁC HÀM CŨ GIỮ NGUYÊN ---
    protected override void OnRespawn()
    {
        base.OnRespawn();
        if (targetPlayer != null)
        {
            float safeY = Mathf.Max(targetPlayer.position.y, -2f) + respawnHeightOffset;
            float punishX = transform.position.x - penaltyDistance;
            if (punishX < targetPlayer.position.x - 15f)
                punishX = targetPlayer.position.x - 15f;
            transform.position = new Vector3(punishX, safeY, 0);
            isHighJumping = false;
            highJumpTimer = 0;
        }
    }

    protected override void OnStuck()
    {
        if (isGrounded)
        {
            Jump();
            transform.position += new Vector3(1.0f, 0.5f, 0);
            currentSpeed = baseRunSpeed;
            stuckTimer = 0f;
        }
        else base.OnStuck();
    }

    private bool PerformRadarScan()
    {
        float currentAngle = Mathf.Sin(Time.time * mySweepSpeed + phiDelta) * maxSweepAngle;
        Vector2 direction = Quaternion.Euler(0, 0, currentAngle) * Vector2.right;
        RaycastHit2D hit = Physics2D.Raycast(sensorPoint.position, direction, viewDistance, obstacleLayer);

        if (hit.collider != null && isGrounded && !isJumpCooldown)
        {
            if (hit.collider.CompareTag("MiniPlatform") && RandomUtils.ChancePercent(50))
            {
                if (!IsInvoking(nameof(PerformJumpAction)))
                    Invoke(nameof(PerformJumpAction), reactionTime);
            }
            else
            {
                if (!IsInvoking(nameof(PerformJumpAction)))
                    Invoke(nameof(PerformJumpAction), reactionTime);
            }
        }
        return hit.collider != null;
    }

    private void PerformJumpAction()
    {
        if (isGrounded && !isJumpCooldown)
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
        if (isHighJumping)
        {
            // Nếu vẫn thấy chướng ngại vật HOẶC đang nhảy qua hố -> Tiếp tục bơm lực
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
        if (dist < -adjustDist) targetRunSpeed = baseRunSpeed * myCatchUpMult;
        else if (dist > adjustDist) targetRunSpeed = baseRunSpeed * mySlowDownMult;
        else targetRunSpeed = baseRunSpeed;
    }

    private void ResetJumpCooldown() => isJumpCooldown = false;
}
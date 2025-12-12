using UnityEngine;

public class BotController : BaseRunner
{
    [Header("Bot Personality (Randomized)")]
    [Tooltip("Độ trễ phản xạ khi thấy vật cản (0.05s - 0.2s)")]
    private float reactionTime;
    private float speedNoiseSeed;

    [Header("Rubber Banding Config")]
    [SerializeField] private float adjustDist = 20f;
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

    [Header("Map Safety (Chống rơi khỏi map)")]
    [Tooltip("Độ cao Y mà nếu Bot rơi xuống dưới mức này sẽ bị coi là lọt map")]
    public float fallThresholdY = -10f;
    [Tooltip("Bot sẽ được hồi sinh cao hơn Player bao nhiêu mét?")]
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
        reactionTime = Random.Range(0.05f, 0.2f);
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        PerformRadarScan();
        AdjustSpeedTarget();

        // --- LOGIC MỚI: KIỂM TRA RƠI KHỎI MAP ---
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

    // --- LOGIC AN TOÀN KHI RƠI KHỎI MAP ---
    private void CheckMapFallSafety()
    {
        // Nếu Bot rơi xuống quá sâu (dưới fallThresholdY)
        if (transform.position.y < fallThresholdY)
        {
            if (targetPlayer != null)
            {
                // 1. Giữ nguyên vị trí X (Tiến độ chạy)
                float keepX = transform.position.x;

                // 2. Lấy vị trí Y của Player + Offset (Để đảm bảo Bot rơi từ trên cao xuống sàn an toàn)
                // Nếu Player lỡ cũng rơi thì dùng tạm 0 hoặc groundY mặc định
                float safeY = Mathf.Max(targetPlayer.position.y, -2f) + respawnHeightOffset;

                // 3. Teleport Bot
                transform.position = new Vector3(keepX, safeY, 0);

                // 4. Quan trọng: Reset vận tốc rơi (để không bị rơi tiếp với tốc độ tên lửa)
#if UNITY_6000_0_OR_NEWER
                _rb.linearVelocity = Vector2.zero;
#else
                _rb.velocity = Vector2.zero;
#endif
                // Reset tốc độ chạy về cơ bản để Bot lấy lại nhịp
                currentSpeed = baseRunSpeed;

                // Debug.Log($"{gameObject.name} lọt map! Đã tele lên cao.");
            }
        }
    }

    // --- CÁC LOGIC CŨ GIỮ NGUYÊN ---
    private void PerformRadarScan()
    {
        if (!isGrounded || isJumpCooldown) return;

        float currentAngle = Mathf.Sin(Time.time * mySweepSpeed + phiDelta) * maxSweepAngle;
        Vector2 direction = Quaternion.Euler(0, 0, currentAngle) * Vector2.right;
        RaycastHit2D hit = Physics2D.Raycast(sensorPoint.position, direction, viewDistance, obstacleLayer);

        if (hit.collider != null)
        {
            if (!IsInvoking(nameof(PerformJumpAction))) Invoke(nameof(PerformJumpAction), reactionTime);
        }
    }

    private void PerformJumpAction()
    {
        if (isGrounded && !isJumpCooldown)
        {
            Jump();
            isJumpCooldown = true;
            Invoke(nameof(ResetJumpCooldown), 0.5f);
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
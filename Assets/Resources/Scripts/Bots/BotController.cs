using DG.Tweening;
using UnityEditor.Animations;
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
    [SerializeField] private Transform sensorPoint;
    [SerializeField] private float viewDistance = 5.0f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float maxSweepAngle = 30f;

    // Pit Check
    [SerializeField] private float pitCheckDistance = 1.5f;
    [SerializeField] private float pitRayLength = 5.0f;

    [Header("High Jump")]
    [SerializeField] private float jumpHoldForce = 5f;
    private float maxJumpHoldTime = 0.35f;
    private bool isHighJumping = false;
    private float highJumpTimer;

    // Internal State
    private bool isJumpCooldown = false;
    private float targetRunSpeed;
    private float phiDelta;
    private float mySweepSpeed;

    [Header("Animation NPC")]
    [SerializeField] private RuntimeAnimatorController[] npcAnimations = new RuntimeAnimatorController[12];
    [SerializeField] private Animator animator;
    private RandomUtils.ShuffleBag<RuntimeAnimatorController> randomAnimations;

    [Header("Optimization Settings")]
    [Tooltip("Khoảng cách tối đa so với Player để Bot còn bật AI. Xa hơn sẽ tắt Raycast.")]
    [SerializeField] private float cullDistance = 25f;

    [Tooltip("Thời gian giữa các lần quét AI (giây). 0.1 = 10 lần/giây.")]
    [SerializeField] private float aiUpdateInterval = 0.1f;

    private float nextNoiseUpdate = 0;
    private float noise = 0;

    private float nextAiUpdateTime;
    private bool isAiActive = true;

    protected override void Awake()
    {
        base.Awake();
        if (npcAnimations != null && npcAnimations.Length > 0)
        {
            randomAnimations = new RandomUtils.ShuffleBag<RuntimeAnimatorController>(npcAnimations);
        }

        speedNoiseSeed = Random.Range(0f, 100000f);
        phiDelta = Random.Range(0f, 180f);
        mySweepSpeed = Random.Range(8f, 15f);
        myCatchUpMult = Random.Range(1.2f, 1.5f);
        mySlowDownMult = Random.Range(0.7f, 0.9f);
        myAccelerationRate = Random.Range(1.5f, 3.0f);
        reactionTime = Random.Range(0.05f, 0.35f);
    }

    protected override void Start()
    {
        base.Start();



        if (targetPlayer == null)
            targetPlayer = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator != null)
        {
            animator.runtimeAnimatorController = randomAnimations.Next();
        }
        // Random thời gian cập nhật AI ban đầu để tránh đồng bộ
        nextAiUpdateTime = Time.time + Random.Range(0f, aiUpdateInterval);
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

        // Logic HighJump
        HandleHighJumpLogic();

        float curTime = Time.time;

        // Giãn cách khoảng thời gian cập nhật AI
        MoveLogic(curTime);

        if (curTime >= nextAiUpdateTime)
        {
            // Thay đổi thời gian phản ứng
            reactionTime = Random.Range(0.05f, 0.35f);

            RunAiLogic();
            nextAiUpdateTime = curTime + aiUpdateInterval;
        }
    }
    private void MoveLogic(float curTime)
    {
        // Tính toán vận tốc mong muốn (Rubber Banding)
        float distanceBonus = (GameStatsController.Instance != null) ? GameStatsController.Instance.resultDistance / 150f : 0f;
        float desiredSpeed = targetRunSpeed + distanceBonus;
        if (curTime >= nextNoiseUpdate)
        {
            noise = MathUtils.ClampPerlinNoise1D(curTime, -1.5f, 1.5f, speedNoiseSeed);

            //Debug.Log(this.name + " Noise: " + noise);

            nextNoiseUpdate = curTime + 0.1f; // Cập nhật noise mỗi 0.1 giây
        }
        desiredSpeed += noise;

        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, myAccelerationRate * Time.fixedDeltaTime);
    }

    private void RunAiLogic()
    {
        if (targetPlayer != null)
        {

            float dist = transform.position.x - targetPlayer.position.x;

            //Debug.Log("dist: " + dist);
            if (dist > cullDistance)
            {
                isAiActive = false;

                if (transform.position.x < targetPlayer.position.x)
                    targetRunSpeed = baseRunSpeed * myCatchUpMult;
                else
                    targetRunSpeed = baseRunSpeed * mySlowDownMult;

                return;

            }
            else
            {
                isAiActive = true;
            }
        }

        if (isAiActive)
        {
            bool seesObstacle = PerformRadarScan();
            bool seesPit = false;

            if (!seesObstacle) seesPit = ScanForPits();

            // Tinh chỉnh tốc độ mục tiêu dựa trên vị trí Player
            AdjustSpeedTarget();


        }
    }
    protected override void Move()
    {
        if (isControlLocked) return;

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
            if (!IsInvoking(nameof(PerformJumpAction))) Invoke(nameof(PerformJumpAction), reactionTime); // Phản xạ ngay
            return true;
        }
        return false;
    }

    private bool PerformRadarScan()
    {
        if (!isAiActive) return false;

        float currentAngle = Mathf.Sin(Time.time * mySweepSpeed + phiDelta) * maxSweepAngle;
        Vector2 direction = Quaternion.Euler(0, 0, currentAngle) * Vector2.right;

        Debug.DrawRay(sensorPoint.position, direction * viewDistance, Color.red);

        RaycastHit2D hit = Physics2D.Raycast(sensorPoint.position, direction, viewDistance, obstacleLayer);

        if (hit.collider != null && isGrounded && !isJumpCooldown)
        {
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

    // Xóa tham số bool needsHighJump
    private void HandleHighJumpLogic()
    {
        if (isHighJumping && !isControlLocked)
        {
            if (highJumpTimer > 0)
            {
                _rb.AddForce(Vector2.up * jumpHoldForce, ForceMode2D.Force);
                highJumpTimer -= Time.fixedDeltaTime;
            }
            else
            {
                isHighJumping = false;
            }
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
            float safeY = Mathf.Max(targetPlayer.position.y, -2f) + 5f;
            float punishX = transform.position.x;

            // Nếu Rớt phìa sau player quá xa mới cần tp lại
            if (transform.position.x - targetPlayer.transform.position.x < -10)
                punishX = targetPlayer.position.x - 8f;

            transform.position = new Vector3(punishX, safeY, 0);

            isHighJumping = false;
            highJumpTimer = 0;
            isJumpCooldown = false;
        }
    }
}
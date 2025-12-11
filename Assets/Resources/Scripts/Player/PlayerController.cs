using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool useUnity6LinearVelocity = true;

    [Header("Runner Stats")]
    [Tooltip("Tốc độ chạy cơ bản")]
    [SerializeField] private float baseRunSpeed = 5f;
    [Tooltip("Lực nhảy ban đầu")]
    [SerializeField] private float jumpForce = 10f;

    [Header("Variable Jump Settings")]
    [Tooltip("Lực cộng thêm khi giữ nút nhảy")]
    [SerializeField] private float jumpHoldForce = 5f;
    [Tooltip("Thời gian tối đa được phép giữ nút")]
    [SerializeField] private float maxJumpHoldTime = 0.3f;
    [Tooltip("Hệ số giảm lực khi thả nút sớm")]
    [SerializeField] private float jumpCutMultiplier = 0.5f;

    [Header("Jump Cooldown")]
    [SerializeField] private float jumpCooldown = 0.2f;
    private float lastJumpTime;

    [Header("Respawn & Acceleration")]
    [SerializeField] private float respawnDelay = 3f;

    [Tooltip("Tốc độ tăng tốc (Đơn vị/Giây). Ví dụ: 2 nghĩa là mỗi giây tăng 2m/s.")]
    [SerializeField] private float accelerationRate = 2.0f; // Chỉnh cái này để tăng nhanh hay chậm

    private float respawnTimer;
    private bool isRespawning = false;
    private float currentSpeed = 0f;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    // Internal Variables
    private Rigidbody2D _rb;
    private BoxCollider2D _collider;
    private Animator _animator;
    private CharacterProfile _profile;

    private bool isGrounded;
    private bool isJumping;
    private float jumpTimeCounter;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<BoxCollider2D>();
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        SetupCharacter();
        // Bắt đầu game tăng tốc từ 0 lên cho mượt
        currentSpeed = 0f;
    }

    private void SetupCharacter()
    {
        _profile = ReferenceManager.Instance.currentSelectedProfile;
        if (_profile == null) return;

        if (UIManager.Instance.MainInfo != null)
            UIManager.Instance.MainInfo.sprite = _profile.mainInfo;

        if (_profile.inGameAnimator != null)
            _animator.runtimeAnimatorController = _profile.inGameAnimator;

        UpdateCollider();
    }

    private void UpdateCollider()
    {
        if (_profile.skinVariants != null && _profile.skinVariants.Length > 0)
        {
            _collider.size = _profile.skinVariants[0].bounds.size;
            _collider.offset = _profile.skinVariants[0].bounds.center;
        }
    }

    private void Update()
    {
        CheckInput();
        HandleStuckAndRespawn();
    }

    private void FixedUpdate()
    {
        CheckGround();
        MoveAutoSmooth();
    }

    // --- LOGIC DI CHUYỂN TĂNG DẦN ĐỀU ---
    private void MoveAutoSmooth()
    {
        if (isRespawning)
        {
            SetVelocity(Vector2.zero);
            return;
        }

        // 1. Tính tốc độ mục tiêu
        float scoreBonus = (GameStatsController.Instance != null) ? GameStatsController.Instance.resultDistance / 150f : 0f;
        float targetSpeed = baseRunSpeed + scoreBonus;

        // 2. Đồng bộ tốc độ vật lý thực tế với biến currentSpeed
        // Nếu nhân vật đâm vào tường, vận tốc vật lý X sẽ về 0.
        // Ta cần gán currentSpeed về 0 theo nó để khi thoát ra nó tăng tốc lại từ đầu.
        float physicalVelocityX = GetVelocity().x;

        // Nếu tốc độ thực tế nhỏ hơn nhiều so với tốc độ tính toán (chứng tỏ đang bị kẹt)
        if (physicalVelocityX < currentSpeed - 1f)
        {
            float tempCurrentSpeed = currentSpeed;

            // Reset currentSpeed về tốc độ thực tế (để tăng tốc lại từ đáy)
            currentSpeed = Mathf.Max(tempCurrentSpeed - baseRunSpeed / 2, baseRunSpeed);
        }

        // 3. Tăng tốc TUYẾN TÍNH (MoveTowards) thay vì Lerp
        // Điều này giúp tốc độ tăng đều đặn, không bị vọt lên đột ngột
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelerationRate * Time.fixedDeltaTime);

        // 4. Áp dụng vận tốc
        Vector2 targetVelocity = new Vector2(currentSpeed, GetVelocity().y);
        SetVelocity(targetVelocity);
    }

    // --- CHECK KẸT & RESPAWN ---
    private void HandleStuckAndRespawn()
    {
        // Điều kiện chết: Vận tốc X gần như bằng 0 trong khi target speed cao
        // Hoặc rơi xuống vực (Y < -10)
        bool isStuck = GetVelocity().x <= 0.1f && currentSpeed > 1f;
        bool isFallen = transform.position.y < -10f;

        if (!isRespawning && (isStuck || isFallen))
        {
            respawnTimer += Time.deltaTime;

            // Nếu kẹt quá respawnDelay giây -> Hồi sinh
            if (respawnTimer >= respawnDelay || isFallen)
            {
                Respawn();
            }
        }
        else
        {
            // Nếu thoát kẹt thì reset timer
            respawnTimer = 0f;
        }
    }

    private void Respawn()
    {
        if (ReferenceManager.Instance.RespawnTrans != null)
        {
            isRespawning = true; // Bật cờ đang hồi sinh để chặn di chuyển

            // Dời vị trí
            transform.position = ReferenceManager.Instance.RespawnTrans.position;

            // Reset vận tốc vật lý
            SetVelocity(Vector2.zero);

            // Biến tạm
            float tempCurrentSpeed = currentSpeed;

            // Reset biến tốc độ về rất thấp
            currentSpeed = Mathf.Max(tempCurrentSpeed - baseRunSpeed / 2, baseRunSpeed);
            respawnTimer = 0f;

            // Cho phép di chuyển lại sau 1 khung hình (hoặc ngay lập tức)
            isRespawning = false;
        }
    }

    // --- XỬ LÝ NHẤN ---
    private void CheckInput()
    {
        if (IsJumpButtonPressed() && isGrounded && Time.time > lastJumpTime + jumpCooldown)
        {

            isJumping = true;
            jumpTimeCounter = maxJumpHoldTime;
            SetVelocity(new Vector2(GetVelocity().x, 0));
            _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            lastJumpTime = Time.time;
            _animator.SetTrigger("isJump");
            PlayJumpSound();
        }

        if (IsJumpButtonHeld() && isJumping)
        {
            if (jumpTimeCounter > 0)
            {
                _rb.AddForce(Vector2.up * jumpHoldForce, ForceMode2D.Force);
                jumpTimeCounter -= Time.deltaTime;
            }
            else isJumping = false;
        }

        if (IsJumpButtonUp())
        {
            isJumping = false;
            if (GetVelocity().y > 0)
                SetVelocity(new Vector2(GetVelocity().x, GetVelocity().y * jumpCutMultiplier));
        }
    }

    private bool IsJumpButtonPressed() => Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
    private bool IsJumpButtonHeld() => Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space) || (Input.touchCount > 0 && (Input.GetTouch(0).phase == TouchPhase.Stationary || Input.GetTouch(0).phase == TouchPhase.Moved));
    private bool IsJumpButtonUp() => Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Space) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended);

    private void CheckGround()
    {
        if (groundCheck != null)
        {
            bool wasGrounded = isGrounded;
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
            if (isGrounded && !wasGrounded)
            {
                isJumping = false;
            }
        }
    }

    private void PlayJumpSound()
    {
        AudioManager.Instance.PlaySFX($"Jump_{Random.Range(0,2)}");
    }
    private Vector2 GetVelocity()
    {
#if UNITY_6000_0_OR_NEWER
        return useUnity6LinearVelocity ? _rb.linearVelocity : _rb.linearVelocity;
#else
        return _rb.velocity;
#endif
    }

    private void SetVelocity(Vector2 v)
    {
#if UNITY_6000_0_OR_NEWER
        if (useUnity6LinearVelocity) _rb.linearVelocity = v; else _rb.linearVelocity = v;
#else
        _rb.velocity = v;
#endif
    }
}
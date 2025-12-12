using UnityEngine;

public class PlayerController : BaseRunner
{
    [Header("Smooth Acceleration")]
    [SerializeField] private float accelerationRate = 2.0f;
    private float targetRunSpeed;

    [Header("Variable Jump Input")]
    public float jumpHoldForce = 5f;
    public float maxJumpHoldTime = 0.3f;
    public float jumpCutMultiplier = 0.5f;

    private bool isJumping;
    private float jumpTimeCounter;

    // --- 1. THÊM HÀM START ĐỂ KHỞI TẠO CHARACTER ---
    protected override void Start()
    {
        // Gọi Setup trước để load Skin và Animator
        SetupCharacter();

        // Sau đó mới gọi base.Start() để BaseRunner tự cập nhật Collider theo Skin vừa load
        base.Start();
    }

    // --- 2. HÀM SETUP CHARACTER (ĐÃ KHÔI PHỤC) ---
    private void SetupCharacter()
    {
        // Kiểm tra xem có ReferenceManager và Profile đã chọn chưa
        if (ReferenceManager.Instance == null || ReferenceManager.Instance.CurrentSelectedProfile == null)
            return;

        var profile = ReferenceManager.Instance.CurrentSelectedProfile;

/*        // Cập nhật chỉ số từ Profile (Ghi đè lên baseRunSpeed của BaseRunner)
        baseRunSpeed = profile.moveSpeed;
        jumpForce = profile.jumpForce;
*/
        // Cập nhật Animator cho nhân vật (Skin)
        if (_animator != null && profile.inGameAnimator != null)
        {
            _animator.runtimeAnimatorController = profile.inGameAnimator;
        }

        // Cập nhật UI hiển thị (Icon nhân vật trên góc màn hình nếu có)
        if (UIManager.Instance != null && UIManager.Instance.MainInfo != null)
        {
            UIManager.Instance.MainInfo.sprite = profile.mainInfo;
        }
    }

    // --- CÁC LOGIC DI CHUYỂN CŨ (GIỮ NGUYÊN) ---
    protected override void Move()
    {
        // Tính tốc độ mục tiêu dựa trên khoảng cách
        float scoreBonus = (GameStatsController.Instance != null) ? GameStatsController.Instance.resultDistance / 150f : 0f;
        targetRunSpeed = baseRunSpeed + scoreBonus;

        // Tăng tốc mượt mà
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetRunSpeed, accelerationRate * Time.fixedDeltaTime);

        base.Move();
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // Input Nhấn xuống
        bool isPressDown = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) isPressDown = true;

        if (isPressDown && isGrounded)
        {
            Jump();
            isJumping = true;
            jumpTimeCounter = maxJumpHoldTime;
        }

        // Input Giữ
        bool isHolding = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space);
        if (Input.touchCount > 0 && (Input.GetTouch(0).phase == TouchPhase.Stationary || Input.GetTouch(0).phase == TouchPhase.Moved)) isHolding = true;

        if (isHolding && isJumping)
        {
            if (jumpTimeCounter > 0)
            {
                _rb.AddForce(Vector2.up * jumpHoldForce, ForceMode2D.Force);
                jumpTimeCounter -= Time.deltaTime;
            }
            else isJumping = false;
        }

        // Input Thả ra
        bool isPressUp = Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Space);
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended) isPressUp = true;

        if (isPressUp)
        {
            isJumping = false;
#if UNITY_6000_0_OR_NEWER
            if (_rb.linearVelocity.y > 0)
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.y * jumpCutMultiplier);
#else
            if (_rb.velocity.y > 0)
                _rb.velocity = new Vector2(_rb.velocity.x, _rb.velocity.y * jumpCutMultiplier);
#endif
        }
    }

    protected override void OnStuck()
    {
        base.OnStuck();
        if (ReferenceManager.Instance != null && ReferenceManager.Instance.RespawnTrans != null)
        {
            transform.position = ReferenceManager.Instance.RespawnTrans.position;
            currentSpeed = baseRunSpeed * 0.8f;
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = Vector2.zero;
#else
            _rb.velocity = Vector2.zero;
#endif
        }
    }
}
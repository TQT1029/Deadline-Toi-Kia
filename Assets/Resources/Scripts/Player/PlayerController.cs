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

    protected override void Move()
    {
        // Tính tốc độ mục tiêu dựa trên điểm số (càng chạy xa càng nhanh)
        float scoreBonus = (GameStatsController.Instance != null) ? GameStatsController.Instance.resultDistance / 150f : 0f;
        targetRunSpeed = baseRunSpeed + scoreBonus;

        // Nếu vừa respawn (tốc độ đang thấp), tăng dần lên
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetRunSpeed, accelerationRate * Time.fixedDeltaTime);

        base.Move();
    }

    private void Update()
    {
        HandleInput();
    }

    // --- 1. XỬ LÝ INPUT (TOUCH + MOUSE) ---
    private void HandleInput()
    {
        // Kiểm tra Input Nhấn xuống (Bắt đầu nhảy)
        bool isPressDown = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);

        // Kiểm tra Touch phase Began
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            isPressDown = true;

        if (isPressDown && isGrounded)
        {
            Jump(); // Nhảy cơ bản
            isJumping = true;
            jumpTimeCounter = maxJumpHoldTime;
        }

        // Kiểm tra Input Giữ (Bay cao hơn)
        bool isHolding = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space);

        // Kiểm tra Touch phase Stationary/Moved
        if (Input.touchCount > 0 && (Input.GetTouch(0).phase == TouchPhase.Stationary || Input.GetTouch(0).phase == TouchPhase.Moved))
            isHolding = true;

        if (isHolding && isJumping)
        {
            if (jumpTimeCounter > 0)
            {
                _rb.AddForce(Vector2.up * jumpHoldForce, ForceMode2D.Force);
                jumpTimeCounter -= Time.deltaTime;
            }
            else isJumping = false;
        }

        // Kiểm tra Input Thả ra (Cắt lực nhảy)
        bool isPressUp = Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Space);

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
            isPressUp = true;

        if (isPressUp)
        {
            isJumping = false;
            // Giảm vận tốc Y ngay lập tức
#if UNITY_6000_0_OR_NEWER
            if (_rb.linearVelocity.y > 0)
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.y * jumpCutMultiplier);
#else
            if (_rb.velocity.y > 0)
                _rb.velocity = new Vector2(_rb.velocity.x, _rb.velocity.y * jumpCutMultiplier);
#endif
        }
    }

    // --- 2. LOGIC KHI BỊ KẸT (RESPAWN) ---
    protected override void OnStuck()
    {
        base.OnStuck();

        // Nếu Player bị kẹt, thực hiện Respawn về điểm hồi sinh
        if (ReferenceManager.Instance != null && ReferenceManager.Instance.RespawnTrans != null)
        {
            transform.position = ReferenceManager.Instance.RespawnTrans.position;

            // Reset tốc độ về thấp để tăng tốc lại từ từ (tránh ngộp)
            currentSpeed = baseRunSpeed * 0.8f;

            // Reset vật lý
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = Vector2.zero;
#else
            _rb.velocity = Vector2.zero;
#endif
        }
    }
}
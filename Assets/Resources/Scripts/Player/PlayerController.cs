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

    protected override void Start()
    {
        SetupCharacter();
        base.Start();
    }

    private void SetupCharacter()
    {
        if (ReferenceManager.Instance == null || ReferenceManager.Instance.CurrentSelectedProfile == null)
            return;

        var profile = ReferenceManager.Instance.CurrentSelectedProfile;
        if (_animator != null && profile.inGameAnimator != null)
            _animator.runtimeAnimatorController = profile.inGameAnimator;
        if (UIManager.Instance != null && UIManager.Instance.MainInfo != null)
            UIManager.Instance.MainInfo.sprite = profile.mainInfo;
    }

    // --- LOGIC RIÊNG CỦA PLAYER ---

    // 1. Ghi đè Move để thêm tính năng tăng tốc mượt
    protected override void Move()
    {
        float scoreBonus = (GameStatsController.Instance != null) ? GameStatsController.Instance.resultDistance / 150f : 0f;
        targetRunSpeed = baseRunSpeed + scoreBonus;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetRunSpeed, accelerationRate * Time.fixedDeltaTime);

        base.Move();
    }

    private void Update()
    {
        HandleInput();
    }

    // 2. Ghi đè OnRespawn để đặt vị trí về Checkpoint
    protected override void OnRespawn()
    {
        // Gọi base để reset vận tốc và trừ tốc độ chạy
        base.OnRespawn();

        if (ReferenceManager.Instance != null && ReferenceManager.Instance.RespawnTrans != null)
        {
            transform.position = ReferenceManager.Instance.RespawnTrans.position;

            // Reset trạng thái Input nhảy
            isJumping = false;
            jumpTimeCounter = 0;

            Debug.Log("Player đã hồi sinh về điểm xuất phát!");
        }
    }

    // Lưu ý: Player không cần override OnStuck nữa vì BaseRunner đã gọi OnRespawn trong đó rồi.

    private void HandleInput()
    {
        // Input Nhảy (Click/Touch)
        bool isPressDown = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);

        if (isPressDown && isGrounded)
        {
            Jump();
            isJumping = true;
            jumpTimeCounter = maxJumpHoldTime;
        }

        bool isHolding = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space);
        if (isHolding && isJumping)
        {
            if (jumpTimeCounter > 0)
            {
                _rb.AddForce(Vector2.up * jumpHoldForce, ForceMode2D.Force);
                jumpTimeCounter -= Time.deltaTime;
            }
            else isJumping = false;
        }

        bool isPressUp = Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Space);
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
}
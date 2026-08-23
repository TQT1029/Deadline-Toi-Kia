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

    private void OnEnable()
    {
        InputManager.OnJumpDown += HandleJumpDown;
        InputManager.OnJumpUp += HandleJumpUp;
    }

    private void OnDisable()
    {
        InputManager.OnJumpDown -= HandleJumpDown;
        InputManager.OnJumpUp -= HandleJumpUp;
    }

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

        GameEvents.TriggerCharacterSelected(profile);

        // Fallback UI nếu có UIManager
        if (UIManager.Instance != null && UIManager.Instance.MainInfo != null)
            UIManager.Instance.MainInfo.sprite = profile.mainInfo;
    }

    protected override void Move()
    {
        if (isControlLocked) return;

        float distanceBonus = (GameStatsController.Instance != null) ? GameStatsController.Instance.resultDistance / 150f : 0f;
        targetRunSpeed = baseRunSpeed + distanceBonus;

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetRunSpeed, accelerationRate * Time.fixedDeltaTime);

        base.Move();
    }

    protected override void Update()
    {
        base.Update();
        if (isControlLocked) return;

        // Xử lý giữ phím nhảy (Variable Jump Height)
        if (isJumping && InputManager.IsJumpHolding)
        {
            if (jumpTimeCounter > 0f)
            {
                _rb.AddForce(Vector2.up * jumpHoldForce, ForceMode2D.Force);
                jumpTimeCounter -= Time.deltaTime;
            }
            else
            {
                isJumping = false;
            }
        }
    }

    private void HandleJumpDown()
    {
        if (isControlLocked) return;

        if (isGrounded)
        {
            Jump();
            isJumping = true;
            jumpTimeCounter = maxJumpHoldTime;
        }
    }

    private void HandleJumpUp()
    {
        if (!isJumping) return;

        isJumping = false;
#if UNITY_6000_0_OR_NEWER
        if (_rb != null && _rb.linearVelocity.y > 0)
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.y * jumpCutMultiplier);
#else
        if (_rb != null && _rb.velocity.y > 0)
            _rb.velocity = new Vector2(_rb.velocity.x, _rb.velocity.y * jumpCutMultiplier);
#endif
    }

    protected override void OnRespawn()
    {
        isJumping = false;
        jumpTimeCounter = 0f;
        base.OnRespawn();
    }
}
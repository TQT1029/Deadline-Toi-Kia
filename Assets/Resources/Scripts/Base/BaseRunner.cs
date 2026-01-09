using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class BaseRunner : MonoBehaviour
{
    [Header("Base Stats")]
    public float baseRunSpeed = 5f;
    public float jumpForce = 10f;

    [Header("Auto Collider & Stuck Config")]
    [Tooltip("Thời gian đứng yên tối đa trước khi bị coi là Kẹt")]
    public float timeToStuck = 1.0f;
    protected float stuckTimer;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.3f;
    public LayerMask groundLayer;

    [Header("Knockback Settings")]
    [SerializeField] protected float knockbackCooldown = 10f;
    private float lastKnockbackTime;

    [Header("Map Safety (Chống rơi khỏi map)")]
    [Tooltip("Độ sâu mà Runner rơi xuống sẽ bị Respawn (VD: -10)")]
    public float fallThresholdY = -10f;

    // Components
    protected Rigidbody2D _rb;
    protected Animator _animator;
    protected BoxCollider2D _collider;
    protected SpriteRenderer _spriteRenderer;

    protected bool isGrounded = false;
    protected bool isControlLocked = false;
    protected float currentSpeed;

    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<BoxCollider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();

        lastKnockbackTime = Time.time;
        currentSpeed = baseRunSpeed;
    }

    protected virtual void Start()
    {
        UpdateColliderSize();
    }

    protected virtual void FixedUpdate()
    {
        CheckGround();
        CheckStuck();
        CheckPitFall(); // Tự động kiểm tra rơi hố
        Move();
    }

    // --- LOGIC DI CHUYỂN ---
    protected virtual void Move()
    {
#if UNITY_6000_0_OR_NEWER
        _rb.linearVelocity = new Vector2(currentSpeed, _rb.linearVelocity.y);
#else
        _rb.velocity = new Vector2(currentSpeed, _rb.velocity.y);
#endif
    }

    // --- LOGIC NHẢY CƠ BẢN ---
    public virtual void Jump()
    {
        if (isGrounded)
        {
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0);
#else
            _rb.velocity = new Vector2(_rb.velocity.x, 0);
#endif
            _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            if (_animator) _animator.SetTrigger("isJump");

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX($"Jump_{Random.Range(0, 2)}");
        }
    }

    /// <summary>
    /// Hàm kiểm tra áp dụng lực đẩy và choáng (knockback + stun).
    /// </summary>
    /// <param name="forceDir">Hướng bị đẩy</param>
    /// <param name="forceStrength">Lực đẩy</param>
    /// <param name="stunDuration">Thời gian stun</param>
    public void ApplyKnockback(Vector2 forceDir, float forceStrength, float stunDuration)
    {
        // Đang bị stun, bỏ qua
        if (isControlLocked) return;

        if (Time.time - lastKnockbackTime > knockbackCooldown)
        {
            StartCoroutine(KnockbackRoutine(forceDir, forceStrength, stunDuration));
            lastKnockbackTime = Time.time;
        }


    }

    private IEnumerator KnockbackRoutine(Vector2 dir, float force, float duration)
    {
        isControlLocked = true; // 1. Ngắt quyền điều khiển di chuyển

        // 2. Reset vận tốc hiện tại về 0 để lực đẩy có tác dụng rõ rệt
#if UNITY_6000_0_OR_NEWER
        _rb.linearVelocity = Vector2.zero;
#else
        _rb.velocity = Vector2.zero;
#endif
        // 3. Thêm lực đẩy
        _rb.AddForce(dir * force, ForceMode2D.Impulse);

        //---Tạm ẩn---//
        //if (_animator) _animator.SetTrigger("isHit"); // Nếu có anim bị thương

        // 4. Chờ hết thời gian choáng
        yield return new WaitForSeconds(duration);

        // 5. Khôi phục
        isControlLocked = false;

        // Reset lại vận tốc để chạy tiếp ngay lập tức
        currentSpeed = baseRunSpeed * 0.8f; // Chạy lại từ từ thôi (optional)
    }

    // --- LOGIC KIỂM TRA & XỬ LÝ SỰ CỐ (KẸT / RƠI) ---
    protected void CheckGround()
    {
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapBox(groundCheck.position, new Vector2(3.7f / 2.5f, groundCheckRadius), 0, groundLayer);
    }

    protected virtual void CheckStuck()
    {
        float vX = 0f;
#if UNITY_6000_0_OR_NEWER
        vX = _rb.linearVelocity.x;
#else
        vX = _rb.velocity.x;
#endif
        // Nếu vận tốc gần 0 mà vẫn muốn chạy
        if (Mathf.Abs(vX) < 0.1f && currentSpeed > 1f)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= timeToStuck)
            {
                OnStuck();
                stuckTimer = 0f;
            }
        }
        else stuckTimer = 0f;
    }

    protected virtual void CheckPitFall()
    {
        if (transform.position.y < fallThresholdY)
        {
            OnRespawn();
        }
    }

    // --- CÁC HÀM XỬ LÝ HÀNH VI (VIRTUAL ĐỂ CON GHI ĐÈ) ---

    // 1. Khi bị kẹt tường -> Mặc định gọi Respawn luôn (đơn giản hóa)
    protected virtual void OnStuck()
    {
        Debug.Log($"{gameObject.name} bị kẹt -> Gọi Respawn.");
        OnRespawn();
    }

    // 2. Khi cần hồi sinh (Do rơi hố hoặc kẹt)
    protected virtual void OnRespawn()
    {
        // LOGIC CHUNG CHO CẢ PLAYER VÀ BOT:

        // a. Reset vận tốc về 0 để không bị trôi/rơi tiếp
#if UNITY_6000_0_OR_NEWER
        _rb.linearVelocity = Vector2.one;
#else
        _rb.velocity = Vector2.zero;
#endif
        // b. Giảm tốc độ chạy (Hình phạt)
        currentSpeed = baseRunSpeed * 0.75f;

        // c. Reset bộ đếm kẹt
        stuckTimer = 0f;

        // Lưu ý: Việc đặt lại transform.position sẽ do lớp con tự quyết định
    }

    // Helper: Update Collider
    protected void UpdateColliderSize()
    {
        if (_spriteRenderer.sprite != null)
        {
            _collider.size = _spriteRenderer.size;
            _collider.offset = Vector2.zero;
        }
    }
}
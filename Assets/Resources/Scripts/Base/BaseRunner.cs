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
    [SerializeField] protected float knockbackCooldown = 1.0f; // Giảm cooldown xuống hợp lý hơn (ví dụ 1s)
    private float lastKnockbackTime;

    [Header("Map Safety (Chống rơi khỏi map)")]
    [Tooltip("Độ sâu mà Runner rơi xuống sẽ bị Respawn (VD: -10)")]
    public float fallThresholdY = -10f;

    // Components
    protected Rigidbody2D _rb;
    protected Animator _animator;
    protected BoxCollider2D _collider;
    protected SpriteRenderer _spriteRenderer;

    private RandomUtils.FloatShuffleBag floatShuffleBag = new RandomUtils.FloatShuffleBag(-3f, 3f, 0.5f);

    protected bool isGrounded = false;
    protected bool isControlLocked = false;
    protected float currentSpeed;

    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<BoxCollider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();

        lastKnockbackTime = -knockbackCooldown; // Fix để có thể bị knockback ngay khi start game nếu cần

        currentSpeed = baseRunSpeed + floatShuffleBag.Next();
    }

    protected virtual void Start()
    {
        UpdateColliderSize();
    }

    protected virtual void FixedUpdate()
    {
        CheckGround();
        CheckStuck();
        CheckPitFall();
        Move();
    }

    // --- LOGIC DI CHUYỂN ---
    protected virtual void Move()
    {
        // QUAN TRỌNG: Chỉ set vận tốc X nếu không bị khóa điều khiển
        // Nếu bị khóa, ta để physics tự làm việc (bị đẩy lùi, rơi tự do...)
        if (isControlLocked) return;

#if UNITY_6000_0_OR_NEWER
        _rb.linearVelocity = new Vector2(currentSpeed, _rb.linearVelocity.y);
#else
        _rb.velocity = new Vector2(currentSpeed, _rb.velocity.y);
#endif
    }

    // --- LOGIC NHẢY ---
    public virtual void Jump()
    {
        // Thêm điều kiện !isControlLocked để không nhảy được khi đang bị choáng
        if (isGrounded && !isControlLocked)
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

    public void ApplyKnockback(Vector2 forceDir, float forceStrength, float stunDuration)
    {
        if (isControlLocked) return;

        if (Time.time - lastKnockbackTime > knockbackCooldown)
        {
            StartCoroutine(KnockbackRoutine(forceDir, forceStrength, stunDuration));
            lastKnockbackTime = Time.time;
        }
    }

    // --- PHẦN BẠN YÊU CẦU CHỈNH SỬA ---
    private IEnumerator KnockbackRoutine(Vector2 dir, float force, float duration)
    {
        isControlLocked = true; // 1. Ngắt quyền điều khiển

        // 2. Reset vận tốc & Thêm lực đẩy
#if UNITY_6000_0_OR_NEWER
        _rb.linearVelocity = Vector2.zero;
#else
        _rb.velocity = Vector2.zero;
#endif
        _rb.AddForce(dir * force, ForceMode2D.Impulse);

        // if (_animator) _animator.SetTrigger("isHit"); 

        // 3. Chờ hết thời gian choáng (Animation choáng)
        yield return new WaitForSeconds(duration);

        // 4. MỚI: Chờ cho đến khi chạm đất
        // WaitUntil sẽ check mỗi frame, nếu isGrounded == true thì mới chạy tiếp
        yield return new WaitUntil(() => isGrounded);

        // 5. Khôi phục di chuyển
        isControlLocked = false;
        currentSpeed = baseRunSpeed * 0.8f;
    }

    // --- LOGIC KIỂM TRA ---
    protected void CheckGround()
    {
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapBox(groundCheck.position, new Vector2(3.7f / 2.5f, groundCheckRadius), 0, groundLayer);
    }

    protected virtual void CheckStuck()
    {
        // Không check kẹt khi đang bị knockback (vì lúc đó vận tốc có thể = 0 do va chạm)
        if (isControlLocked)
        {
            stuckTimer = 0f;
            return;
        }

        float vX = 0f;
#if UNITY_6000_0_OR_NEWER
        vX = _rb.linearVelocity.x;
#else
        vX = _rb.velocity.x;
#endif
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

    protected virtual void OnStuck()
    {
        OnRespawn();
    }

    protected virtual void OnRespawn()
    {
        // để tránh việc nhân vật hồi sinh xong vẫn đứng đơ ra chờ chạm đất
        StopAllCoroutines();

        // Reset trạng thái
        isControlLocked = false;

#if UNITY_6000_0_OR_NEWER
        _rb.linearVelocity = Vector2.zero; // Sửa lại thành zero cho an toàn
#else
        _rb.velocity = Vector2.zero;
#endif
        currentSpeed = baseRunSpeed * 0.75f;
        stuckTimer = 0f;
    }

    protected void UpdateColliderSize()
    {
        if (_spriteRenderer.sprite != null)
        {
            _collider.size = _spriteRenderer.size;
            _collider.offset = Vector2.zero;
        }
    }
}
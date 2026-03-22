using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(SpriteRenderer))]
public class BaseRunner : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] protected float baseRunSpeed = 5f;
    [SerializeField] protected float jumpForce = 10f;

    [Header("Auto Collider & Stuck Config")]
    [Tooltip("Thời gian đứng yên tối đa trước khi bị coi là Kẹt")]
    [SerializeField] protected float timeToStuck = 1.0f;
    protected float stuckTimer;

    [Header("Ground Detection")]
    [SerializeField] protected Transform groundCheckPos;
    protected float groundCheckRadius = 0.3f;
    [SerializeField] protected LayerMask groundLayer;

    [Header("Knockback Settings")]
    [SerializeField] protected float knockbackCooldown = 1.0f;
    private float lastKnockbackTime;

    [Header("Map Safety")]
    [Tooltip("Độ sâu mà Runner rơi xuống sẽ bị Respawn (VD: -10)")]
    [SerializeField] protected float fallThresholdY = -10f;
    [SerializeField] protected float respawnDelay = 1f; // Thời gian delay trước khi respawn sau khi rơi 
    [SerializeField] protected Vector2 respawnPosition = Vector2.zero;

    [Tooltip("Số lần rớt tối đa trước khi bị ép dịch chuyển tới trước")]
    [SerializeField] protected int maxConsecutiveFalls = 3;
    protected int fallCount = 0;


    // Components
    [SerializeField] private Transform startPoint; // Điểm bắt đầu của Runner
    protected Rigidbody2D _rb;
    protected Animator _animator;
    protected CapsuleCollider2D _collider;
    protected SpriteRenderer _spriteRenderer;

    private RandomUtils.FloatShuffleBag floatShuffleBag = new RandomUtils.FloatShuffleBag(-3f, 3f, 0.5f);

    protected bool isGrounded = false;
    protected bool isControlLocked = false;
    protected bool isRespawning = false;
    protected float currentSpeed;

    protected virtual void Awake()
    {
        if (startPoint == null) startPoint = GameObject.FindGameObjectWithTag("SpawnPoint")?.transform;
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<CapsuleCollider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();

        lastKnockbackTime = -knockbackCooldown;

        currentSpeed = baseRunSpeed + floatShuffleBag.Next();

    }

    protected virtual void Start()
    {
        //UpdateColliderSize();
        transform.position = startPoint.position;


    }
    protected virtual void Update()
    {
        if (isGrounded && !isControlLocked && !isRespawning && transform.position.y >= MapGlobalConfig.Instance.groundY)
        {
            respawnPosition = new Vector2(transform.position.x - 3f, transform.position.y + 3f);
            fallCount = 0;
        }
    }
    protected virtual void FixedUpdate()
    {
        CheckGround();
        CheckStuck();
        if (!isRespawning && transform.position.y < fallThresholdY) StartCoroutine(PitFallRoutine());
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
        if (groundCheckPos != null)
        {
            isGrounded = Physics2D.OverlapBox(groundCheckPos.position, new Vector2(3.7f / 2.5f, groundCheckRadius), 0, groundLayer);
        }
    }


    protected virtual void CheckStuck()
    {
        // Không check kẹt khi đang bị knockback (vì lúc đó vận tốc có thể = 0 do va chạm)
        if (isControlLocked)
        {
            stuckTimer = 0f;
            return;
        }

        float vX = _rb.linearVelocity.x;

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

    protected virtual IEnumerator PitFallRoutine()
    {
        isControlLocked = true; // Khóa điều khiển ngay khi rớt
        isRespawning = true;

        fallCount++;
        // Ngừng gia tốc rơi trước khi dịch chuyển
        _rb.linearVelocity = Vector2.zero;

        // Dịch chuyển nhân vật ngay lập tức về vị trí an toàn
        RespawnFromPit();

        // Chờ một khoảng thời gian trước khi đưa nhân vật lên
        yield return new WaitForSeconds(respawnDelay);

        OnRespawn();
    }

    protected virtual void OnStuck()
    {
        RespawnFromStuck();
        OnRespawn();
    }

    protected virtual void OnRespawn()
    {
        // để tránh việc nhân vật hồi sinh xong vẫn đứng đơ ra chờ chạm đất
        StopAllCoroutines();

        // Reset trạng thái 
        isControlLocked = false;
        isRespawning = false;

        _rb.linearVelocity = Vector2.zero; // Sửa lại thành zero cho an toàn

        currentSpeed = baseRunSpeed * 0.75f;
        stuckTimer = 0f;
    }

    protected virtual void RespawnFromPit()
    {
        // Kiểm tra nếu rớt liên tục quá số lần cho phép
        if (fallCount >= maxConsecutiveFalls)
        {
            // Ép vị trí respawn tiến về phía trước 10 đơn vị, cao lên 5 đơn vị
            // Cập nhật thẳng vào respawnPosition để nếu rớt tiếp, nó sẽ lại tính mốc từ vị trí mới này mà cộng thêm 10
            respawnPosition = new Vector2(respawnPosition.x + 10f, respawnPosition.y + 5f);
            transform.position = respawnPosition;
        }
        else
        {
            // Dịch chuyển về vị trí lưu mặc định
            transform.position = respawnPosition;
        }
    }

    protected virtual void RespawnFromStuck()
    {
        transform.position = new Vector2(transform.position.x - 3f, transform.position.y + 5f);
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
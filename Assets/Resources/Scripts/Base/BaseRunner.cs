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
    protected bool isStuck;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    // Components
    protected Rigidbody2D _rb;
    protected Animator _animator;
    protected BoxCollider2D _collider;
    protected SpriteRenderer _spriteRenderer;

    protected bool isGrounded;
    protected float currentSpeed;

    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<BoxCollider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>(); // Animator có thể null

        currentSpeed = baseRunSpeed;
    }

    protected virtual void Start()
    {
        // Tự động cập nhật Collider theo Sprite đầu tiên
        UpdateColliderSize();
    }

    protected virtual void FixedUpdate()
    {
        CheckGround();
        CheckStuck(); // Kiểm tra xem có bị kẹt không
        Move();
    }

    // --- 1. LOGIC TỰ CẬP NHẬT COLLIDER ---
    protected void UpdateColliderSize()
    {
        // Chờ 1 chút để Animator cập nhật frame đầu tiên (nếu có)
        // Hoặc lấy trực tiếp từ SpriteRenderer
        if (_spriteRenderer.sprite != null)
        {
            _collider.size = _spriteRenderer.sprite.bounds.size;
            _collider.offset = _spriteRenderer.sprite.bounds.center - transform.position;
            // Lưu ý: offset tính tương đối nên trừ đi transform.position nếu cần, 
            // nhưng thường sprite.bounds.center là local nếu pivot đúng.
            // Cách an toàn nhất cho 2D Sprite là:
            _collider.size = _spriteRenderer.size;
            _collider.offset = Vector2.zero; // Nếu Pivot sprite ở giữa
                                             // Nếu Pivot ở chân, bạn có thể cần chỉnh offset Y = size.y / 2
        }
    }

    // --- 2. LOGIC DI CHUYỂN & CHỐNG KẸT ---
    protected virtual void Move()
    {
        // Giữ vận tốc Y, ghi đè vận tốc X
#if UNITY_6000_0_OR_NEWER
        _rb.linearVelocity = new Vector2(currentSpeed, _rb.linearVelocity.y);
#else
        _rb.velocity = new Vector2(currentSpeed, _rb.velocity.y);
#endif
    }

    protected virtual void CheckStuck()
    {
        // Nếu vận tốc X gần bằng 0 (đang bị chặn) nhưng logic game vẫn muốn chạy (currentSpeed > 0)
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
                OnStuck(); // Gọi hàm xử lý kẹt
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    // Hàm ảo để lớp con tự định nghĩa cách giải thoát (Player thì Respawn, Bot thì Nhảy)
    protected virtual void OnStuck()
    {
        Debug.Log($"{gameObject.name} is Stuck!");
    }

    // --- CÁC HÀM CƠ BẢN ---
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

            AudioManager.Instance.PlaySFX($"Jump_{Random.Range(0, 2)}");
        }
    }

    protected void CheckGround()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
            //if (_animator) _animator.SetBool("isGrounded", isGrounded);
        }
    }
}
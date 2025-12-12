using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class BotController : MonoBehaviour
{
    [Header("Bot Stats")]
    public float baseSpeed = 5f;
    public float jumpForce = 10f;

    [Header("AI Senses (Mắt thần)")]
    [Tooltip("Vị trí mắt bắn tia dò đường (Nên đặt ở chân hoặc bụng)")]
    public Transform sensorPoint;
    [Tooltip("Khoảng cách nhìn thấy vật cản để nhảy")]
    public float viewDistance = 3.0f;
    [Tooltip("Layer của chướng ngại vật (Bắt buộc chọn đúng)")]
    public LayerMask obstacleLayer;
    [Tooltip("Layer mặt đất")]
    public LayerMask groundLayer;

    [Header("Rubber Banding (Cân bằng game)")]
    [Tooltip("Kéo Player vào đây để Bot biết đường đuổi theo")]
    public Transform targetPlayer;
    public float catchUpMultiplier = 1.3f; // Tăng tốc khi bị bỏ lại
    public float slowDownMultiplier = 0.8f; // Giảm tốc khi chạy quá xa

    private Rigidbody2D _rb;
    private Animator _animator;
    private bool isGrounded;
    private bool isJumpCooldown = false;
    private float currentSpeed;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        currentSpeed = baseSpeed;
    }

    private void FixedUpdate()
    {
        CheckGround();
        HandleAIBehavior();
        Move();
    }

    private void HandleAIBehavior()
    {
        // 1. Logic Raycast để nhảy
        if (isGrounded && !isJumpCooldown)
        {
            // Bắn tia về phía trước
            RaycastHit2D hit = Physics2D.Raycast(sensorPoint.position, Vector2.right, viewDistance, obstacleLayer);
            Debug.DrawRay(sensorPoint.position, Vector2.right * viewDistance, Color.red);

            if (hit.collider != null)
            {
                Jump();
            }
        }

        // 2. Logic điều chỉnh tốc độ (Rubber Banding)
        if (targetPlayer != null)
        {
            float distance = transform.position.x - targetPlayer.position.x;

            if (distance < -8f) // Bot bị tụt lại xa quá (> 8m)
            {
                currentSpeed = Mathf.Lerp(currentSpeed, baseSpeed * catchUpMultiplier, Time.fixedDeltaTime);
            }
            else if (distance > 8f) // Bot chạy nhanh quá (> 8m)
            {
                currentSpeed = Mathf.Lerp(currentSpeed, baseSpeed * slowDownMultiplier, Time.fixedDeltaTime);
            }
            else
            {
                currentSpeed = Mathf.Lerp(currentSpeed, baseSpeed, Time.fixedDeltaTime);
            }
        }
    }

    private void Move()
    {
        _rb.linearVelocity = new Vector2(currentSpeed, _rb.linearVelocity.y);
    }

    private void Jump()
    {
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0); // Reset Y
        _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        if (_animator) _animator.SetTrigger("isJump"); // Sử dụng trigger giống PlayerController

        // Cooldown để không nhảy liên tục (tránh lỗi double jump)
        isJumpCooldown = true;
        Invoke(nameof(ResetJump), 0.5f);
    }

    private void ResetJump() => isJumpCooldown = false;

    private void CheckGround()
    {
        // Bắn tia xuống dưới chân
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1.1f, groundLayer);
        isGrounded = hit.collider != null;
    }
}
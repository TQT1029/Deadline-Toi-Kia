using UnityEngine;
using DG.Tweening; // Bắt buộc

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool useUnity6LinearVelocity = true;

    [Header("Runner Stats")]
    [Tooltip("Tốc độ chạy liên tục")]
    public float runSpeed = 5f;
    [Tooltip("Lực nhảy")]
    public float jumpForce = 10f;

    [SerializeField] private float jumpCooldown = 0.5f;
    private float lastJumpTime;

    [Header("Ground Detection")]
    public Transform groundCheck; // Kéo một GameObject con nằm dưới chân Player vào đây
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer; // Chọn Layer là "Ground" hoặc "Platform"

    private Rigidbody2D _rb;
    private Animator _animator;
    private CharacterProfile _profile;
    private bool isGrounded;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        SetupCharacter();
    }

    private void SetupCharacter()
    {
        // 1. Lấy dữ liệu (Giữ nguyên logic cũ của bạn)
        _profile = ReferenceManager.Instance.currentSelectedProfile;
        var mapData = ReferenceManager.Instance.currentSelectedMap;

        if (_profile == null) return;

        runSpeed = _profile.moveSpeed;

        // 2. Setup Animation
        _animator.runtimeAnimatorController = _profile.inGameAnimator;
    }

    private void Update()
    {
        CheckInput();
    }

    private void FixedUpdate()
    {
        CheckGround();
        MoveAuto();
    }

    // --- LOGIC DI CHUYỂN TỰ ĐỘNG ---
    private void MoveAuto()
    {
        // Luôn luôn di chuyển sang phải với tốc độ runSpeed
        // Giữ nguyên vận tốc Y hiện tại (để trọng lực hoạt động)
        Vector2 targetVelocity = new Vector2(runSpeed, _rb.linearVelocity.y);

        if (useUnity6LinearVelocity)
        {
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = new Vector2(runSpeed, _rb.linearVelocity.y);
#else
            _rb.velocity = targetVelocity;
#endif
        }
        else
        {
            _rb.linearVelocity = targetVelocity;
        }
    }

    // --- KIỂM TRA ĐẤT ---
    private void CheckGround()
    {
        if (groundCheck != null)
        {
            // Tạo một vòng tròn nhỏ dưới chân để xem có chạm lớp Ground không
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
    }

    // --- XỬ LÝ NHẤN MÀN HÌNH ---
    private void CheckInput()
    {
        // Hỗ trợ cả Click chuột trái, Chạm màn hình, hoặc nút Space
        if (lastJumpTime >= jumpCooldown)
        {
            if ((Input.GetMouseButton(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) || Input.GetKeyDown(KeyCode.Space)) && isGrounded)
            {
                Jump();
                lastJumpTime = 0f;
            }
        }
        else
        {
            lastJumpTime += Time.deltaTime;

            // Reset animation nhảy khi đã hạ cánh
            _animator.SetBool("isJump", false);
        }
    }

    private void Jump()
    {
        // 1. Reset vận tốc Y về 0 trước khi nhảy để lực nhảy luôn đồng đều
        if (useUnity6LinearVelocity)
        {
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0);
            _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
#else
             _rb.velocity = new Vector2(_rb.velocity.x, 0);
             _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
#endif
        }
        else
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0);
            _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }

        // 2. Thực hiện Animation lộn vòng bằng DOTween
        _animator.SetBool("isJump", true);
    }


    // Vẽ vòng tròn check ground trong Editor để dễ chỉnh
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
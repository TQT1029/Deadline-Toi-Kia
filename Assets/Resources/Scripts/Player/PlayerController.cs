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

    [Header("Animation")]
    [Tooltip("Thời gian xoay 1 vòng")]
    public float rotateDuration = 0.5f;
    [Tooltip("Số vòng xoay khi nhảy (vd: -360 là xoay ra trước)")]
    public Vector3 rotateAngle = new Vector3(0, 0, -360);

    [Header("Ground Detection")]
    public Transform groundCheck; // Kéo một GameObject con nằm dưới chân Player vào đây
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer; // Chọn Layer là "Ground" hoặc "Platform"

    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer;
    private CharacterProfile _profile;
    private bool isGrounded;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
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

        // 2. Setup Skin
        int skinIndex = (mapData != null) ? mapData.mapIndex : 0;
        if (_profile.skinVariants != null && skinIndex < _profile.skinVariants.Length)
        {
            _spriteRenderer.sprite = _profile.skinVariants[skinIndex];
        }
        else if (_profile.skinVariants.Length > 0)
        {
            _spriteRenderer.sprite = _profile.skinVariants[0];
        }
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
        if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)) && isGrounded)
        {
            Jump();
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
        DoJumpAnimation();
    }

    private void DoJumpAnimation()
    {
        // Ngắt tween cũ nếu đang xoay dở để xoay cái mới
        transform.DOKill();

        // Reset góc xoay về mặc định (hoặc giữ nguyên nếu muốn xoay tiếp)
        // Ở đây ta reset Z về 0 để xoay cho chuẩn vòng
        transform.rotation = Quaternion.identity;

        // Xoay 360 độ (RotateMode.FastBeyond360 đảm bảo nó xoay đủ vòng chứ không đứng im)
        transform.DORotate(rotateAngle, rotateDuration, RotateMode.FastBeyond360)
                 .SetEase(Ease.OutCubic); // Hiệu ứng nhanh dần rồi chậm lại khi tiếp đất
    }

    private void OnDestroy()
    {
        // Dọn dẹp tween khi nhân vật bị hủy
        transform.DOKill();
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
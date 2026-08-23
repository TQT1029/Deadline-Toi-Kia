using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Đã gỡ bỏ RequireComponent để hỗ trợ cấu trúc Object cha chứa nhiều Object con mang Collider
public class SurfaceEffect : MonoBehaviour
{
    public enum EffectType
    {
        [Tooltip("Cần người chơi bấm nhảy (hoặc Bot có ý định nhảy) để kích hoạt nảy.")]
        InteractiveBounce,
        [Tooltip("Chỉ cần chạm vào là tự động bắn lên cao (như bệ phóng).")]
        AutoLaunchPad,
        [Tooltip("Đẩy thực thể liên tục về một hướng (như băng chuyền hoặc gió thổi).")]
        Treadmill
    }

    [Header("Core Settings")]
    [Tooltip("Loại hiệu ứng bề mặt sẽ áp dụng.")]
    public EffectType currentEffect = EffectType.InteractiveBounce;

    [Tooltip("Danh sách các Tag được phép tương tác với bề mặt này.")]
    public List<string> validTags = new List<string> { "Player", "Bot" };

    [Header("Force Settings")]
    [Tooltip("Lực nảy hoặc lực đẩy áp dụng lên thực thể.")]
    public float appliedForce = 25f;
    [Tooltip("Hướng của lực (Mặc định là hướng lên). Dùng cho LaunchPad hoặc Treadmill.")]
    public Vector2 forceDirection = Vector2.up;

    [Header("Input Forgiveness")]
    [Tooltip("Thời gian đệm phím (giây). Bấm nhảy sớm trước khi chạm bục chừng này thời gian vẫn được tính.")]
    public float jumpBufferTime = 0.2f;
    private float lastJumpPressTime = -100f; // Lưu thời điểm bấm phím gần nhất

    [Header("Visual Bounce Settings")]
    [Tooltip("Có phát hoạt ảnh lún/nảy hình học khi được kích hoạt không?")]
    public bool enableVisualSquish = true;
    [Tooltip("Tỷ lệ lún xuống (0.8 = 80% kích thước gốc).")]
    public float squishAmount = 0.8f;
    [Tooltip("Thời gian lún xuống (giây).")]
    public float downDuration = 0.05f;
    [Tooltip("Thời gian nảy lên lại (giây).")]
    public float upDuration = 0.15f;

    // Internal State
    private Vector3 originalScale;
    private Coroutine bounceCoroutine;
    private bool isAnimationPlaying = false;
    private HashSet<GameObject> activeEntities = new HashSet<GameObject>();
    private HashSet<GameObject> pendingBounceEntities = new HashSet<GameObject>();
    private Dictionary<GameObject, Collider2D[]> cachedEntityColliders = new Dictionary<GameObject, Collider2D[]>();
    private List<GameObject> entitiesToProcess = new List<GameObject>();
    private Collider2D[] myColliders;

    private void Start()
    {
        originalScale = transform.localScale;
        myColliders = GetComponentsInChildren<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsValidEntity(collision.gameObject))
        {
            activeEntities.Add(collision.gameObject);
            if (!cachedEntityColliders.ContainsKey(collision.gameObject))
            {
                cachedEntityColliders[collision.gameObject] = collision.gameObject.GetComponentsInChildren<Collider2D>();
            }

            // Kích hoạt ngay lập tức nếu là AutoLaunchPad
            if (currentEffect == EffectType.AutoLaunchPad)
            {
                if (IsEntityOnTop(collision.gameObject))
                {
                    ApplyForce(collision.gameObject, true);
                    if (enableVisualSquish) TriggerVisualBounce();
                }
            }
        }
    }

    private void OnEnable()
    {
        InputManager.OnJumpDown += RecordJumpBuffer;
    }

    private void OnDisable()
    {
        InputManager.OnJumpDown -= RecordJumpBuffer;

        if (isAnimationPlaying)
        {
            StopAllCoroutines();
            transform.localScale = originalScale;
            isAnimationPlaying = false;
        }
    }

    private void RecordJumpBuffer()
    {
        lastJumpPressTime = Time.time;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (IsValidEntity(collision.gameObject))
        {
            activeEntities.Remove(collision.gameObject);
            cachedEntityColliders.Remove(collision.gameObject);
        }
    }

    private void Update()
    {
        // 1. NẾU KHÔNG CÓ AI TRÊN BỤC THÌ DỪNG XỬ LÝ
        if (activeEntities.Count == 0) return;

        entitiesToProcess.Clear();
        entitiesToProcess.AddRange(activeEntities);

        for (int i = 0; i < entitiesToProcess.Count; i++)
        {
            GameObject entity = entitiesToProcess[i];
            if (entity == null)
            {
                activeEntities.Remove(entity);
                cachedEntityColliders.Remove(entity);
                continue;
            }

            if (!IsEntityOnTop(entity)) continue;

            switch (currentEffect)
            {
                case EffectType.InteractiveBounce:
                    HandleInteractiveBounce(entity);
                    break;
                case EffectType.Treadmill:
                    ApplyForce(entity, false);
                    break;
            }
        }
    }
    private void HandleInteractiveBounce(GameObject entity)
    {
        // Tránh lỗi gọi nảy liên tục nhiều lần trên 1 frame nếu Bot đang trong thời gian delay
        if (pendingBounceEntities.Contains(entity)) return;

        bool shouldBounce = false;
        bool isBot = false;

        if (entity.CompareTag(GameConstants.TAG_PLAYER))
        {
            // ĐIỀU KIỆN 1: Người chơi đang giữ phím Nhảy lúc chạm bục (InputManager / Touch / Mouse)
            bool isHoldingJump = InputManager.IsJumpHolding;

            // ĐIỀU KIỆN 2: Người chơi vừa bấm Nhảy trong khoảng buffer time (Jump Buffer)
            bool isBufferedJump = (Time.time - lastJumpPressTime) <= jumpBufferTime;

            // Chỉ cần thỏa mãn 1 trong 2 điều kiện là được nảy
            if (isHoldingJump || isBufferedJump)
            {
                shouldBounce = true;
                lastJumpPressTime = -100f; // Reset buffer ngay lập tức để tránh nảy đúp
            }
        }
        else if (entity.CompareTag(GameConstants.TAG_BOT))
        {
            Rigidbody2D rb = entity.GetComponent<Rigidbody2D>();
#if UNITY_6000_0_OR_NEWER
            if (rb != null && rb.linearVelocity.y > 0.1f)
#else
            if (rb != null && rb.velocity.y > 0.1f) 
#endif
            {
                shouldBounce = true;
                isBot = true;
            }
        }

        if (shouldBounce)
        {
            if (isBot)
            {
                // Nếu là Bot, cho một khoảng delay ngẫu nhiên nhỏ (ví dụ từ 0.05s đến 0.25s)
                float randomBotDelay = Random.Range(0.05f, 0.25f);
                StartCoroutine(DelayedBounceRoutine(entity, randomBotDelay));
            }
            else
            {
                // Nếu là Player, thực thi nảy ngay lập tức để cảm giác chơi mượt mà nhất
                ExecuteBounce(entity);
            }
        }
    }

    // Coroutine xử lý chờ delay cho Bot
    private IEnumerator DelayedBounceRoutine(GameObject entity, float delay)
    {
        // Đưa Bot vào danh sách chờ để Update không gọi hàm nhảy nữa
        pendingBounceEntities.Add(entity);

        // Chờ hết khoảng delay ngẫu nhiên
        yield return new WaitForSeconds(delay);

        // Sau khi chờ xong, kiểm tra xem Bot còn sống và CÒN ĐỨNG TRÊN BỤC không? (Lỡ nó bị đụng rớt khỏi bục trong lúc chờ)
        if (entity != null && activeEntities.Contains(entity))
        {
            ExecuteBounce(entity);
        }

        // Xóa Bot khỏi danh sách chờ
        pendingBounceEntities.Remove(entity);
    }

    // Hàm thực thi vật lý & animation chung
    private void ExecuteBounce(GameObject entity)
    {
        ApplyForce(entity, true);
        if (enableVisualSquish) TriggerVisualBounce();
    }

    private void ApplyForce(GameObject entity, bool clearVelocityY)
    {
        Rigidbody2D rb = entity.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            if (clearVelocityY)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
#else
                rb.velocity = new Vector2(rb.velocity.x, 0);
#endif
            }

            ForceMode2D mode = (currentEffect == EffectType.Treadmill) ? ForceMode2D.Force : ForceMode2D.Impulse;
            rb.AddForce(forceDirection.normalized * appliedForce, mode);
        }
    }
    private bool IsValidEntity(GameObject obj)
    {
        foreach (string tag in validTags)
        {
            if (obj.CompareTag(tag)) return true;
        }
        return false;
    }

    private bool IsEntityOnTop(GameObject entity)
    {
        if (myColliders == null || myColliders.Length == 0) return false;

        // 1. TÍNH TOÁN TỔNG KHỐI CỦA NHÂN VẬT TỪ CACHE
        if (!cachedEntityColliders.TryGetValue(entity, out Collider2D[] entityCols) || entityCols == null || entityCols.Length == 0)
        {
            entityCols = entity.GetComponentsInChildren<Collider2D>();
            cachedEntityColliders[entity] = entityCols;
            if (entityCols.Length == 0) return false;
        }

        Bounds entityBounds = entityCols[0].bounds;
        for (int i = 1; i < entityCols.Length; i++)
        {
            if (entityCols[i] != null && !entityCols[i].isTrigger) entityBounds.Encapsulate(entityCols[i].bounds);
        }

        // 2. TÍNH TOÁN TỔNG KHỐI CỦA BỤC NẢY
        Bounds surfaceBounds = myColliders[0].bounds;
        for (int i = 1; i < myColliders.Length; i++)
        {
            if (myColliders[i] != null && !myColliders[i].isTrigger) surfaceBounds.Encapsulate(myColliders[i].bounds);
        }

        // 3. So sánh: Điểm thấp nhất của cụm nhân vật (gót chân) phải lớn hơn hoặc bằng 1/5 của cụm bục
        float checkBoundY = surfaceBounds.min.y + (surfaceBounds.size.y) / 5f;

        return entityBounds.min.y >= checkBoundY;
    }

    // --- Animation Routine ---
    private void TriggerVisualBounce()
    {
        if (bounceCoroutine != null) StopCoroutine(bounceCoroutine);
        bounceCoroutine = StartCoroutine(AnimateBounce());
    }

    private IEnumerator AnimateBounce()
    {
        isAnimationPlaying = true;
        Vector3 targetSquishScale = new Vector3(originalScale.x, originalScale.y * squishAmount, originalScale.z);

        float elapsedTime = 0f;
        while (elapsedTime < downDuration)
        {
            elapsedTime += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, targetSquishScale, elapsedTime / downDuration);
            yield return null;
        }
        transform.localScale = targetSquishScale;

        elapsedTime = 0f;
        while (elapsedTime < upDuration)
        {
            elapsedTime += Time.deltaTime;
            transform.localScale = Vector3.Lerp(targetSquishScale, originalScale, elapsedTime / upDuration);
            yield return null;
        }
        transform.localScale = originalScale;
        isAnimationPlaying = false;
    }
}
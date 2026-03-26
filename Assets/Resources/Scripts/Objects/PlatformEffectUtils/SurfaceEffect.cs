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
    private Collider2D[] myColliders;

    private void Start()
    {
        originalScale = transform.localScale;

        // Lấy TẤT CẢ collider của bục nảy (kể cả ở các object con)
        myColliders = GetComponentsInChildren<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsValidEntity(collision.gameObject))
        {
            activeEntities.Add(collision.gameObject);

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

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (IsValidEntity(collision.gameObject))
        {
            activeEntities.Remove(collision.gameObject);
        }
    }

    private void Update()
    {
        if (activeEntities.Count == 0) return;

        List<GameObject> entitiesToProcess = new List<GameObject>(activeEntities);

        foreach (GameObject entity in entitiesToProcess)
        {
            if (entity == null)
            {
                activeEntities.Remove(entity);
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
        bool shouldBounce = false;

        if (entity.CompareTag("Player") && Input.GetButtonDown("Jump"))
        {
            shouldBounce = true;
        }
        else if (entity.CompareTag("Bot"))
        {
            Rigidbody2D rb = entity.GetComponent<Rigidbody2D>();
#if UNITY_6000_0_OR_NEWER
            if (rb != null && rb.linearVelocity.y > 0.1f) shouldBounce = true;
#else
            if (rb != null && rb.velocity.y > 0.1f) shouldBounce = true;
#endif
        }

        if (shouldBounce)
        {
            ApplyForce(entity, true);
            if (enableVisualSquish) TriggerVisualBounce();
        }
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

        // 1. TÍNH TOÁN TỔNG KHỐI CỦA NHÂN VẬT (Hỗ trợ Đa Collider cho Player/Bot)
        Collider2D[] entityCols = entity.GetComponentsInChildren<Collider2D>();
        if (entityCols.Length == 0) return false;

        Bounds entityBounds = entityCols[0].bounds;
        for (int i = 1; i < entityCols.Length; i++)
        {
            if (!entityCols[i].isTrigger) entityBounds.Encapsulate(entityCols[i].bounds);
        }

        // 2. TÍNH TOÁN TỔNG KHỐI CỦA BỤC NẢY (Hỗ trợ Đa Collider cho Surface)
        Bounds surfaceBounds = myColliders[0].bounds;
        for (int i = 1; i < myColliders.Length; i++)
        {
            if (!myColliders[i].isTrigger) surfaceBounds.Encapsulate(myColliders[i].bounds);
        }

        // 3. So sánh: Điểm thấp nhất của cụm nhân vật (gót chân) phải lớn hơn hoặc bằng Tâm của cụm bục
        return entityBounds.min.y >= surfaceBounds.center.y;
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

    private void OnDisable()
    {
        if (isAnimationPlaying)
        {
            StopAllCoroutines();
            transform.localScale = originalScale;
            isAnimationPlaying = false;
        }
    }
}
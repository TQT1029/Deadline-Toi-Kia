using UnityEngine;

public class MoveProjecties : MonoBehaviour
{
    private Vector3 moveDirection;
    private float moveSpeed;

    private Rigidbody2D rb;
    private float currentRotateSpeed;

    // Thời gian tối đa tồn tại (để tránh rác bộ nhớ nếu vật bay ra khỏi màn hình quá xa)
    private float maxLifetime = 3f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private Coroutine moveCoroutine;

    public void Initialize(Vector3 startWorldPos, Vector3 targetWorldPos, float speed, float rotateSpeed, float delayTime)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        transform.position = startWorldPos;

        moveDirection = (targetWorldPos - startWorldPos).normalized;
        moveSpeed = speed;
        currentRotateSpeed = rotateSpeed;

        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(StartMovingProcess(delayTime));
    }

    private System.Collections.IEnumerator StartMovingProcess(float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = moveDirection * moveSpeed;
#else
            rb.velocity = moveDirection * moveSpeed;
#endif
            rb.angularVelocity = currentRotateSpeed * 360f;
        }

        yield return new WaitForSeconds(maxLifetime);
        ReturnToPool();
    }

    public void ReturnToPool()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero;
#endif
            rb.angularVelocity = 0f;
        }

        SimpleObjectPool<MoveProjecties>.Return(this);
    }

    private void OnDisable()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero;
#endif
            rb.angularVelocity = 0f;
        }
    }
}
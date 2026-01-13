using UnityEngine;
using DG.Tweening; // 1. Nhớ thêm thư viện này

public class WinPoint : MonoBehaviour
{
    [SerializeField] private Transform exitGateTrans;

    [Header("Animation Settings")]
    [SerializeField] private float animDuration = 1.5f; // Thời gian bay vào cổng
    [SerializeField] private float arcHeight = 2.0f;    // Độ cao của vòng cung
    [SerializeField] private int rotationLoops = 2;     // Số vòng xoay (720 độ)

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (exitGateTrans == null) return;

        if (GameUtils.CompareLayer(other.gameObject, "Racer"))
        {
            // Tắt vật lý
            // Để tránh Player bị rơi xuống đất hoặc va chạm lung tung trong lúc đang bay vào cổng
            Rigidbody2D otherRB = other.GetComponent<Rigidbody2D>();
            if (otherRB != null)
            {
#if UNITY_6000_0_OR_NEWER
                otherRB.linearVelocity = Vector2.zero;
#else
                rb.velocity = Vector2.zero;
#endif
                otherRB.bodyType = RigidbodyType2D.Kinematic; // Tắt vật lý để DOTween toàn quyền điều khiển
            }

            // Tắt Collider để không va chạm thêm
            Collider2D col = other.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            PlayWinSequence(other.gameObject);
        }
    }

    private void PlayWinSequence(GameObject raccer)
    {
        Sequence sequence = OnDotweenAnimation(raccer.transform);

        if (raccer.CompareTag("Player"))
        {
            sequence.OnComplete(() =>
            {
                // Ẩn player
                raccer.gameObject.SetActive(false);

                // Gọi GameManager hiển thị Victory
                if (GameManager.Instance != null)
                    GameManager.Instance.ChangeState(GameState.Victory);


                // Dừng thời gian
                Time.timeScale = 0f;
            });
            return;
        }
        else
        {
            sequence.OnComplete(() =>
            {
                raccer.gameObject.SetActive(false);
            });
        }


    }

    private Sequence OnDotweenAnimation(Transform raccer)
    {
        Sequence seq = DOTween.Sequence();

        // Di chuyển theo đường vòng cung (Dùng DOJump là cách dễ nhất để tạo Arc)
        seq.Join(raccer.DOJump(exitGateTrans.position, arcHeight, 1, animDuration)
            .SetEase(Ease.InOutSine));

        // Xoay tròn (Vừa bay vừa xoay)
        // RotateMode.FastBeyond360 giúp xoay nhiều vòng đúng hướng
        seq.Join(raccer.DORotate(new Vector3(0, 0, 360 * rotationLoops), animDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear));

        // Thu nhỏ lại
        seq.Join(raccer.DOScale(Vector3.zero, animDuration)
            .SetEase(Ease.InBack)); // InBack tạo cảm giác bị hút vào

        return seq;

    }
}
using UnityEngine;
using DG.Tweening;

public class WinChecking : MonoBehaviour
{
    [SerializeField] private Transform exitGateTrans;

    [Header("Animation Settings")]
    [SerializeField] private float animDuration = 1.5f;
    [SerializeField] private float arcHeight = 2.0f;
    [SerializeField] private int rotationLoops = 2;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (exitGateTrans == null) return;

        if (GameUtils.CompareLayer(other.gameObject, "Racer"))
        {
            // Tắt vật lý để DOTween toàn quyền điều khiển
            Rigidbody2D otherRB = other.GetComponent<Rigidbody2D>();
            if (otherRB != null)
            {
#if UNITY_6000_0_OR_NEWER
                otherRB.linearVelocity = Vector2.zero;
#else
                otherRB.velocity = Vector2.zero;
#endif
                otherRB.bodyType = RigidbodyType2D.Kinematic;
            }

            // Tắt Collider để không va chạm thêm
            Collider2D col = other.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            PlayWinSequence(other.gameObject);
        }
    }

    private void PlayWinSequence(GameObject racer)
    {
        Sequence sequence = OnDotweenAnimation(racer.transform);

        if (racer.CompareTag(GameConstants.TAG_PLAYER))
        {
            sequence.OnComplete(() =>
            {
                racer.SetActive(false);

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ChangeState(GameState.Victory);
                }
            });
        }
        else
        {
            sequence.OnComplete(() =>
            {
                racer.SetActive(false);
            });
        }
    }

    private Sequence OnDotweenAnimation(Transform racer)
    {
        Sequence seq = DOTween.Sequence();

        float jumpPower = Mathf.Abs(racer.position.y + exitGateTrans.position.y) / 2f + arcHeight;
        seq.Join(racer.DOJump(exitGateTrans.position, jumpPower, 1, animDuration).SetEase(Ease.InOutSine));
        seq.Join(racer.DORotate(new Vector3(0, 0, 360 * rotationLoops), animDuration, RotateMode.FastBeyond360).SetEase(Ease.Linear));
        seq.Join(racer.DOScale(Vector3.zero, animDuration).SetEase(Ease.InBack));

        return seq;
    }
}
using UnityEngine;
using DG.Tweening;

public class SelectionArrow : MonoBehaviour
{
    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 1.5f, 0);
    public float moveDuration = 0.4f;
    public Ease moveEase = Ease.OutBack;

    [Header("Bobbing")]
    public float bobDistance = 0.2f;
    public float bobDuration = 0.5f;

    public void MoveTo(Transform target)
    {
        transform.DOKill();
        Vector3 targetPos = target.position + offset;

        transform.DOMove(targetPos, moveDuration)
            .SetEase(moveEase)
            .OnComplete(() => StartBobbing(targetPos));
    }

    private void StartBobbing(Vector3 basePos)
    {
        transform.position = basePos;
        transform.DOMoveY(basePos.y - bobDistance, bobDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private void OnDisable() => transform.DOKill();
}
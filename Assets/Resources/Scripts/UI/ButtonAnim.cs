using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonAnim : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float pressScale = 0.9f;
    [SerializeField] private float duration = 0.1f;
    [SerializeField] private Ease easeType = Ease.OutQuad;

    private Vector3 originalScale;

    private void Awake() => originalScale = transform.localScale;
    private void OnEnable() => transform.localScale = originalScale;
    private void OnDisable()
    {
        transform.DOKill();
        transform.localScale = originalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(originalScale * pressScale, duration)
                 .SetEase(easeType).SetUpdate(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(originalScale, duration)
                 .SetEase(Ease.OutElastic).SetUpdate(true);
    }
}
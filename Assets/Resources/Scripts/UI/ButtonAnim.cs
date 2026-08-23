using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonAnim : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Animation Settings")]
    [SerializeField] private float pressScale = 0.9f;
    [SerializeField] private float duration = 0.1f;
    [SerializeField] private Ease easeType = Ease.OutQuad;

    [Header("Audio Settings")]
    [Tooltip("ID của âm thanh trong AudioManager (VD: 'ButtonClick_')")]
    [SerializeField] private string clickDownSoundId = "ButtonClick_";
    [SerializeField] private bool randomizeButtonSound = true;

    private Vector3 originalScale;
    private string resolvedSoundId;

    private void Awake()
    {
        originalScale = transform.localScale;
        resolvedSoundId = randomizeButtonSound ? $"{clickDownSoundId}{Random.Range(0, 6)}" : clickDownSoundId;
    }
    private void OnEnable() => transform.localScale = originalScale;
    private void OnDisable()
    {
        transform.localScale = originalScale;
        transform.DOKill();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(originalScale * pressScale, duration)
                 .SetEase(easeType).SetUpdate(true);

        // --- THÊM SFX KHI NHẤN ---
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(resolvedSoundId))
        {
            AudioManager.Instance.PlaySFX(resolvedSoundId);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(originalScale, duration)
                 .SetEase(Ease.OutElastic).SetUpdate(true);
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}
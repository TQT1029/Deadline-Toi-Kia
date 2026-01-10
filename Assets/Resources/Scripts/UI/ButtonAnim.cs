using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using static RandomUtils;

public class ButtonAnim : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Animation Settings")]
    [SerializeField] private float pressScale = 0.9f;
    [SerializeField] private float duration = 0.1f;
    [SerializeField] private Ease easeType = Ease.OutQuad;

    [Header("Audio Settings")]
    [Tooltip("ID của âm thanh trong AudioManager (VD: 'UI_Click')")]
    [SerializeField] private string clickDownSoundId = "ButtonClick_"; // ID mặc định

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
        clickDownSoundId += Random.Range(0,6);


    }
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

        // --- THÊM SFX KHI NHẤN ---
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(clickDownSoundId))
        {
            AudioManager.Instance.PlaySFX(clickDownSoundId);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.DOKill();
        transform.DOScale(originalScale, duration)
                 .SetEase(Ease.OutElastic).SetUpdate(true);
    }
}
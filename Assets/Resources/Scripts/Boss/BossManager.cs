using DG.Tweening;
using System.Collections;
using System.Collections.Generic; // List
using Unity.Cinemachine;
using UnityEngine;
/// <summary>
/// Manages boss encounters, state, and related visual and camera effects during boss fights.
/// </summary>
/// <remarks>This class provides a singleton instance for coordinating boss fight logic, including loading boss
/// data, controlling boss visuals, and handling camera transitions. It should be attached to a GameObject in the scene.
/// Access the current boss data using the static property, and use the provided methods to start or stop boss fights as
/// needed.</remarks>

public class BossManager : MonoBehaviour
{
    public static BossManager Instance;
    private void Awake() => Instance = this;

    [Header("References")]
    public ProjectiesControl projectiesControl;
    public Transform bossVisualTransform;
    [SerializeField] private SpriteRenderer bossSpriteRenderer; // Sprite con để hiển thị ảnh
    [SerializeField] private Animator bossAnimator;
    [SerializeField] private CinemachineCamera vCam;

    [Header("Data")]
    public List<BossData_SO> allBosses;
    public static BossData_SO currentBossData { get; private set; } // Dữ liệu Boss hiện tại đang đánh

    [Header("Camera Settings")]
    [SerializeField] private float baseViewport = 7f;
    [SerializeField] private float zoomViewport = 10f;
    [SerializeField] private float zoomDuration = 1.5f;

    [Header("Movement Settings")]
    [Tooltip("Thời gian để Boss đuổi kịp Camera. Càng lớn càng trễ (mềm), càng nhỏ càng cứng. Gợi ý: 0.1 - 0.3")]
    [SerializeField] private float positionDamping = 0.2f;

    private Vector3 currentVelocity = Vector3.zero;

    [Header("Internal State")]
    private bool isFighting = false;
    private Vector3 currentViewportPos = new Vector3(0.8f, 5f, 0f);
    private float bossDepth = 10f;

    private Tween idleTween;
    private Tween zoomTween;

    private Camera _cachedCam;

    private void Start()
    {
        _cachedCam = Camera.main;
        if (vCam != null) baseViewport = vCam.Lens.OrthographicSize;
    }

    private void LateUpdate()
    {
        if (bossVisualTransform != null)
        {
            if (_cachedCam == null) _cachedCam = Camera.main;
            if (_cachedCam == null) return;

            Vector3 targetWorldPos = _cachedCam.ViewportToWorldPoint(new Vector3(currentViewportPos.x, currentViewportPos.y, bossDepth));

            bossVisualTransform.position = Vector3.SmoothDamp(
                bossVisualTransform.position,
                targetWorldPos,
                ref currentVelocity,
                positionDamping
            );
        }
    }

    public void StartBossFight(int bossIndex = -1)
    {
        if (isFighting) return;

        if (allBosses == null || allBosses.Count == 0)
        {
            Debug.LogError("[BossManager] allBosses list is empty! Cannot start boss fight.");
            return;
        }

        // 1. Load dữ liệu Boss
        if (bossIndex >= 0 && bossIndex < allBosses.Count)
        {
            currentBossData = allBosses[bossIndex];
        }
        else
        {
            currentBossData = allBosses[Random.Range(0, allBosses.Count)];
        }

        if (currentBossData == null)
        {
            Debug.LogError("[BossManager] Selected BossData is null!");
            return;
        }

        // 2. Setup Visual
        if (bossSpriteRenderer != null)
        {
            bossSpriteRenderer.flipX = currentBossData.flipXAnimator;
        }

        if (bossAnimator != null)
        {
            bossAnimator.runtimeAnimatorController = currentBossData.bossAnimation;
        }

        if (bossVisualTransform != null)
        {
            bossVisualTransform.gameObject.SetActive(true);
        }

        // 3. Zoom & Enter Sequence
        HandleCameraZoom(zoomViewport);
        EnterBossSequence();
    }

    private void EnterBossSequence()
    {
        currentViewportPos = new Vector3(0.5f, 1.5f, 0f);
        DOTween.To(() => currentViewportPos, x => currentViewportPos = x, new Vector3(0.75f, 0.5f, 0f), 2f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                isFighting = true;
                StartCoroutine(CombatLoop());
            });
    }

    private IEnumerator CombatLoop()
    {
        while (isFighting)
        {
            if (projectiesControl != null)
            {
                ProjectiesControl.AttackPattern selectedPattern = GetRandomPatternFromData();
                projectiesControl.ExecuteAttack(selectedPattern);
            }

            float minInterval = (currentBossData != null) ? currentBossData.minAttackInterval : 1.5f;
            float maxInterval = (currentBossData != null) ? currentBossData.maxAttackInterval : 3.0f;
            float waitTime = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private ProjectiesControl.AttackPattern GetRandomPatternFromData()
    {
        if (currentBossData == null || currentBossData.availablePatterns == null || currentBossData.availablePatterns.Count == 0)
        {
            return ProjectiesControl.AttackPattern.RainDown_AllAtOnce;
        }

        int randIndex = Random.Range(0, currentBossData.availablePatterns.Count);
        return currentBossData.availablePatterns[randIndex];
    }

    private void HandleCameraZoom(float targetSize)
    {
        if (vCam == null) return;
        zoomTween?.Kill();
        zoomTween = DOTween.To(() => vCam.Lens.OrthographicSize, x => vCam.Lens.OrthographicSize = x, targetSize, zoomDuration).SetEase(Ease.InOutSine);
    }

    public void StopFight()
    {
        isFighting = false;
        StopAllCoroutines();
        idleTween?.Kill();

        HandleCameraZoom(baseViewport);

        DOTween.To(() => currentViewportPos, x => currentViewportPos = x, new Vector3(0.5f, 3f, 0f), 2f)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                if (bossVisualTransform != null)
                    bossVisualTransform.gameObject.SetActive(false);
            });
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        idleTween?.Kill();
        zoomTween?.Kill();
    }
}
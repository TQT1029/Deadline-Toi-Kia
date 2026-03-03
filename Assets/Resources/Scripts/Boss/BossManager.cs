using DG.Tweening;
using System.Collections;
using System.Collections.Generic; // List
using Unity.Cinemachine;
using UnityEngine;

public class BossManager : MonoBehaviour
{
    public static BossManager Instance;
    private void Awake() => Instance = this;

    [Header("References")]
    public ObstacleBossController obstacleController;
    public Transform bossVisualTransform; 
    [SerializeField] private SpriteRenderer bossSpriteRenderer; // Sprite con để hiển thị ảnh
    [SerializeField] private Animator bossAnimator;
    [SerializeField] private CinemachineCamera vCam;

    [Header("Data")]
    public List<BossDataSO> allBosses;
    private BossDataSO currentBossData; // Dữ liệu Boss hiện tại đang đánh

    [Header("Camera Settings")]
    [SerializeField] private float baseViewport = 7f;
    [SerializeField] private float zoomViewport = 10f;
    [SerializeField] private float zoomDuration = 1.5f;

    [Header("Internal State")]
    private bool isFighting = false;
    private Vector3 currentViewportPos = new Vector3(0.8f, 5f, 0f);
    private float bossDepth = 10f;

    private Tween idleTween;
    private Tween zoomTween;

    private void Start()
    {
        if (vCam != null) baseViewport = vCam.Lens.OrthographicSize;

    }

    private void LateUpdate()
    {
        if (bossVisualTransform != null)
        {
            Camera cam = Camera.main;
            Vector3 targetWorldPos = cam.ViewportToWorldPoint(new Vector3(currentViewportPos.x, currentViewportPos.y, bossDepth));
            bossVisualTransform.position = targetWorldPos;
        }
    }

    // Hàm gọi Boss (có thể truyền index hoặc loại Boss vào đây)
    public void StartBossFight(int bossIndex = 0)
    {
        if (isFighting) return;

        // 1. Load dữ liệu Boss
        if (bossIndex < 0 || bossIndex >= allBosses.Count)
        {
            Debug.LogError("Boss Index không hợp lệ! Load boss mặc định 0");
            currentBossData = allBosses[0];
        }
        else
        {
            currentBossData = allBosses[Random.Range(0, allBosses.Count)];
        }

        // 2. Setup Visual
        if (bossSpriteRenderer != null && currentBossData.bossSprite != null)
        {
            bossSpriteRenderer.flipX = currentBossData.flipXAnimator;
        }

        if (bossAnimator!= null)
        {
            bossAnimator.runtimeAnimatorController = currentBossData.bossAnimation;
        }

        bossVisualTransform.gameObject.SetActive(true);

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
                //StartBossIdleMotion();
                StartCoroutine(CombatLoop());
            });
    }

    private void StartBossIdleMotion()
    {
        // Loop bay qua bay lại
        idleTween = DOTween.To(() => currentViewportPos.x, x => currentViewportPos.x = x, 0.7f, 2f)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                idleTween = DOTween.To(() => currentViewportPos.x, x => currentViewportPos.x = x, 0.3f, 4f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            });
    }

    private IEnumerator CombatLoop()
    {
        while (isFighting)
        {
            // 1. Chọn random 1 chiêu TRONG LIST CỦA BOSS ĐÓ (chứ không phải tất cả enum)
            ObstacleBossController.AttackPattern selectedPattern = GetRandomPatternFromData();

            // 2. Thực thi
            obstacleController.ExecuteAttack(selectedPattern);

            // 3. Nghỉ theo config của Boss Data
            float waitTime = Random.Range(currentBossData.minAttackInterval, currentBossData.maxAttackInterval);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private ObstacleBossController.AttackPattern GetRandomPatternFromData()
    {
        if (currentBossData == null || currentBossData.availablePatterns.Count == 0)
        {
            // Fallback nếu quên config data
            return ObstacleBossController.AttackPattern.RainDown_AllAtOnce;
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

        DOTween.To(() => currentViewportPos, x => currentViewportPos = x, new Vector3(0.5f, 1.5f, 0f), 1f)
            .SetEase(Ease.InBack)
            .OnComplete(() => bossVisualTransform.gameObject.SetActive(false));
    }
}
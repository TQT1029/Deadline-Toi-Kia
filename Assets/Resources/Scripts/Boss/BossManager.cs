using DG.Tweening;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class BossManager : MonoBehaviour
{
    public static BossManager Instance;
    private void Awake() => Instance = this;

    [Header("References")]
    public ObstacleBossController obstacleController;
    public Transform bossVisual;
    [SerializeField] private CinemachineCamera vCam;

    [Header("Camera Settings")]
    private float baseViewport = 5f; // Giá trị mặc định ban đầu
    [SerializeField] private float zoomViewport = 7f; // Giá trị khi gặp Boss (Zoom xa ra)
    [SerializeField] private float zoomDuration = 1.5f; // Thời gian thực hiện zoom


    [Header("Game Flow")]
    [SerializeField] private float minAttackInterval = 2f;
    [SerializeField] private float maxAttackInterval = 4f;
    [SerializeField] private float bossDepth = 10f; // Khoảng cách từ Camera đến Boss

    private bool isFighting = false;

    // BIẾN QUAN TRỌNG: Lưu vị trí Boss trên màn hình (0-1)
    // Mặc định start ở giữa chiều ngang (0.5), và tít trên cao ngoài màn hình (1.5)
    private Vector3 currentViewportPos = new Vector3(0.8f, 1.5f, 0f);

    private Tween idleTween;
    private Tween zoomTween;

    private void Start()
    {
        // Lưu lại kích thước camera ban đầu khi game bắt đầu
        if (vCam != null)
        {
            baseViewport = vCam.Lens.OrthographicSize;
        }
        else
        {
            Debug.LogError("Chưa gán CinemachineVirtualCamera vào BossManager!");
        }
    }

    // --- LOGIC DI CHUYỂN MỚI ---
    private void LateUpdate()
    {
        // Luôn cập nhật vị trí Boss theo Camera mỗi khung hình
        if (bossVisual != null)
        {
            // Lấy vị trí Camera hiện tại
            Camera cam = Camera.main;

            // Tính toán vị trí World dựa trên Viewport đã được Tween
            // Z dùng bossDepth để giữ khoảng cách cố định
            Vector3 targetWorldPos = cam.ViewportToWorldPoint(new Vector3(currentViewportPos.x, currentViewportPos.y, bossDepth));

            // Gán vào Boss
            bossVisual.position = targetWorldPos;
        }
    }

    // Hàm này được GameController gọi
    public void StartBossFight()
    {
        if (isFighting) return;
        bossVisual.gameObject.SetActive(true); // Đảm bảo boss bật

        HandleCameraZoom(zoomViewport);

        EnterBossSequence();
    }


    private void EnterBossSequence()
    {
        // 1. Reset vị trí Viewport về điểm xuất phát (ngoài màn hình trên cao)
        currentViewportPos = new Vector3(0.5f, 1.5f, 0f);

        // 2. Tween giá trị Viewport từ (0.5, 1.5) xuống (0.5, 0.75)
        // Dùng DOTween.To thay vì DOMove
        DOTween.To(() => currentViewportPos, x => currentViewportPos = x, new Vector3(0.5f, 0.75f, 0f), 2f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                isFighting = true;
                StartBossIdleMotion(); // Bắt đầu bay qua lại
                StartCoroutine(CombatLoop());
            });
    }

    private void StartBossIdleMotion()
    {
        // Boss bay qua bay lại nhẹ nhàng trong phạm vi màn hình
        // Từ 0.3 (trái) đến 0.7 (phải) để không bị sát mép quá

        // Bước 1: Bay sang phải
        idleTween = DOTween.To(() => currentViewportPos.x, x => currentViewportPos.x = x, 0.7f, 2f)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                // Bước 2: Loop Yoyo giữa 0.7 và 0.3
                idleTween = DOTween.To(() => currentViewportPos.x, x => currentViewportPos.x = x, 0.3f, 4f) // 4s cho 1 lượt qua lại chậm
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            });
    }

    private void HandleCameraZoom(float targetSize)
    {
        if (vCam == null) return;

        // Kill tween cũ nếu đang chạy dở để tránh xung đột
        zoomTween?.Kill();

        // Tween giá trị OrthographicSize trong struct m_Lens của Cinemachine
        zoomTween = DOTween.To(
                () => vCam.Lens.OrthographicSize,     // Getter
                x => vCam.Lens.OrthographicSize = x,  // Setter
                targetSize,                             // Target
                zoomDuration                            // Duration
            )
            .SetEase(Ease.InOutSine);
    }    
    //==========//
    private IEnumerator CombatLoop()
    {
        while (isFighting)
        {
            ObstacleBossController.AttackPattern randomPattern = GetRandomPattern();
            obstacleController.ExecuteAttack(randomPattern);
            yield return new WaitForSeconds(Random.Range(minAttackInterval, maxAttackInterval));
        }
    }

    private ObstacleBossController.AttackPattern GetRandomPattern()
    {
        var values = System.Enum.GetValues(typeof(ObstacleBossController.AttackPattern));
        return (ObstacleBossController.AttackPattern)values.GetValue(Random.Range(0, values.Length));
    }

    public void StopFight()
    {
        isFighting = false;
        StopAllCoroutines();
        idleTween?.Kill(); // Hủy Tween di chuyển


        // Trả cam về ban đầu

        HandleCameraZoom(baseViewport);

        // Optional: Boss bay ngược lên trời biến mất
        DOTween.To(() => currentViewportPos, x => currentViewportPos = x, new Vector3(0.5f, 1.5f, 0f), 1f)
            .SetEase(Ease.InBack)
            .OnComplete(() => bossVisual.gameObject.SetActive(false));

       
    }
}
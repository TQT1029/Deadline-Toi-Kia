using UnityEngine;
using UnityEngine.UI; // Nếu muốn hiển thị Text đếm ngược

public class EndlessGameManager : MonoBehaviour
{
    public static EndlessGameManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Header("References")]
    public Transform playerTransform;
    public Transform ground1; // Miếng sàn số 1
    public Transform ground2; // Miếng sàn số 2
    public GameObject winPointPrefab; // Prefab vạch đích

    [Header("Ground Loop Settings")]
    [Tooltip("Chiều dài của một miếng sàn (Sprite Width)")]
    public float groundLength = 20f;

    [Header("Game Flow Settings")]
    [Tooltip("Khoảng cách cần chạy trước khi bắt đầu đếm ngược")]
    public float distanceToTriggerTimer = 500f;

    [Tooltip("Thời gian đếm ngược để xuất hiện đích (Giây)")]
    public float countdownTime = 60f;

    [Header("UI (Optional)")]
    public Text timerText; // Kéo UI Text vào để hiện giờ

    // Private variables
    private float currentDistance;
    private bool isTimerRunning = false;
    private bool isWinSpawned = false;
    private float timeRemaining;

    private void Start()
    {
        timeRemaining = countdownTime;

        // Tự tìm Player nếu quên kéo
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // 1. Xử lý Vòng lặp sàn (Infinite Ground)
        HandleGroundLoop();

        // 2. Tính khoảng cách đã chạy
        currentDistance = playerTransform.position.x;

        // 3. Logic kích hoạt Timer
        if (!isTimerRunning && !isWinSpawned && currentDistance >= distanceToTriggerTimer)
        {
            isTimerRunning = true;
            Debug.Log("Bắt đầu đếm ngược về đích!");
        }

        // 4. Logic Đếm ngược & Spawn WinPoint
        if (isTimerRunning)
        {
            timeRemaining -= Time.deltaTime;

            // Cập nhật UI (nếu có)
            if (timerText != null)
                timerText.text = $"Time: {Mathf.Ceil(timeRemaining)}";

            if (timeRemaining <= 0)
            {
                SpawnWinPoint();
                isTimerRunning = false;
            }
        }
    }

    // --- LOGIC ĐẢO SÀN VÔ TẬN ---
    private void HandleGroundLoop()
    {
        // Kiểm tra Player đang đứng ở đâu so với sàn
        // Nếu Player chạy qua khỏi sàn 1 -> Chuyển sàn 1 ra sau sàn 2
        if (playerTransform.position.x > ground1.position.x + groundLength)
        {
            MoveGroundToNext(ground1, ground2);
        }

        // Nếu Player chạy qua khỏi sàn 2 -> Chuyển sàn 2 ra sau sàn 1
        if (playerTransform.position.x > ground2.position.x + groundLength)
        {
            MoveGroundToNext(ground2, ground1);
        }
    }

    // Hàm dời sàn "current" ra phía sau sàn "target"
    private void MoveGroundToNext(Transform current, Transform target)
    {
        Vector3 newPos = target.position;
        newPos.x += groundLength;
        current.position = newPos;
    }

    // --- LOGIC SPAWN ĐÍCH ---
    private void SpawnWinPoint()
    {
        if (isWinSpawned) return;
        isWinSpawned = true;

        // Spawn đích ở phía trước mặt người chơi một đoạn (ví dụ 30m)
        Vector3 winPos = new Vector3(playerTransform.position.x + 30f, playerTransform.position.y, 0);

        Instantiate(winPointPrefab, winPos, Quaternion.identity);

        Debug.Log("Đã mở cổng chiến thắng!");
        if (timerText != null) timerText.text = "GOAL!";
    }
}
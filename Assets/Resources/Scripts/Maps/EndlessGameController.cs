using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // Cần dùng List
using System.Linq; // Cần dùng để sắp xếp List ban đầu

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

    [Tooltip("Kéo tất cả các miếng sàn (Ground_1, Ground_2, Ground_3...) vào đây")]
    public List<Transform> groundPieces;

    public GameObject winPointPrefab;

    [Header("Ground Settings")]
    [Tooltip("Chiều dài của một miếng sàn (Sprite Width)")]
    public float groundLength = 20f;

    [Header("Pit (Hố) Settings")]
    [Tooltip("Tỉ lệ xuất hiện hố (%)")]
    [Range(0, 100)] public int pitChance = 30;

    [Tooltip("Độ rộng tối thiểu của hố")]
    public float minGap = 2f;

    [Tooltip("Độ rộng tối đa của hố (Đừng để quá lực nhảy của Player)")]
    public float maxGap = 4f;

    [Header("Game Flow Settings")]
    public float distanceToTriggerTimer = 500f;
    public float countdownTime = 60f;

    [Header("UI (Optional)")]
    public Text timerText;

    // Private variables
    private float currentDistance;
    private bool isTimerRunning = false;
    private bool isWinSpawned = false;
    private float timeRemaining;

    private void Start()
    {
        timeRemaining = countdownTime;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        // Sắp xếp lại List sàn theo thứ tự X tăng dần để đảm bảo logic chạy đúng
        // (Phòng trường hợp bạn kéo vào List lộn xộn)
        if (groundPieces.Count > 0)
        {
            groundPieces.Sort((a, b) => a.position.x.CompareTo(b.position.x));
        }
    }

    private void Update()
    {
        if (playerTransform == null || groundPieces.Count == 0) return;

        // 1. Xử lý Vòng lặp sàn (Infinite Ground)
        HandleGroundLoop();

        // 2. Tính khoảng cách đã chạy
        currentDistance = playerTransform.position.x;

        // 3. Logic kích hoạt Timer (Giữ nguyên logic cũ)
        if (!isTimerRunning && !isWinSpawned && currentDistance >= distanceToTriggerTimer)
        {
            isTimerRunning = true;
            Debug.Log("Bắt đầu đếm ngược về đích!");
        }

        // 4. Logic Đếm ngược & Spawn WinPoint (Giữ nguyên logic cũ)
        if (isTimerRunning)
        {
            timeRemaining -= Time.deltaTime;
            if (timerText != null) timerText.text = $"Time: {Mathf.Ceil(timeRemaining)}";

            if (timeRemaining <= 0)
            {
                SpawnWinPoint();
                isTimerRunning = false;
            }
        }
    }

    // --- LOGIC ĐẢO SÀN MỚI (HỖ TRỢ LIST & GAP) ---
    private void HandleGroundLoop()
    {
        // Lấy miếng sàn đầu tiên trong danh sách (miếng đang ở phía sau cùng)
        Transform firstGround = groundPieces[0];

        // Kiểm tra nếu Player đã chạy qua miếng sàn này một đoạn an toàn (ví dụ: groundLength)
        // Cộng thêm 5f để chắc chắn nó đã ra khỏi màn hình bên trái
        if (playerTransform.position.x > firstGround.position.x + groundLength + 10f)
        {
            RecycleGround(firstGround);
        }
    }

    private void RecycleGround(Transform groundToMove)
    {
        // 1. Tìm miếng sàn đang ở xa nhất phía trước (miếng cuối cùng trong List)
        Transform lastGround = groundPieces[groundPieces.Count - 1];

        // 2. Tính toán vị trí mới
        // Mặc định nối tiếp nhau:
        float newX = lastGround.position.x + groundLength;

        // 3. Random Hố (Gap)
        // Chỉ tạo hố nếu không phải là đoạn đầu game (player.x > 50) để tránh rơi lúc mới vào
        if (playerTransform.position.x > 50f && Random.Range(0, 100) < pitChance)
        {
            float gap = Random.Range(minGap, maxGap);
            newX += gap; // Cộng thêm khoảng trống vào vị trí X
        }

        // 4. Di chuyển miếng sàn cũ lên vị trí mới
        Vector3 newPos = groundToMove.position;
        newPos.x = newX;
        groundToMove.position = newPos;

        // 5. Cập nhật lại Danh Sách:
        // Đưa miếng vừa di chuyển xuống cuối danh sách (vì giờ nó là miếng xa nhất)
        groundPieces.RemoveAt(0);
        groundPieces.Add(groundToMove);
    }

    private void SpawnWinPoint()
    {
        if (isWinSpawned) return;
        isWinSpawned = true;

        Vector3 winPos = new Vector3(playerTransform.position.x + 30f, playerTransform.position.y, 0);
        Instantiate(winPointPrefab, winPos, Quaternion.identity);

        if (timerText != null) timerText.text = "GOAL!";
    }
}
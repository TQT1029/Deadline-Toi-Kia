using UnityEngine;
using UnityEngine.UI;
using System.Collections; // Cần cho Coroutine
using System.Collections.Generic;
using System.Linq;

public class EndlessGameController : MonoBehaviour
{
    public static EndlessGameController Instance;

    public enum MapType { NoPits, WithPits }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // --- SỰ KIỆN QUAN TRỌNG: Gửi thông báo khi có sàn mới ---
    // Tham số 1: Mép trái (Start X), Tham số 2: Mép phải (End X)
    public event System.Action<float, float> OnPlatformSpawned;

    [Header("References")]
    public Transform playerTransform;
    public GameObject winPointPrefab;
    [Tooltip("Container chứa các sàn được sinh ra")]
    public GameObject platformObjs;

    [Header("Platform Library")]
    public List<PlatformData> platformLibrary;

    [SerializeField] private List<Transform> activePlatforms = new List<Transform>();

    [Header("Map Settings")]
    public MapType mapType = MapType.WithPits;

    [Header("Generation Config")]
    public float generationDistanceAhead = 80f;
    public float destroyDistanceBehind = 30f;

    [Header("Length Settings")]
    public bool useCommonLength = false;
    public float commonLength = 20f;

    [Header("Pit (Hố) Settings")]
    [Range(0, 100)] public int pitChance = 30;
    public float minGap = 2f;
    public float maxGap = 4f;

    [Header("Game Flow")]
    public float distanceToTriggerTimer = 500f;
    public float countdownTime = 60f;
    public Text timerText;

    private float currentDistance;
    private bool isTimerRunning = false;
    private bool isWinSpawned = false;
    private float timeRemaining;

    // Sử dụng IEnumerator cho Start để xử lý bất đồng bộ (chờ MapGenerator load xong)
    private IEnumerator Start()
    {
        timeRemaining = countdownTime;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        // Sắp xếp các sàn có sẵn
        if (activePlatforms.Count > 0)
        {
            activePlatforms.Sort((a, b) => a.position.x.CompareTo(b.position.x));
        }

        // --- QUAN TRỌNG: Chờ 1 frame để đảm bảo MapGenerator đã chạy hàm Start và đăng ký Event ---
        yield return null;

        // Sau khi chờ xong mới bắt đầu sinh map
        ManageMapGeneration();
    }

    private void Update()
    {
        if (playerTransform == null || activePlatforms.Count == 0) return;

        ManageMapGeneration();
        CleanupOldMap();
        HandleGameFlow();
    }

    private void ManageMapGeneration()
    {
        Transform farthestPlatform = activePlatforms[activePlatforms.Count - 1];
        float farthestEdge = GetPlatformRightEdge(farthestPlatform);

        // Sinh liên tục nếu chưa đủ độ dài
        while (farthestEdge < playerTransform.position.x + generationDistanceAhead)
        {
            SpawnNextPlatform();

            farthestPlatform = activePlatforms[activePlatforms.Count - 1];
            farthestEdge = GetPlatformRightEdge(farthestPlatform);
        }
    }

    private void SpawnNextPlatform()
    {
        Transform lastPlatform = activePlatforms[activePlatforms.Count - 1];
        float lastPlatformRightEdge = GetPlatformRightEdge(lastPlatform);

        PlatformData newData = GetRandomPlatformData();
        if (newData == null || newData.prefab == null) return;

        // Tính Gap
        float gap = 0f;
        if (mapType == MapType.WithPits && playerTransform.position.x > 0f)
        {
            if (Random.Range(0, 100) < pitChance) gap = Random.Range(minGap, maxGap);
            else gap = 0f;
        }

        Debug.Log($"[EndlessGameController] Gap: {gap}");
        // Sinh Object
        GameObject newObj = Instantiate(newData.prefab, lastPlatform.position, Quaternion.identity);
        if (platformObjs != null) newObj.transform.SetParent(platformObjs.transform);
        else newObj.transform.SetParent(this.transform);

        // Tính toán vị trí chính xác
        BoxCollider2D newCol = newObj.GetComponent<BoxCollider2D>();
        float halfWidth = 0f;
        float offsetX = 0f;

        if (newCol != null)
        {
            float scaledWidth = newCol.size.x * newObj.transform.localScale.x;
            halfWidth = scaledWidth / 2;
            offsetX = newCol.offset.x * newObj.transform.localScale.x;
        }
        else
        {
            float length = useCommonLength ? commonLength : newData.length;
            halfWidth = length / 2;
        }

        // Công thức: Mép phải cũ + Gap + (Một nửa độ rộng mới - Offset lệch tâm)
        float targetLeftEdgeX = lastPlatformRightEdge + gap;
        float distancePivotToLeftEdge = halfWidth - offsetX;
        float newCenterX = targetLeftEdgeX + distancePivotToLeftEdge;

        newObj.transform.position = new Vector3(newCenterX, lastPlatform.position.y, 0);
        activePlatforms.Add(newObj.transform);

        // --- BẮN SỰ KIỆN CHO MAPCONTROLLER ---
        // Gửi thông tin: "Tôi vừa tạo đất từ [targetLeftEdgeX] đến [targetLeftEdgeX + width]"
        // MapGenerator sẽ nhận tin này và điền item vào đó.
        float width = halfWidth * 2;
        OnPlatformSpawned?.Invoke(targetLeftEdgeX, targetLeftEdgeX + width);
    }

    private void CleanupOldMap()
    {
        if (activePlatforms.Count == 0) return;
        Transform oldestPlatform = activePlatforms[0];
        float oldestEdge = GetPlatformRightEdge(oldestPlatform);

        if (playerTransform.position.x > oldestEdge + destroyDistanceBehind)
        {
            activePlatforms.RemoveAt(0);
            Destroy(oldestPlatform.gameObject);
        }
    }

    private void HandleGameFlow()
    {
        currentDistance = playerTransform.position.x;
        if (!isTimerRunning && !isWinSpawned && currentDistance >= distanceToTriggerTimer)
        {
            isTimerRunning = true;
        }

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

    private float GetPlatformRightEdge(Transform platform)
    {
        BoxCollider2D col = platform.GetComponent<BoxCollider2D>();
        if (col != null) return col.bounds.max.x;
        else
        {
            float length = useCommonLength ? commonLength : 20f;
            return platform.position.x + (length / 2);
        }
    }

    private PlatformData GetRandomPlatformData()
    {
        if (platformLibrary == null || platformLibrary.Count == 0) return null;
        float totalWeight = 0;
        foreach (var p in platformLibrary) totalWeight += p.spawnWeight;
        float r = Random.Range(0, totalWeight);
        float c = 0;
        foreach (var p in platformLibrary) { c += p.spawnWeight; if (r < c) return p; }
        return platformLibrary[0];
    }

    private void SpawnWinPoint()
    {
        if (isWinSpawned) return;
        isWinSpawned = true;
        Transform lastP = activePlatforms[activePlatforms.Count - 1];
        float edge = GetPlatformRightEdge(lastP);
        Vector3 winPos = new Vector3(edge + 10f, lastP.position.y, 0);
        Instantiate(winPointPrefab, winPos, Quaternion.identity);
        if (timerText != null) timerText.text = "GOAL!";
    }
}
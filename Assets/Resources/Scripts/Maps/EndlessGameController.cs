using UnityEngine;
using UnityEngine.UI;
using System.Collections;
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

    // Sự kiện gửi: Mép Trái, Mép Phải, Độ Cao
    public event System.Action<float, float, float> OnPlatformSpawned;

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
    public float generationDistanceAhead = 80f;
    public float destroyDistanceBehind = 30f;

    [Header("Height Limits")]
    public float groundY = -2f;
    public float maxY = 6f;

    [Header("Length Settings")]
    [Tooltip("Nếu bật, bỏ qua thông số trong Data mà dùng Common Length")]
    public bool useCommonLength = false;
    public float commonLength = 20f;

    [Header("Pit Settings")]
    [Range(0, 100)] public int pitChance = 30;
    public float minGap = 2f;
    public float maxGap = 4f;

    [Header("Game Flow")]
    public float distanceToTriggerTimer = 500f;
    public float countdownTime = 60f;
    public Text timerText;

    private bool isTimerRunning = false;
    private bool isWinSpawned = false;
    private float timeRemaining;

    private IEnumerator Start()
    {
        timeRemaining = countdownTime;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        // Kiểm tra an toàn
        if (platformLibrary == null || platformLibrary.Count == 0)
        {
            Debug.LogError("EndlessGameController: Chưa có PlatformData nào trong Library!");
            yield break;
        }

        // Sắp xếp các sàn có sẵn (nếu có)
        if (activePlatforms.Count > 0)
            activePlatforms.Sort((a, b) => a.position.x.CompareTo(b.position.x));

        yield return null; // Chờ 1 frame

        // Nếu chưa có sàn nào, tạo sàn đầu tiên
        if (activePlatforms.Count == 0)
        {
            CreateFirstPlatform();
        }

        ManageMapGeneration();
    }

    private void CreateFirstPlatform()
    {
        if (platformLibrary.Count == 0) return;
        PlatformData firstData = platformLibrary[0];

        // Spawn tại 0,0
        GameObject newObj = Instantiate(firstData.prefab, new Vector3(0, groundY, 0), Quaternion.identity);
        if (platformObjs) newObj.transform.SetParent(platformObjs.transform);

        // [FIX] Đồng bộ Collider ngay cho sàn đầu tiên
        SyncColliderSize(newObj, firstData.length);

        activePlatforms.Add(newObj.transform);
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
        if (activePlatforms.Count == 0) return;

        Transform farthestPlatform = activePlatforms[activePlatforms.Count - 1];
        float farthestEdge = GetPlatformRightEdge(farthestPlatform);

        // SAFETY BREAK: Chỉ cho phép lặp tối đa 50 lần/frame để chống treo máy
        int loopGuard = 0;

        while (farthestEdge < playerTransform.position.x + generationDistanceAhead)
        {
            loopGuard++;
            if (loopGuard > 50)
            {
                Debug.LogError("INFINITE LOOP DETECTED: Đã dừng sinh map khẩn cấp! Kiểm tra lại độ dài Platform.");
                break;
            }

            bool success = SpawnNextPlatform();
            if (!success) break;

            // Cập nhật lại mốc
            farthestPlatform = activePlatforms[activePlatforms.Count - 1];
            farthestEdge = GetPlatformRightEdge(farthestPlatform);
        }
    }

    private bool SpawnNextPlatform()
    {
        Transform lastPlatform = activePlatforms[activePlatforms.Count - 1];
        float lastEdgeX = GetPlatformRightEdge(lastPlatform);
        float lastY = lastPlatform.position.y;

        PlatformData newData = GetRandomPlatformData();
        if (newData == null || newData.prefab == null) return false;

        // 1. Tính Gap
        float gap = 0f;
        if (mapType == MapType.WithPits && playerTransform.position.x > 50f)
        {
            if (Random.Range(0, 100) < pitChance) gap = Random.Range(minGap, maxGap);
        }

        // 2. Tính Height
        float newY = groundY;
        if (newData.isFlying)
        {
            float heightStep = Random.Range(newData.minHeightDiff, newData.maxHeightDiff);
            newY = lastY + heightStep;
            if (newY > maxY) newY = maxY;
            if (gap < 1f) gap = 1.5f;
        }
        else
        {
            newY = groundY;
            if (lastY > groundY + 1f) gap += 2f;
        }

        // 3. Sinh Object
        GameObject newObj = Instantiate(newData.prefab, Vector3.zero, Quaternion.identity);
        if (platformObjs != null) newObj.transform.SetParent(platformObjs.transform);
        else newObj.transform.SetParent(this.transform);

        // 4. [QUAN TRỌNG] Xác định chiều dài sử dụng
        float usedLength = useCommonLength ? commonLength : newData.length;
        if (usedLength < 1f) usedLength = 1f; // Không bao giờ để < 1

        // 5. [FIX LỖI CHỒNG LẤN] Tự động chỉnh Collider khớp với chiều dài nhập tay
        SyncColliderSize(newObj, usedLength);

        // 6. Tính toán vị trí dựa trên Collider đã chỉnh
        BoxCollider2D newCol = newObj.GetComponent<BoxCollider2D>();
        float halfWidth = usedLength / 2;
        float offsetX = 0f;

        if (newCol != null)
        {
            // Tính lại halfWidth chuẩn xác từ collider
            float scaledSize = newCol.size.x * newObj.transform.localScale.x;
            halfWidth = scaledSize / 2;
            offsetX = newCol.offset.x * newObj.transform.localScale.x;
        }

        float targetLeftEdgeX = lastEdgeX + gap;
        // Đảm bảo luôn tiến về phía trước ít nhất 0.1f
        if (targetLeftEdgeX <= lastEdgeX) targetLeftEdgeX = lastEdgeX + 0.1f;

        float finalCenterX = targetLeftEdgeX + (halfWidth - offsetX);

        newObj.transform.position = new Vector3(finalCenterX, newY, 0);
        activePlatforms.Add(newObj.transform);

        // 7. Bắn sự kiện
        float width = halfWidth * 2;
        OnPlatformSpawned?.Invoke(targetLeftEdgeX, targetLeftEdgeX + width, newY);

        return true;
    }

    // --- HÀM MỚI: ĐỒNG BỘ COLLIDER VỚI LENGTH NHẬP TAY ---
    private void SyncColliderSize(GameObject obj, float targetLength)
    {
        BoxCollider2D col = obj.GetComponent<BoxCollider2D>();
        if (col != null)
        {
            // Tính toán Size X cần thiết: targetLength / localScale.x
            float requiredSizeX = targetLength / obj.transform.localScale.x;

            // Gán lại size cho collider
            Vector2 currentSize = col.size;
            currentSize.x = requiredSizeX;
            col.size = currentSize;
        }
    }

    private void CleanupOldMap()
    {
        if (activePlatforms.Count == 0) return;
        Transform oldest = activePlatforms[0];

        if (oldest == null) { activePlatforms.RemoveAt(0); return; }

        float edge = GetPlatformRightEdge(oldest);
        if (playerTransform.position.x > edge + destroyDistanceBehind)
        {
            activePlatforms.RemoveAt(0);
            Destroy(oldest.gameObject);
        }
    }

    private void HandleGameFlow()
    {
        float currentDistance = playerTransform.position.x;
        if (!isTimerRunning && !isWinSpawned && currentDistance >= distanceToTriggerTimer)
            isTimerRunning = true;

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
        if (platform == null) return 0f;
        BoxCollider2D col = platform.GetComponent<BoxCollider2D>();

        // Vì ta đã Sync Collider ở trên, nên col.bounds.max.x bây giờ luôn đúng!
        if (col != null) return col.bounds.max.x;

        // Fallback
        float len = useCommonLength ? commonLength : 20f;
        return platform.position.x + (len / 2);
    }

    private PlatformData GetRandomPlatformData()
    {
        if (platformLibrary == null || platformLibrary.Count == 0) return null;
        float total = 0; foreach (var p in platformLibrary) total += p.spawnWeight;
        float r = Random.Range(0, total);
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
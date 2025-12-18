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

    public event System.Action<float, float, float> OnBasePlatformSpawned;

    [Header("References")]
    public Transform playerTransform;
    public GameObject winPointPrefab;
    public GameObject basePlatformContainer;

    [Header("Base Platform Config")]
    public List<PlatformData> basePlatformLibrary;

    [SerializeField] private List<Transform> activeBasePlatforms = new List<Transform>();

    [Header("Map Settings")]
    public MapType mapType = MapType.WithPits;
    public float generationDistanceAhead = 80f;
    public float destroyDistanceBehind = 30f;
    public float groundY = -2f; // Mặt đất luôn ở đây

    [Header("Length Settings")]
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
    private float lastEdgeX;

    private IEnumerator Start()
    {
        timeRemaining = countdownTime;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        if (basePlatformLibrary == null || basePlatformLibrary.Count == 0)
        {
            Debug.LogError("EndlessGameController: Chưa có Base Platform Data!");
            yield break;
        }

        if (activeBasePlatforms.Count == 0) CreateFirstPlatform();
        else
        {
            activeBasePlatforms.Sort((a, b) => a.position.x.CompareTo(b.position.x));
            lastEdgeX = GetPlatformRightEdge(activeBasePlatforms[activeBasePlatforms.Count - 1]);
        }

        yield return null;
        ManageMapGeneration();
    }

    private void CreateFirstPlatform()
    {
        PlatformData firstData = basePlatformLibrary[0];
        GameObject newObj = Instantiate(firstData.prefab, new Vector3(0, groundY, 0), Quaternion.identity);

        if (basePlatformContainer) newObj.transform.SetParent(basePlatformContainer.transform);

        SyncColliderSize(newObj, firstData.length);
        activeBasePlatforms.Add(newObj.transform);
        lastEdgeX = firstData.length / 2f;
    }

    private void Update()
    {
        if (playerTransform == null) return;
        ManageMapGeneration();
        CleanupOldMap();
        HandleGameFlow();
    }

    private void ManageMapGeneration()
    {
        int safetyLoop = 0;
        while (lastEdgeX < playerTransform.position.x + generationDistanceAhead)
        {
            safetyLoop++;
            if (safetyLoop > 50) break;
            if (!SpawnNextBasePlatform()) break;
        }
    }

    private bool SpawnNextBasePlatform()
    {
        PlatformData newData = GetRandomBasePlatform();
        if (newData == null) return false;

        float gap = 0f;
        if (mapType == MapType.WithPits && playerTransform.position.x > 50f)
        {
            if (Random.Range(0, 100) < pitChance) gap = Random.Range(minGap, maxGap);
        }

        float currentLength = Mathf.Max(newData.length, 1f);
        float newCenterX = lastEdgeX + gap + (currentLength / 2f);

        GameObject newObj = Instantiate(newData.prefab, new Vector3(newCenterX, groundY, 0), Quaternion.identity);

        if (basePlatformContainer) newObj.transform.SetParent(basePlatformContainer.transform);
        else newObj.transform.SetParent(this.transform);

        SyncColliderSize(newObj, currentLength);
        activeBasePlatforms.Add(newObj.transform);

        lastEdgeX = newCenterX + (currentLength / 2f);

        // Báo cho MapGenerator
        float leftEdge = lastEdgeX - currentLength;
        OnBasePlatformSpawned?.Invoke(leftEdge, lastEdgeX, groundY);

        return true;
    }

    private void CleanupOldMap()
    {
        if (activeBasePlatforms.Count == 0) return;
        Transform oldest = activeBasePlatforms[0];
        if (oldest == null) { activeBasePlatforms.RemoveAt(0); return; }

        float len = GetPlatformLength(oldest);
        float oldestEdge = oldest.position.x + (len / 2f);

        if (playerTransform.position.x > oldestEdge + destroyDistanceBehind)
        {
            activeBasePlatforms.RemoveAt(0);
            Destroy(oldest.gameObject);
        }
    }

    private void SyncColliderSize(GameObject obj, float targetLength)
    {
        BoxCollider2D col = obj.GetComponent<BoxCollider2D>();
        if (col != null)
        {
            float scaleX = obj.transform.localScale.x;
            if (scaleX == 0) scaleX = 1;
            Vector2 s = col.size; s.x = targetLength / scaleX; col.size = s;
            col.offset = new Vector2(0, col.offset.y);
        }
    }

    private float GetPlatformLength(Transform t)
    {
        BoxCollider2D col = t.GetComponent<BoxCollider2D>();
        if (col != null) return col.bounds.size.x;
        return useCommonLength ? commonLength : 20f;
    }

    private float GetPlatformRightEdge(Transform t)
    {
        BoxCollider2D col = t.GetComponent<BoxCollider2D>();
        if (col != null) return col.bounds.max.x;
        return t.position.x + 10f;
    }

    private PlatformData GetRandomBasePlatform()
    {
        if (basePlatformLibrary == null || basePlatformLibrary.Count == 0) return null;
        float t = 0; foreach (var p in basePlatformLibrary) t += p.spawnWeight;
        float r = Random.Range(0, t);
        float c = 0; foreach (var p in basePlatformLibrary) { c += p.spawnWeight; if (r < c) return p; }
        return basePlatformLibrary[0];
    }

    private void HandleGameFlow()
    {
        if (!isTimerRunning && !isWinSpawned && playerTransform.position.x >= distanceToTriggerTimer)
            isTimerRunning = true;

        if (isTimerRunning)
        {
            timeRemaining -= Time.deltaTime;
            if (timerText != null) timerText.text = $"Time: {Mathf.Ceil(timeRemaining)}";
            if (timeRemaining <= 0) { SpawnWinPoint(); isTimerRunning = false; }
        }
    }

    private void SpawnWinPoint()
    {
        if (isWinSpawned) return;
        isWinSpawned = true;
        Vector3 winPos = new Vector3(lastEdgeX + 10f, activeBasePlatforms.Last().position.y, 0);
        Instantiate(winPointPrefab, winPos, Quaternion.identity);
        if (timerText != null) timerText.text = "GOAL!";
    }
}
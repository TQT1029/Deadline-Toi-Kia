using UnityEngine;
using System.Collections.Generic;

public class MiniPlatformGenerator : MonoBehaviour
{
    public static MiniPlatformGenerator Instance;
    private void Awake() => Instance = this;

    [Header("Config")]
    public List<MiniPlatformData> miniPlatformLibrary;
    public Transform objectContainer;

    [Header("Settings")]
    [Range(0, 100)] public int spawnChance = 50;
    public int minChain = 2;
    public int maxChain = 5;
    public float minHeight = 2f;
    public float maxHeight = 5f;
    public float gapBetweenSteps = 2f;

    // Lưu trữ để ItemGenerator check
    public List<Collider2D> spawnedPlatforms = new List<Collider2D>();

    public void SpawnMiniPlatforms(float startX, float endX, float groundY)
    {
        spawnedPlatforms.Clear();

        // Bắt đầu spawn mini platform độc lập với obstacle, nhưng cùng trên đoạn đường đó
        float currentX = startX + UnityEngine.Random.Range(5f, 10f);

        while (currentX < endX - 5f)
        {
            if (UnityEngine.Random.Range(0, 100) < spawnChance)
            {
                int chainLength = UnityEngine.Random.Range(minChain, maxChain + 1);
                float currentHeight = UnityEngine.Random.Range(minHeight, maxHeight);

                for (int i = 0; i < chainLength; i++)
                {
                    MiniPlatformData data = GetRandomPlatform();
                    if (data == null) break;

                    if (currentX + data.Length > endX) break;

                    Vector3 pos = new Vector3(currentX + data.Length / 2f, groundY + currentHeight, 0);
                    GameObject obj = Instantiate(data.prefab, pos, Quaternion.identity);
                    obj.transform.SetParent(objectContainer);

                    Collider2D col = obj.GetComponent<Collider2D>();
                    if (col != null) spawnedPlatforms.Add(col);

                    currentX += data.Length + gapBetweenSteps;

                    // Logic bậc thang đơn giản (lên xuống ngẫu nhiên)
                    currentHeight += UnityEngine.Random.Range(-1.5f, 1.5f);
                    currentHeight = Mathf.Clamp(currentHeight, minHeight, maxHeight);
                }
            }
            currentX += UnityEngine.Random.Range(10f, 20f); // Khoảng cách giữa các chuỗi
        }
    }

    private MiniPlatformData GetRandomPlatform()
    {
        if (miniPlatformLibrary.Count == 0) return null;
        return miniPlatformLibrary[UnityEngine.Random.Range(0, miniPlatformLibrary.Count)];
    }
}
using UnityEngine;
using System.Collections.Generic;

public class BasePlatformSpawner : MonoBehaviour
{
    [Header("Config")]
    public List<BasePlatformData> library;
    public Transform container;

    [Header("Pit Settings")]
    [Range(0, 100)] public int pitChance = 30;
    public float minPitWidth = 3f;
    public float maxPitWidth = 6f;

    // Struct lưu thông tin đoạn đất để các spawner sau dùng
    public struct GroundSegment { public float startX; public float endX; public float y; }
    public struct PitSegment { public float startX; public float endX; }

    public void Spawn(float startGenX, float endGenX, float groundY,
                      out List<GroundSegment> grounds, out List<PitSegment> pits)
    {
        grounds = new List<GroundSegment>();
        pits = new List<PitSegment>();

        float currentX = startGenX;

        while (currentX < endGenX)
        {
            // 1. Sinh Đất
            BasePlatformData data = GetRandom();
            float length = data.GetLength();

            Vector3 pos = new Vector3(currentX + length / 2f, groundY, 0);
            GameObject obj = Instantiate(data.prefab, pos, Quaternion.identity, container);

            // Fix size collider nếu cần (để chắc chắn)
            var col = obj.GetComponent<BoxCollider2D>();
            if (col) col.size = new Vector2(length / obj.transform.localScale.x, col.size.y);

            // Lưu đoạn đất này lại
            grounds.Add(new GroundSegment { startX = currentX, endX = currentX + length, y = groundY });
            currentX += length;

            // 2. Quyết định Sinh Hố (sau mỗi đoạn đất)
            if (currentX < endGenX && Random.Range(0, 100) < pitChance)
            {
                float pitWidth = Random.Range(minPitWidth, maxPitWidth);
                pits.Add(new PitSegment { startX = currentX, endX = currentX + pitWidth });
                currentX += pitWidth; // Dời con trỏ qua hố
            }
        }

        // Buộc cập nhật Physics ngay lập tức để các bước sau Raycast trúng
        Physics2D.SyncTransforms();
    }

    private BasePlatformData GetRandom() => library[Random.Range(0, library.Count)];
}
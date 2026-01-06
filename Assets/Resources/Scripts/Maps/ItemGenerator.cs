using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class ItemGenerator : MonoBehaviour
{
    public static ItemGenerator Instance;
    private void Awake() => Instance = this;

    [System.Serializable]
    public class PatternTemplate
    {
        public string id;
        public GameObject prefab; // Prefab cha chứa các item con
        public Vector2 size;      // Kích thước bao quanh (Width, Height)
        public Vector2 centerOffset = new Vector2(0.5f, 0f); // Độ lệch để căn giữa (nếu cần)
    }

    [Header("References")]
    [SerializeField] private Transform patternPoolContainer; // Nơi chứa các mẫu pattern (ẩn)
    public Transform itemContainer; // Nơi chứa item khi spawn ra map

    [Tooltip("Item Prefab (Coin/Star...)")]
    [SerializeField] private List<ItemData> commonItems;

    // Danh sách các mẫu pattern đã được setup sẵn
    [SerializeField] private List<PatternTemplate> patternTemplates = new List<PatternTemplate>();

    [Header("Settings")]
    [SerializeField] private LayerMask surfaceLayer;
    [SerializeField] private LayerMask obstacleLayer; // Layer của vật cản để check pattern có đè lên không
    [SerializeField] private float itemSpacing = 1.0f;
    [SerializeField] private float patternPadding = 1.0f; // Khoảng cách đệm giữa các pattern
    [SerializeField] private float groundPadding = 1.0f;  // Cách mặt đất bao nhiêu

    [SerializeField] private float minGap = 5.0f; // Khoảng cách tối thiểu giữa các lần spawn
    [SerializeField] private float maxGap = 7.0f; // Khoảng cách tối thiểu giữa các lần spawn

    [Space]
    [Tooltip("Xác suất spawn item trên vật cản (%)")]
    [SerializeField, Range(0, 100)] private float obstacleChanceItems = 50f;
    [Tooltip("Xác suất spawn item trên sàn bay (%)")]
    [SerializeField, Range(0, 100)] private float platformChanceItems = 70f;


    // ENUM
    public enum ItemPattern
    {
        Line, Grid, Wave, Diamond, RectHollow,
        RectVertical, RectHorizontal, ShapeVLU, ShapeAPlus,
        Triangle, StairsUp, StairsDown, ZigZag, DoubleLine
    }

    private void Start()
    {
        // 1. Setup toàn bộ pattern mẫu và tính toán bounds trước
        BakePatterns();
    }

    // --- PHẦN 1: GENERATE LOGIC (RUNTIME) ---

    public void GenerateItems(float startX, float endX)
    {
        float currentX = startX + 2f;
        int safetyLoop = 0;

        while (currentX < endX - 2f)
        {
            if (safetyLoop++ > 1000) { Debug.LogWarning("Safety Break!"); break; }

            // 1. Check bề mặt bên dưới
            RaycastHit2D hit = GameUtils.GetSurfaceHit(currentX, surfaceLayer);

            if (hit.collider != null)
            {
                GameObject obj = GameUtils.GetObstacleRoot(hit.collider.transform);

                // --- XỬ LÝ VẬT CẢN (OBSTACLE / PLATFORM) ---
                if (obj.CompareTag("Obstacle") || obj.CompareTag("MiniPlatform"))
                {
                    Bounds obsBounds = GameUtils.GetBounds(obj);

                    // Thử spawn pattern đơn giản trên đỉnh vật cản (nếu muốn)
                    if (RandomUtils.ChancePercent(obstacleChanceItems))
                    {
                        currentX += SpawnOnTop(obsBounds);
                    }
                    else
                    {
                        // Đi tiếp
                        currentX += 2f; // Fallback
                    }
                }
                // --- XỬ LÝ MẶT ĐẤT (GROUND) ---
                else
                {
                    // Lấy ngẫu nhiên 1 pattern mẫu
                    PatternTemplate template = GetRandomTemplate();

                    // Tính vị trí dự kiến (Pivot là Center)
                    float spawnY = hit.point.y + groundPadding + (template.size.y / 2f);
                    Vector2 centerPos = new Vector2(currentX + (template.size.x / 2f), spawnY);

                    // 2. CHECK: Pattern có lọt vừa không? (Không đụng vật cản khác)
                    if (CheckFits(centerPos, template.size))
                    {
                        // Spawn Pattern tại vị trí đã tính
                        SpawnPattern(template, centerPos);

                        // Cập nhật currentX: Nhảy qua hết chiều dài pattern + padding
                        currentX += template.size.x + patternPadding + RandomUtils.RandomWithSteps(minGap, maxGap);
                    }
                    else
                    {
                        // Nếu không vừa, nhích lên một chút rồi thử lại vòng sau
                        currentX += 1.0f;
                    }
                }
            }
            else
            {
                // Không thấy đất (Hố), nhảy qua
                currentX += 2f;
            }
        }
    }

    private bool CheckFits(Vector2 centerPos, Vector2 size)
    {
        // Dùng OverlapBox để xem vùng không gian này có dính Obstacle nào không
        // size * 0.9f để trừ hao một chút tránh va chạm quá gắt
        Collider2D hit = Physics2D.OverlapBox(centerPos, size * 0.9f, 0f, obstacleLayer);
        return hit == null;
    }

    private float SpawnOnTop(Bounds hitBounds)
    {
        // Lấy ngẫu nhiên 1 pattern mẫu
        PatternTemplate template = GetRandomTemplate();

        // Tính vị trí dự kiến (Pivot là Center)
        float spawnY = hitBounds.max.y + groundPadding + (template.size.y / 2f);
        Vector2 centerPos = new Vector2(hitBounds.center.x, spawnY);

        SpawnPattern(template, centerPos);

        return hitBounds.size.x + patternPadding + RandomUtils.RandomWithSteps(minGap, maxGap);
    }

    private void SpawnPattern(PatternTemplate template, Vector2 position)
    {
        // Instantiate cả cụm pattern prefab
        GameObject newPattern = Instantiate(template.prefab, position, Quaternion.identity, itemContainer);
        newPattern.SetActive(true);

        // Thay thế ngẫu nhiên các phần tử
        foreach (Transform child in newPattern.transform)
        {

            GameObject childObj = child.gameObject;
            int randIndex = Random.Range(1, commonItems.Count);
            RandomUtils.ReplaceWithChance(childObj, commonItems[randIndex].prefab, commonItems[randIndex].spawnWeight);

        }

    }

    // --- PHẦN 2: PRE-BAKE LOGIC (SETUP) ---

    private void BakePatterns()
    {
        // 1. Tạo container ẩn để chứa template
        if (patternPoolContainer == null)
        {
            GameObject container = new GameObject("PatternTemplates_Pool");
            container.SetActive(false); // Ẩn đi để không ảnh hưởng game
            patternPoolContainer = container.transform;
        }

        patternTemplates.Clear();

        foreach (ItemPattern p in System.Enum.GetValues(typeof(ItemPattern)))
        {
            // Tạo Object cha tạm thời
            GameObject patternObj = new GameObject($"Template_{p}");
            patternObj.transform.SetParent(patternPoolContainer);
            patternObj.SetActive(false); // Template không active

            // Sinh các điểm Local Points
            List<Vector2> points = Generatepts(p);

            // Tạo các item con tạm thời để tính Bounds
            if (commonItems.Count > 0 && commonItems[0].prefab != null)
            {
                foreach (Vector2 pt in points)
                {
                    GameObject item = Instantiate(commonItems[0].prefab, patternObj.transform);
                    item.transform.localPosition = new Vector3(pt.x * itemSpacing, pt.y * itemSpacing, 0);
                }
            }

            // --- QUAN TRỌNG: CĂN GIỮA (RE-CENTER PIVOT) ---
            Bounds totalBounds = GameUtils.GetBounds(patternObj);
            Vector3 center = totalBounds.center;

            // Dời tất cả con ngược lại để tâm của cha (0,0) trùng với tâm hình học (Center)
            foreach (Transform child in patternObj.transform)
            {
                child.position -= center;
            }

            // Sau khi dời, vị trí của cha hiện tại chính là tâm của Pattern.
            // Reset position cha về 0 cục bộ để dễ quản lý trong pool
            patternObj.transform.localPosition = Vector3.zero;

            // Lưu vào danh sách Template
            PatternTemplate tmpl = new PatternTemplate();
            tmpl.id = p.ToString();
            tmpl.prefab = patternObj;
            tmpl.size = totalBounds.size; // Kích thước thật

            patternTemplates.Add(tmpl);
        }

        Debug.Log($"[ItemGenerator] Baked {patternTemplates.Count} patterns successfully.");
    }

    private List<Vector2> Generatepts(ItemPattern p)
    {
        List<Vector2> pts = new List<Vector2>();
        switch (p)
        {
            case ItemPattern.Line:
                for (int c = 3; c <= 6; c++)
                    for (int i = 0; i < c; i++)
                        pts.Add(new Vector2(i, 0));
                break;
            //---//
            case ItemPattern.Grid:
                for (int x = 0; x < 3; x++)
                    for (int y = 0; y < 3; y++)
                        pts.Add(new Vector2(x, y));
                break;
            //---//

            case ItemPattern.Wave:
                for (int i = 0; i < 8; i++)
                    pts.Add(new Vector2(i, Mathf.Sin(i * 0.8f) * 1.5f + 1.5f));
                break;
            //---//
            case ItemPattern.Diamond:
                pts.Add(new Vector2(1, 2));
                pts.Add(new Vector2(0, 1));
                pts.Add(new Vector2(2, 1));
                pts.Add(new Vector2(1, 0));
                break;
            //---//
            case ItemPattern.RectHollow:
                int rw = 4, rh = 3;
                for (int rx = 0; rx < rw; rx++)
                    for (int ry = 0; ry < rh; ry++)
                        if (rx == 0 || rx == rw - 1 || ry == 0 || ry == rh - 1)
                            pts.Add(new Vector2(rx, ry));
                break;
            //---//
            case ItemPattern.RectVertical:
                int vw = 4;
                int vh = 5;
                for (int vx = 0; vx < vw; vx++)
                    for (int vy = 0; vy < vh; vy++)
                        pts.Add(new Vector2(vx, vy));
                break;
            //---//
            case ItemPattern.RectHorizontal:
                int hw = 6;
                int hh = 4;
                for (int hx = 0; hx < hw; hx++)
                    for (int hy = 0; hy < hh; hy++)
                        pts.Add(new Vector2(hx, hy));
                break;
            //---//
            case ItemPattern.ShapeVLU:
                pts.AddRange(GetTextPoints("V", 0));
                pts.AddRange(GetTextPoints("L", 4));
                pts.AddRange(GetTextPoints("U", 8));
                break;
            //---//
            case ItemPattern.ShapeAPlus:
                pts.AddRange(GetTextPoints("A", 0));
                pts.AddRange(GetTextPoints("+", 4));
                break;
            //---//
            case ItemPattern.Triangle:
                for (int y = 0; y < 3; y++)
                    for (int x = 0; x <= y; x++)
                        pts.Add(new Vector2(y, x));
                break;
            //---//
            case ItemPattern.StairsUp:
                for (int i = 0; i < 5; i++)
                    pts.Add(new Vector2(i, i * 0.5f));
                break;
            //---//
            case ItemPattern.StairsDown:
                for (int i = 0; i < 5; i++)
                    pts.Add(new Vector2(i, 2.5f - (i * 0.5f)));
                break;
            //---//
            case ItemPattern.ZigZag:
                for (int i = 0; i < 6; i++)
                    pts.Add(new Vector2(i, (i % 2 == 0) ? 0 : 1.5f));
                break;
            //---//
            case ItemPattern.DoubleLine:
                for (int i = 0; i < 5; i++)
                {
                    pts.Add(new Vector2(i, 0));
                    pts.Add(new Vector2(i, 1.5f));
                }
                break;
        }
        return pts;
    }

    private List<Vector2> GetTextPoints(string charType, int xOffset)
    {
        List<Vector2> pts = new List<Vector2>();
        switch (charType)
        {
            case "V": pts.Add(new Vector2(0, 2)); pts.Add(new Vector2(2, 2)); pts.Add(new Vector2(0, 1)); pts.Add(new Vector2(2, 1)); pts.Add(new Vector2(1, 0)); break;
            case "L": pts.Add(new Vector2(0, 2)); pts.Add(new Vector2(0, 1)); pts.Add(new Vector2(0, 0)); pts.Add(new Vector2(1, 0)); pts.Add(new Vector2(2, 0)); break;
            case "U": pts.Add(new Vector2(0, 2)); pts.Add(new Vector2(2, 2)); pts.Add(new Vector2(0, 1)); pts.Add(new Vector2(2, 1)); pts.Add(new Vector2(0, 0)); pts.Add(new Vector2(1, 0)); pts.Add(new Vector2(2, 0)); break;
            case "A": pts.Add(new Vector2(1, 2)); pts.Add(new Vector2(0, 1)); pts.Add(new Vector2(2, 1)); pts.Add(new Vector2(1, 1)); pts.Add(new Vector2(0, 0)); pts.Add(new Vector2(2, 0)); break;
            case "+": pts.Add(new Vector2(1, 2)); pts.Add(new Vector2(0, 1)); pts.Add(new Vector2(1, 1)); pts.Add(new Vector2(2, 1)); pts.Add(new Vector2(1, 0)); break;
        }
        for (int i = 0; i < pts.Count; i++) pts[i] = new Vector2(pts[i].x + xOffset, pts[i].y);
        return pts;
    }

    private PatternTemplate GetRandomTemplate()
    {
        if (patternTemplates.Count == 0) return null;
        return patternTemplates[Random.Range(0, patternTemplates.Count)];
    }

#if UNITY_EDITOR
    // Vẽ Gizmos để debug xem vùng check có đúng không
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        // Logic vẽ gizmos debug nếu cần
    }
#endif
}
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.SceneManagement; // Cần để load scene khi nhấn GO

public class MapSelectionController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Kéo GameObject 'MapsContent' chứa các map con vào đây")]
    public RectTransform mapsContent;
    [Tooltip("Nút mũi tên trái")]
    public Button leftArrowBtn;
    [Tooltip("Nút mũi tên phải")]
    public Button rightArrowBtn;

    [Header("Settings")]
    [Tooltip("Khoảng cách giữa tâm map này đến tâm map kia (Bạn cần đo trong Scene)")]
    public float mapSpacing = 600f;
    [Tooltip("Thời gian di chuyển chuyển đổi map")]
    public float moveDuration = 0.4f;
    [Tooltip("Kiểu chuyển động (OutBack tạo cảm giác nảy nhẹ)")]
    public Ease moveEase = Ease.OutBack;

    [Header("Swipe Settings")]
    [Tooltip("Khoảng cách tối thiểu để tính là một cú vuốt (swipe)")]
    public float swipeThreshold = 50f;

    private int currentIndex = 0;
    private int totalMaps = 0;
    private List<RectTransform> mapRects = new List<RectTransform>();

    // Biến cho xử lý swipe
    private Vector2 startTouchPos;
    private Vector2 endTouchPos;
    private bool isSwiping = false;

    private void Start()
    {
        InitMaps();

        // Đăng ký sự kiện click cho các nút
        leftArrowBtn.onClick.AddListener(NavigateLeft);
        rightArrowBtn.onClick.AddListener(NavigateRight);

        // Cập nhật trạng thái ban đầu (ở map đầu tiên) ngay lập tức không cần hiệu ứng
        UpdateSelection(0, true);
    }

    // Tìm và đếm số lượng map con
    private void InitMaps()
    {
        totalMaps = mapsContent.childCount;
        for (int i = 0; i < totalMaps; i++)
        {
            // Lấy RectTransform của từng map con để sau này có thể thêm hiệu ứng scale nếu muốn
            mapRects.Add(mapsContent.GetChild(i) as RectTransform);
        }

        if (totalMaps == 0)
        {
            Debug.LogError("[MapSelectionController] Không tìm thấy map con nào trong MapsContent!");
        }
    }

    private void Update()
    {
        HandleSwipeInput();
    }

    // --- XỬ LÝ VUỐT (SWIPE) ---
    private void HandleSwipeInput()
    {
        // Chỉ nhận input khi không có tween nào đang chạy trên content để tránh lỗi
        if (DOTween.IsTweening(mapsContent)) return;

        // Bắt đầu chạm
        if (Input.GetMouseButtonDown(0))
        {
            isSwiping = true;
            startTouchPos = Input.mousePosition;
        }
        // Kết thúc chạm (thả tay ra)
        else if (Input.GetMouseButtonUp(0) && isSwiping)
        {
            isSwiping = false;
            endTouchPos = Input.mousePosition;
            DetectSwipeDirection();
        }
    }

    private void DetectSwipeDirection()
    {
        // Tính khoảng cách và hướng vuốt
        Vector2 swipeDelta = endTouchPos - startTouchPos;

        // Chỉ xử lý nếu khoảng cách vuốt đủ lớn (tránh nhầm với click)
        if (Mathf.Abs(swipeDelta.x) >= swipeThreshold)
        {
            // Vuốt sang phải (Delta X dương) -> Muốn về map bên trái (Previous)
            if (swipeDelta.x > 0)
            {
                NavigateLeft();
            }
            // Vuốt sang trái (Delta X âm) -> Muốn đến map bên phải (Next)
            else
            {
                NavigateRight();
            }
        }
    }

    // --- CÁC HÀM ĐIỀU HƯỚNG ---
    // Gọi khi bấm nút trái hoặc vuốt phải
    public void NavigateLeft()
    {
        if (currentIndex > 0)
        {
            UpdateSelection(currentIndex - 1);
        }
    }

    // Gọi khi bấm nút phải hoặc vuốt trái
    public void NavigateRight()
    {
        if (currentIndex < totalMaps - 1)
        {
            UpdateSelection(currentIndex + 1);
        }
    }

    // --- HÀM CẬP NHẬT TRUNG TÂM ---
    private void UpdateSelection(int newIndex, bool immediate = false)
    {
        currentIndex = newIndex;

        // 1. Xử lý ẩn/hiện nút mũi tên
        // Nếu ở map đầu (index 0) -> tắt nút trái
        leftArrowBtn.gameObject.SetActive(currentIndex > 0);
        // Nếu ở map cuối (index = tổng - 1) -> tắt nút phải
        rightArrowBtn.gameObject.SetActive(currentIndex < totalMaps - 1);

        // 2. Tính toán vị trí cần di chuyển đến cho MapsContent
        // Để map tại newIndex nằm giữa, ta cần dịch content sang trái một đoạn
        float targetPosX = -(currentIndex * mapSpacing);

        // Dừng tween cũ nếu có
        mapsContent.DOKill();

        if (immediate)
        {
            // Di chuyển ngay lập tức (dùng khi khởi tạo)
            mapsContent.anchoredPosition = new Vector2(targetPosX, mapsContent.anchoredPosition.y);
        }
        else
        {
            // Di chuyển mượt mà bằng DOTween
            mapsContent.DOAnchorPosX(targetPosX, moveDuration).SetEase(moveEase);
        }

        UIManager.Instance.SelectedMap(currentIndex);
    }

    // --- TÍCH HỢP NÚT GO ---
    // Gán hàm này vào sự kiện OnClick của nút GO
    public void OnGoButtonClick()
    {
        Debug.Log($"[GO] Bắt đầu vào map số: {currentIndex}");

        // --- Ví dụ logic load map ---
        // Cách 1: Load scene theo tên định sẵn
        // string sceneName = "Map_Level_" + (currentIndex + 1); 
        // SceneManager.LoadScene(sceneName);

        // Cách 2: Lưu index map đã chọn vào Manager rồi chuyển scene
        // if (ReferenceManager.Instance != null) {
        //     ReferenceManager.Instance.SelectedMapIndex = currentIndex;
        //     SceneManager.LoadScene("GameplayScene");
        // }

        // Tạm thời log ra console
        Debug.Log("Logic load scene cần được cài đặt tại đây.");
    }
}
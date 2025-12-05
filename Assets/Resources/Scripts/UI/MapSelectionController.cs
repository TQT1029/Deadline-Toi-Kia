using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

public class MapSelectionUI : MonoBehaviour
{
    [Header("References")]
    public RectTransform mapsContent;
    public Button leftArrowBtn;
    public Button rightArrowBtn;
    public Button goButton; // Thêm tham chiếu nút Go

    [Header("Settings")]
    public float mapSpacing = 600f;
    public float moveDuration = 0.4f;
    public Ease moveEase = Ease.OutBack;
    public float swipeThreshold = 50f;

    private int currentIndex = 0;
    private int totalMaps = 0;

    // Swipe Logic
    private Vector2 startTouchPos;
    private bool isSwiping = false;

    private void Start()
    {
        InitMaps();
        leftArrowBtn.onClick.AddListener(NavigateLeft);
        rightArrowBtn.onClick.AddListener(NavigateRight);

        // Nếu có script SceneLoader trên nút GO, có thể không cần add listener ở đây
        // Nhưng logic chọn map phải được cập nhật
        UpdateSelection(0, true);
    }

    private void InitMaps()
    {
        totalMaps = mapsContent.childCount;
        if (totalMaps == 0) Debug.LogError("MapsContent is empty!");
    }

    private void Update() => HandleSwipeInput();

    private void HandleSwipeInput()
    {
        if (DOTween.IsTweening(mapsContent)) return;

        if (Input.GetMouseButtonDown(0))
        {
            isSwiping = true;
            startTouchPos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0) && isSwiping)
        {
            isSwiping = false;
            float diff = Input.mousePosition.x - startTouchPos.x;

            if (Mathf.Abs(diff) >= swipeThreshold)
            {
                if (diff > 0) NavigateLeft();
                else NavigateRight();
            }
        }
    }

    public void NavigateLeft()
    {
        if (currentIndex > 0) UpdateSelection(currentIndex - 1);
    }

    public void NavigateRight()
    {
        if (currentIndex < totalMaps - 1) UpdateSelection(currentIndex + 1);
    }

    private void UpdateSelection(int newIndex, bool immediate = false)
    {
        currentIndex = newIndex;

        // UI Arrows
        leftArrowBtn.gameObject.SetActive(currentIndex > 0);
        rightArrowBtn.gameObject.SetActive(currentIndex < totalMaps - 1);

        // Movement
        float targetX = -(currentIndex * mapSpacing);
        mapsContent.DOKill();

        if (immediate) mapsContent.anchoredPosition = new Vector2(targetX, mapsContent.anchoredPosition.y);
        else mapsContent.DOAnchorPosX(targetX, moveDuration).SetEase(moveEase);

        // Thông báo UIManager
        if (UIManager.Instance)
        {
            UIManager.Instance.SelectMapByIndex(currentIndex);
        }
    }
}
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class AutoNativeSizeAnimator : MonoBehaviour
{
    private Image _targetImage;
    private Sprite _lastSprite;

    private void Awake()
    {
        _targetImage = GetComponent<Image>();
    }

    // Dùng LateUpdate để đảm bảo Animator đã cập nhật xong frame hiện tại
    private void LateUpdate()
    {
        if (_targetImage == null || _targetImage.sprite == null) return;

        // Chỉ xử lý khi Sprite thực sự thay đổi để tối ưu hiệu năng
        // (Tránh việc Build lại Layout Canvas liên tục mỗi frame)
        if (_targetImage.sprite != _lastSprite)
        {
            UpdateSize();
            _lastSprite = _targetImage.sprite;
        }
    }

    private void UpdateSize()
    {
        _targetImage.SetNativeSize();

        // Mẹo nhỏ: Đôi khi NativeSize quá to hoặc quá nhỏ so với UI
        // Bạn có thể chỉnh Scale của GameObject này (ví dụ 0.5, 0.5, 1) trong Editor
        // Script này chỉ thay đổi width/height (RectTransform), không đổi Scale.
    }
}
using UnityEngine;
using UnityEngine.UI;

public enum SettingType
{
    Sound,
    Vibration,
    FPS60
}

[RequireComponent(typeof(Toggle))]
public class SettingsToggle : MonoBehaviour
{
    [Header("Cài đặt loại nút")]
    public SettingType settingType;

    [Header("UI References")]
    [Tooltip("Ảnh icon cần thay đổi")]
    public Image targetImage;
    public Sprite onSprite;  // Ảnh khi BẬT (vd: Loa có sóng)
    public Sprite offSprite; // Ảnh khi TẮT (vd: Loa gạch chéo)

    private bool isOn;

    private void Awake()
    {
        isOn = GetComponent<Toggle>().isOn;
        Debug.Log($"[SettingToggle] {settingType} - Initial isOn: {isOn}");

        targetImage.sprite = isOn ? onSprite : offSprite;
    }
    private void Start()
    {
        // Load trạng thái đã lưu, mặc định là 1 (Bật)
        // PlayerPrefs: 1 = True, 0 = False
        isOn = PlayerPrefs.GetInt(settingType.ToString(), 1) == 1;

        ApplySetting(); // Áp dụng ngay khi game bắt đầu
        UpdateSprite(); // Cập nhật hình ảnh
    }

    // Gán hàm này vào sự kiện OnClick của Button
    public void OnToggleClick()
    {
        isOn = !isOn; // Đảo ngược trạng thái

        // Lưu trạng thái
        PlayerPrefs.SetInt(settingType.ToString(), isOn ? 1 : 0);
        PlayerPrefs.Save();

        ApplySetting();
        UpdateSprite();
    }

    private void ApplySetting()
    {
        switch (settingType)
        {
            case SettingType.Sound:
                // Logic tắt bật âm thanh toàn cục
                AudioListener.volume = isOn ? 1f : 0f;
                Debug.Log($"Sound: {isOn}");
                break;

            case SettingType.Vibration:
                // Unity không có biến global cho rung, ta chỉ lưu flag để logic khác check
                // Ví dụ khi va chạm: if(PlayerPrefs.GetInt("Vibration") == 1) Handheld.Vibrate();
                Debug.Log($"Vibration: {isOn}");
                break;

            case SettingType.FPS60:
                Application.targetFrameRate = isOn ? 60 : 30;
                Debug.Log($"Target FPS: {(isOn ? 60 : 30)}");
                break;
        }
    }

    private void UpdateSprite()
    {
        if (targetImage != null)
        {
            targetImage.sprite = isOn ? onSprite : offSprite;
        }
    }
}
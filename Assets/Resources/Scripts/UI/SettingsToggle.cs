using UnityEngine;
using UnityEngine.UI;

public enum SettingType { Sound, Vibration, FPS60 }

public class SettingsToggle : MonoBehaviour
{
    public SettingType settingType;
    public Image targetImage;
    public Sprite onSprite;
    public Sprite offSprite;

    private bool isOn;

    private void Start()
    {
        // Load prefs
        string key = settingType.ToString();
        isOn = PlayerPrefs.GetInt(key, 1) == 1;

        ApplySetting();
        UpdateSprite();
    }

    public void OnToggleClick()
    {
        isOn = !isOn;
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
                AudioListener.volume = isOn ? 1f : 0f;
                break;
            case SettingType.FPS60:
                Application.targetFrameRate = isOn ? 60 : 30;
                break;
            case SettingType.Vibration:
                // Logic rung xử lý ở nơi khác dựa trên Prefs
                break;
        }
    }

    private void UpdateSprite()
    {
        if (targetImage) targetImage.sprite = isOn ? onSprite : offSprite;
    }
}
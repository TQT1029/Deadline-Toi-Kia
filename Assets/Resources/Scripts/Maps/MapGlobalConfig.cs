using UnityEngine;

[RequireComponent(typeof(EndlessGameController))]
public class MapGlobalConfig : MonoBehaviour
{
    public static MapGlobalConfig Instance;
    private void Awake() => Instance = this;

    [Header("Coordinate Settings (Dùng chung)")]
    public float groundY = -5f;      // Độ cao mặt đất
    public float pitY = -7f;         // Độ sâu đáy hố
    public float maxHeightMap = 15f; // Độ cao tối đa của map (để giới hạn sàn bay)

    [Header("Global Logic Settings")]
    [Range(0, 100)] public int pitChance = 30; // Tỉ lệ xuất hiện hố
    public bool hasPit = true;                  // Có cho phép tạo hố không

    [Header("Global Noise Settings")]
    [Tooltip("Càng nhỏ càng thoải, càng lớn càng dóc.")]
    public float waveFrequency = 0.4f;

    public void ApplyConfig(MapProfile profile)
    {
        if (profile == null) return;
        groundY = profile.groundY;
        pitY = profile.pitY;
        maxHeightMap = profile.maxHeightMap;
        hasPit = profile.hasPit;
        pitChance = profile.pitChance;
        waveFrequency = profile.waveFrequency;
    }
}
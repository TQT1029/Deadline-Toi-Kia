using UnityEngine;
using System.Collections.Generic;

public class AudioManager : Singleton<AudioManager>
{

    [Header("Audio Sources (Tự tạo nếu chưa có)")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Library")]
    [SerializeField] private List<SoundProfile> musicLibrary;
    [SerializeField] private List<SoundProfile> sfxLibrary;

    // Khai báo 2 Controller thuần C#
    private MusicController _musicController;
    private SFXController _sfxController;

    private Dictionary<string, SoundProfile> _musicDict;
    private Dictionary<string, SoundProfile> _sfxDict;

    protected override void OnAwake()
    {
        // Gọi hàm khởi tạo
        Initialize();
    }
    private void Initialize()
    {
        // 1. Tự tạo AudioSource nếu quên kéo
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        // 2. Khởi tạo Controller thuần C#
        _musicController = new MusicController(musicSource);
        _sfxController = new SFXController(sfxSource);

        // 3. Chuyển List sang Dictionary để tra cứu cho nhanh
        _musicDict = new Dictionary<string, SoundProfile>();
        _sfxDict = new Dictionary<string, SoundProfile>();

        foreach (var m in musicLibrary) if (!_musicDict.ContainsKey(m.id)) _musicDict.Add(m.id, m);
        foreach (var s in sfxLibrary) if (!_sfxDict.ContainsKey(s.id)) _sfxDict.Add(s.id, s);

        // 4. Load Settings cũ (nếu có)
        LoadVolumeSettings();
    }

    // --- PUBLIC METHODS (GỌI TỪ BÊN NGOÀI) ---

    public void PlayMusic(string id)
    {
        if (_musicDict.TryGetValue(id, out SoundProfile profile))
        {
            _musicController.PlayMusic(profile.clip);
        }
        else Debug.LogWarning($"Music ID not found: {id}");
    }

    public void PlaySFX(string id)
    {
        if (_sfxDict.TryGetValue(id, out SoundProfile profile))
        {
            // Truyền cả volume và pitch riêng của từng clip vào controller
            _sfxController.PlaySFX(profile.clip, profile.volume, profile.pitch);
        }
        else Debug.LogWarning($"SFX ID not found: {id}");
    }

    public void StopMusic() => _musicController.StopMusic();

    // --- SETTINGS (DÙNG CHO UI SLIDER) ---

    public void SetMusicVolume(float value)
    {
        _musicController.SetGlobalVolume(value);
        PlayerPrefs.SetFloat("MusicVol", value);
    }

    public void SetSFXVolume(float value)
    {
        _sfxController.SetGlobalVolume(value);
        PlayerPrefs.SetFloat("SFXVol", value);
    }

    private void LoadVolumeSettings()
    {
        SetMusicVolume(PlayerPrefs.GetFloat("MusicVol", 1f));
        SetSFXVolume(PlayerPrefs.GetFloat("SFXVol", 1f));
    }
}
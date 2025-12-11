using UnityEngine;

[System.Serializable]
public class SoundProfile
{
    public string id; // Tên định danh (vd: "Jump", "BGM_Main")
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}
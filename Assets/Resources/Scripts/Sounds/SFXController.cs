using UnityEngine;

[System.Serializable]
public class SFXController
{
    private AudioSource _audioSource;
    private float _globalVolume = 1f;

    // Constructor
    public SFXController(AudioSource source)
    {
        _audioSource = source;
        _audioSource.loop = false; // SFX không lặp
    }

    public void SetGlobalVolume(float vol)
    {
        _globalVolume = vol;
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        // PlayOneShot cho phép chồng âm thanh lên nhau (không bị ngắt quãng)
        _audioSource.pitch = pitch;
        _audioSource.PlayOneShot(clip, _globalVolume * volumeScale);
    }
}
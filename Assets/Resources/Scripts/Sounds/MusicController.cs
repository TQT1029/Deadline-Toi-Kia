using UnityEngine;
using DG.Tweening; // Cần DOTween để Fade nhạc

[System.Serializable]
public class MusicController
{
    private AudioSource _audioSource;
    private float _globalVolume = 1f;

    // Constructor: Nhận AudioSource từ AudioManager
    public MusicController(AudioSource source)
    {
        _audioSource = source;
        _audioSource.loop = true; // Nhạc nền luôn lặp
    }

    public void SetGlobalVolume(float vol)
    {
        _globalVolume = vol;
        _audioSource.volume = vol;
    }

    public void PlayMusic(AudioClip clip, bool fade = true)
    {
        if (clip == null) return;

        // Nếu đang phát đúng bài này rồi thì thôi
        if (_audioSource.clip == clip && _audioSource.isPlaying) return;

        if (fade)
        {
            // Logic Fade: Giảm âm lượng về 0 -> Đổi bài -> Tăng lại
            _audioSource.DOFade(0f, 0.5f).OnComplete(() =>
            {
                _audioSource.clip = clip;
                _audioSource.Play();
                _audioSource.DOFade(_globalVolume, 0.5f);
            });
        }
        else
        {
            // Chuyển bài gắt (không fade)
            _audioSource.volume = _globalVolume;
            _audioSource.clip = clip;
            _audioSource.Play();
        }
    }

    public void StopMusic(bool fade = true)
    {
        if (fade)
        {
            _audioSource.DOFade(0f, 0.5f).OnComplete(() => _audioSource.Stop());
        }
        else
        {
            _audioSource.Stop();
        }
    }
}
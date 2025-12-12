using UnityEngine;
using DG.Tweening; // Cần DOTween

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
        // Nếu đang không fade thì cập nhật ngay, còn đang fade thì để tween tự xử lý
        if (!DOTween.IsTweening(_audioSource))
        {
            _audioSource.volume = vol;
        }
    }

    public void PlayMusic(AudioClip clip, bool fade = true)
    {
        if (clip == null) return;

        // Nếu đang phát đúng bài này rồi thì thôi, không làm gì cả
        if (_audioSource.clip == clip && _audioSource.isPlaying) return;

        // Ngắt tất cả các tween cũ đang chạy trên AudioSource này (tránh xung đột)
        _audioSource.DOKill();

        if (fade)
        {
            // TRƯỜNG HỢP 1: Đang có nhạc chạy -> Cần Fade Out bài cũ trước
            if (_audioSource.isPlaying && _audioSource.volume > 0)
            {
                // Giảm volume về 0 trong 0.5s -> Đổi bài -> Tăng lại
                _audioSource.DOFade(0f, 0.5f).SetUpdate(true).OnComplete(() =>
                {
                    _audioSource.clip = clip;
                    _audioSource.Play();
                    _audioSource.DOFade(_globalVolume, 0.5f).SetUpdate(true);
                });
            }
            // TRƯỜNG HỢP 2: Chưa có nhạc (Mới vào game) -> Fade In luôn cho nhanh
            else
            {
                _audioSource.clip = clip;
                _audioSource.volume = 0f; // Set về 0 ngay lập tức
                _audioSource.Play();
                // Tăng dần lên volume gốc
                _audioSource.DOFade(_globalVolume, 0.8f).SetUpdate(true);
            }
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
        _audioSource.DOKill(); // Ngắt tween cũ

        if (fade && _audioSource.isPlaying)
        {
            // Fade Out rồi mới Stop
            _audioSource.DOFade(0f, 0.5f).SetUpdate(true).OnComplete(() =>
            {
                _audioSource.Stop();
            });
        }
        else
        {
            _audioSource.Stop();
        }
    }
}
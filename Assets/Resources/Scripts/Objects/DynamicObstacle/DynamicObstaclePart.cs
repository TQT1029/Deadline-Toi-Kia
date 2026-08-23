using UnityEngine;
using DG.Tweening;

namespace ProObstacleEngine
{
    public class DynamicObstaclePart : MonoBehaviour
    {
        private Vector3 initialRotation = Vector3.zero;

        private Tween _moveTween;
        private Tween _scaleTween;
        private Tween _rotateTween;
        private Tween _colorTween;
        private Tween _shakeTween;

        private Vector3 _startLocalPos;
        private Vector3 _startLocalScale;
        private Vector3 _startLocalEuler;
        private bool _isCached = false;

        private Renderer _renderer;
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            CacheInitialTransform();
            _renderer = GetComponent<Renderer>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Ghi nhớ trạng thái ban đầu để khi tính toán toạ độ mới không bị sai lệch (drift).
        /// </summary>
        public void CacheInitialTransform()
        {
            if (_isCached) return;
            _startLocalPos = transform.localPosition;
            _startLocalScale = transform.localScale;
            _startLocalEuler = transform.localEulerAngles;
            _isCached = true;
        }

        public void StartMove(Vector3 offset, float duration, Ease easeType, float delay, LoopType loopType)
        {
            transform.localPosition = _startLocalPos;
            _moveTween = transform.DOLocalMove(_startLocalPos + offset, duration)
                .SetEase(easeType)
                .SetLoops(-1, loopType)
                .SetDelay(delay);
        }

        public void StartScale(Vector3 scaleMultiplier, float duration, Ease easeType, float delay, LoopType loopType)
        {
            transform.localScale = _startLocalScale;
            Vector3 targetScale = Vector3.Scale(_startLocalScale, scaleMultiplier);
            _scaleTween = transform.DOScale(targetScale, duration)
                .SetEase(easeType)
                .SetLoops(-1, loopType)
                .SetDelay(delay);
        }

        public void StartRotate(Vector3 rotationAngles, float duration, Ease easeType, float delay, bool continuousSpin, LoopType loopType, bool symmetricRotation = false)
        {
            transform.localEulerAngles = _startLocalEuler;

            if (continuousSpin)
            {
                _rotateTween = transform.DOLocalRotate(rotationAngles, duration, RotateMode.FastBeyond360)
                    .SetRelative(true)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Incremental)
                    .SetDelay(delay);
            }
            else if (symmetricRotation)
            {
                Sequence seq = DOTween.Sequence();
                seq.Append(transform.DOLocalRotate(initialRotation + rotationAngles, duration / 2f).SetEase(easeType));
                seq.Append(transform.DOLocalRotate(initialRotation - rotationAngles, duration).SetEase(easeType));
                seq.Append(transform.DOLocalRotate(initialRotation, duration / 2f).SetEase(easeType));
                seq.SetDelay(delay);
                seq.SetLoops(-1, LoopType.Restart);

                _rotateTween = seq;
            }
            else
            {
                _rotateTween = transform.DOLocalRotate(_startLocalEuler + rotationAngles, duration)
                    .SetEase(easeType)
                    .SetLoops(-1, loopType)
                    .SetDelay(delay);
            }
        }

        public void StartColorWait(Color targetColor, float duration, Ease easeType, float delay, LoopType loopType)
        {
            if (_spriteRenderer != null)
            {
                _colorTween = _spriteRenderer.DOColor(targetColor, duration)
                    .SetEase(easeType)
                    .SetLoops(-1, loopType)
                    .SetDelay(delay);
            }
        }

        public void StartShake(Vector3 strength, float duration, float delay)
        {
            transform.localPosition = _startLocalPos;
            _shakeTween = transform.DOShakePosition(duration, strength, vibrato: 10, randomness: 90)
                .SetLoops(-1, LoopType.Restart)
                .SetDelay(delay);
        }

        public void StopAllMotions()
        {
            _moveTween?.Kill();
            _scaleTween?.Kill();
            _rotateTween?.Kill();
            _colorTween?.Kill();
            _shakeTween?.Kill();

            transform.DOKill();
            if (_spriteRenderer != null) _spriteRenderer.DOKill();
        }

        private void OnDisable() => StopAllMotions();
        private void OnDestroy() => StopAllMotions();
    }
}
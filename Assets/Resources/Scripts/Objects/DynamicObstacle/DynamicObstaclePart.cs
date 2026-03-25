using UnityEngine;
using DG.Tweening;

namespace ProObstacleEngine
{
    public class DynamicObstaclePart : MonoBehaviour
    {
        private Tween _moveTween;
        private Tween _scaleTween;
        private Tween _rotateTween;

        public void StartMove(Vector3 offset, float duration, Ease easeType, float delay)
        {
            _moveTween?.Kill();
            _moveTween = transform.DOLocalMove(transform.localPosition + offset, duration)
                .SetEase(easeType)
                .SetLoops(-1, LoopType.Yoyo)
                .SetDelay(delay);
        }

        public void StartScale(Vector3 targetScale, float duration, Ease easeType, float delay)
        {
            _scaleTween?.Kill();
            _scaleTween = transform.DOScale(targetScale, duration)
                .SetEase(easeType)
                .SetLoops(-1, LoopType.Yoyo)
                .SetDelay(delay);
        }

        public void StartRotate(Vector3 rotationAngles, float duration, Ease easeType, float delay, bool continuousSpin)
        {
            _rotateTween?.Kill();

            if (continuousSpin)
            {
                // Xoay tròn liên tục 360 độ (bỏ qua Ease để xoay đều)
                _rotateTween = transform.DOLocalRotate(rotationAngles, duration, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Restart)
                    .SetDelay(delay);
            }
            else
            {
                // Xoay lắc lư qua lại (YoYo)
                _rotateTween = transform.DOLocalRotate(rotationAngles, duration)
                    .SetEase(easeType)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetDelay(delay);
            }
        }

        public void StopAllMotions()
        {
            _moveTween?.Kill();
            _scaleTween?.Kill();
            _rotateTween?.Kill();
        }

        private void OnDisable() => StopAllMotions();
        private void OnDestroy() => StopAllMotions();
    }
}
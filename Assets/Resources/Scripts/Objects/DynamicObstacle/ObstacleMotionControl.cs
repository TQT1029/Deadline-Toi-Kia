using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

namespace ProObstacleEngine
{
    /// <summary>
    /// Các kiểu phối hợp chuyển động giữa các bộ phận của chướng ngại vật.
    /// </summary>
    public enum GroupPattern
    {
        [Tooltip("Tất cả chuyển động cùng lúc")] Sync,
        [Tooltip("Chuyển động nối tiếp nhau tạo thành làn sóng")] Wave,
        [Tooltip("Chuyển động so le (Chẵn/Lẻ)")] Alternating,
        [Tooltip("Tỏa từ giữa ra 2 biên")] CenterOut,
        [Tooltip("Dồn từ 2 biên vào giữa")] EndsToCenter,
        [Tooltip("Độ trễ hoàn toàn ngẫu nhiên")] RandomDelay
    }

    public class ObstacleMotionControl : MonoBehaviour
    {
        [Header("--- GENERAL SETTINGS ---")]
        [Tooltip("Kiểu phối hợp chuyển động giữa các khối con.")]
        public GroupPattern groupPattern = GroupPattern.Sync;

        [Tooltip("Kiểu lặp lại của Animation.")]
        public LoopType loopType = LoopType.Yoyo;

        [Tooltip("Thời gian hoàn thành một chu kỳ chuyển động (giây).")]
        public float duration = 1.0f;

        [Tooltip("Đường cong gia tốc chuyển động (Vd: InOutSine làm mượt ở 2 đầu).")]
        public Ease easeType = Ease.InOutSine;

        [Tooltip("Độ trễ giữa từng khối con (Chỉ dùng cho các Pattern như Wave, CenterOut...).")]
        public float waveDelayStep = 0.2f;

        [Header("--- MOVEMENT ---")]
        public bool enableMove = false;
        [Tooltip("Khoảng cách di chuyển so với vị trí ban đầu.")]
        public Vector3 moveOffset = Vector3.up;

        [Header("--- ROTATION ---")]
        public bool enableRotate = false;
        [Tooltip("Góc xoay mục tiêu.")]
        public Vector3 rotateAngles = new Vector3(0, 0, 90f);
        [Tooltip("Nếu bật, vật thể sẽ xoay vòng tròn 360 độ liên tục thay vì lắc lư.")]
        public bool continuousSpin = false;

        [Header("--- SCALE ---")]
        public bool enableScale = false;
        [Tooltip("Hệ số phóng to/thu nhỏ (1 là giữ nguyên).")]
        public Vector3 scaleMultiplier = new Vector3(1.2f, 1.2f, 1.2f);

        [Header("--- COLOR & FADE ---")]
        public bool enableColor = false;
        [Tooltip("Màu sắc mục tiêu vật thể sẽ nhấp nháy tới.")]
        public Color targetColor = Color.red;

        [Header("--- SHAKE ---")]
        public bool enableShake = false;
        [Tooltip("Độ mạnh của lực rung lắc (Vector3)")]
        public Vector3 shakeStrength = new Vector3(0.2f, 0.2f, 0);

        private List<DynamicObstaclePart> _parts = new List<DynamicObstaclePart>();

        private void Start()
        {
            SetupParts();
            ApplyMotion();
        }

        [ContextMenu("Fetch Parts")]
        public void SetupParts()
        {
            _parts.Clear();
            foreach (Transform child in transform)
            {
                if (!child.gameObject.activeSelf) continue;

                if (!child.TryGetComponent(out DynamicObstaclePart part))
                {
                    part = child.gameObject.AddComponent<DynamicObstaclePart>();
                }

                // Lưu lại vị trí ban đầu để tránh lỗi trôi vị trí khi Editor update liên tục
                part.CacheInitialTransform();
                _parts.Add(part);
            }
        }

        [ContextMenu("Preview Motion (Play Mode Only)")]
        public void ApplyMotion()
        {
            if (!Application.isPlaying) return;
            if (_parts.Count == 0) SetupParts();

            for (int i = 0; i < _parts.Count; i++)
            {
                DynamicObstaclePart part = _parts[i];
                float delay = CalculateDelay(i, _parts.Count);

                part.StopAllMotions();

                if (enableMove)
                    part.StartMove(moveOffset, duration, easeType, delay, loopType);

                if (enableScale)
                    part.StartScale(scaleMultiplier, duration, easeType, delay, loopType);

                if (enableRotate)
                    part.StartRotate(rotateAngles, duration, easeType, delay, continuousSpin, loopType);

                if (enableColor)
                    part.StartColorWait(targetColor, duration, easeType, delay, loopType);

                if (enableShake)
                    part.StartShake(shakeStrength, duration, delay);
            }
        }

        private float CalculateDelay(int index, int totalCount)
        {
            switch (groupPattern)
            {
                case GroupPattern.Sync: return 0f;
                case GroupPattern.Wave: return index * waveDelayStep;
                case GroupPattern.Alternating: return (index % 2 == 0) ? 0f : duration;
                case GroupPattern.CenterOut:
                    float centerIndex = (totalCount - 1) / 2f;
                    return Mathf.Abs(index - centerIndex) * waveDelayStep;
                case GroupPattern.EndsToCenter:
                    float centerIdx = (totalCount - 1) / 2f;
                    float maxDist = totalCount / 2f;
                    return (maxDist - Mathf.Abs(index - centerIdx)) * waveDelayStep;
                case GroupPattern.RandomDelay:
                    return UnityEngine.Random.Range(0f, waveDelayStep * totalCount);
                default: return 0f;
            }
        }
    }
}
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

namespace ProObstacleEngine
{
    public enum GroupPattern
    {
        Sync,         // Cùng lúc
        Wave,         // Lượn sóng
        Alternating,  // So le (Chẵn/Lẻ)
        CenterOut,    // Tỏa từ giữa ra 2 biên
        RandomDelay   // Trễ ngẫu nhiên
    }

    public class ObstacleMotionControl : MonoBehaviour
    {
        [Header("General Settings")]
        public GroupPattern groupPattern = GroupPattern.Sync;
        public float duration = 1.0f;
        public Ease easeType = Ease.InOutSine;
        public float waveDelayStep = 0.2f;

        [Header("Movement")]
        public bool enableMove = false;
        public Vector3 moveOffset = Vector3.up;

        [Header("Rotation")]
        public bool enableRotate = false;
        public Vector3 rotateAngles = new Vector3(0, 0, 90f);
        public bool continuousSpin = false;

        [Header("Scale")]
        public bool enableScale = false;
        public Vector3 scaleMultiplier = new Vector3(1.2f, 1.2f, 1.2f);

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
                    part.StartMove(moveOffset, duration, easeType, delay);

                if (enableScale)
                {
                    Vector3 targetScale = Vector3.Scale(part.transform.localScale, scaleMultiplier);
                    part.StartScale(targetScale, duration, easeType, delay);
                }

                if (enableRotate)
                    part.StartRotate(rotateAngles, duration, easeType, delay, continuousSpin);
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
                case GroupPattern.RandomDelay:
                    return UnityEngine.Random.Range(0f, waveDelayStep * totalCount);
                default: return 0f;
            }
        }
    }
}
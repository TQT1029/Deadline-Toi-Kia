using Unity.Cinemachine;
using UnityEngine;

namespace ParallaxEngine
{
    public class ParallaxScaler : MonoBehaviour
    {
        [Header("Camera Integration")]
        [Tooltip("Cinemachine Camera (Tùy chọn). Nếu không dùng Cinemachine, hãy bỏ trống.")]
        [SerializeField] private CinemachineCamera vCam;

        [Tooltip("Camera mặc định. Tự động lấy Camera.main nếu vCam bị bỏ trống.")]
        [SerializeField] private Camera standardCamera;

        [Tooltip("Bật để scale đều trên cả 2 trục X và Y nhằm giữ nguyên tỉ lệ ảnh.")]
        [SerializeField] private bool maintainAspectRatio = true;

        private Vector3 _initialScale;
        private float _initialOrthoSize;

        private void Start()
        {
            // Bổ sung logic Fallback: Dùng standard camera nếu không gán Cinemachine
            if (vCam == null && standardCamera == null)
            {
                standardCamera = Camera.main;
            }

            if (vCam == null && standardCamera == null)
            {
                Debug.LogWarning("[ParallaxScaler] Không tìm thấy Camera nào để tham chiếu scale!", this);
                return;
            }

            _initialScale = transform.localScale;
            _initialOrthoSize = GetCurrentOrthoSize();
        }

        private void LateUpdate()
        {
            float currentOrthoSize = GetCurrentOrthoSize();
            if (currentOrthoSize == 0 || _initialOrthoSize == 0) return;

            float zoomRatio = currentOrthoSize / _initialOrthoSize;

            if (maintainAspectRatio)
            {
                transform.localScale = _initialScale * zoomRatio;
            }
            else
            {
                transform.localScale = new Vector3(_initialScale.x, _initialScale.y * zoomRatio, _initialScale.z);
            }
        }

        // Tách hàm để lấy Orthographic Size linh hoạt giữa 2 hệ thống Camera
        private float GetCurrentOrthoSize()
        {
            if (vCam != null) return vCam.Lens.OrthographicSize;
            if (standardCamera != null) return standardCamera.orthographicSize;
            return 0f;
        }
    }
}
using Unity.Cinemachine;
using UnityEngine;

public class BackgroundScaler : MonoBehaviour
{
    [Header("Settings")]
    public CinemachineCamera vCam; // Kéo VCam vào đây
    public bool maintainAspectRatio = true; // Luôn giữ tỉ lệ khung hình (nên bật)

    private Vector3 initialScale;
    private float initialOrthoSize;

    void Start()
    {
        if (vCam == null)
        {
            Debug.LogError("Chưa gán CinemachineVirtualCamera cho BackgroundScaler!");
            enabled = false;
            return;
        }

        // 1. Ghi nhớ Scale và Size ban đầu làm "mốc chuẩn"
        initialScale = transform.localScale;
        initialOrthoSize = vCam.Lens.OrthographicSize;
    }

    void LateUpdate()
    {
        if (vCam == null) return;

        // 2. Tính tỉ lệ Zoom hiện tại so với ban đầu
        // Ví dụ: Lúc đầu size 5, giờ size 7 => ratio = 1.4
        float currentOrthoSize = vCam.Lens.OrthographicSize;
        float zoomRatio = currentOrthoSize / initialOrthoSize;

        // 3. Áp dụng tỉ lệ đó vào Scale của Background
        if (maintainAspectRatio)
        {
            transform.localScale = initialScale * zoomRatio;
        }
        else
        {
            // Nếu bạn chỉ muốn scale chiều Y (chiều cao) còn chiều ngang giữ nguyên (ít dùng)
            transform.localScale = new Vector3(initialScale.x, initialScale.y * zoomRatio, initialScale.z);
        }
    }
}
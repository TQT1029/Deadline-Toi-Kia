using Unity.Cinemachine;
using UnityEngine;

public class BackgroundScaler : MonoBehaviour
{
    public CinemachineCamera vCam;
    public bool maintainAspectRatio = true;

    private Vector3 _initialScale;
    private float _initialOrthoSize;

    void Start()
    {
        if (vCam == null) return;
        _initialScale = transform.localScale;
        _initialOrthoSize = vCam.Lens.OrthographicSize;
    }

    void LateUpdate()
    {
        if (vCam == null || _initialOrthoSize == 0) return;

        float zoomRatio = vCam.Lens.OrthographicSize / _initialOrthoSize;

        if (maintainAspectRatio)
            transform.localScale = _initialScale * zoomRatio;
        else
            transform.localScale = new Vector3(_initialScale.x, _initialScale.y * zoomRatio, _initialScale.z);
    }
}
using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class CinemachineRightMousePOVInput : MonoBehaviour
{
    [Header("Input")]
    public bool rotateOnlyWhenRightMouse = true;
    public bool invertY = false;

    private CinemachinePOV _pov;

    void Awake()
    {
        var vcam = GetComponent<CinemachineVirtualCamera>();
        _pov = vcam.GetCinemachineComponent<CinemachinePOV>();

        if (_pov == null)
        {
            Debug.LogError("Missing CinemachinePOV. Set Aim to POV on this Virtual Camera.", this);
            enabled = false;
            return;
        }

        // We push mouse input manually so we can gate it behind RMB.
        _pov.m_HorizontalAxis.m_InputAxisName = string.Empty;
        _pov.m_VerticalAxis.m_InputAxisName = string.Empty;
    }

    void Update()
    {
        if (_pov == null) return;

        bool canRotate = !rotateOnlyWhenRightMouse || Input.GetMouseButton(1);
        if (!canRotate)
        {
            _pov.m_HorizontalAxis.m_InputAxisValue = 0f;
            _pov.m_VerticalAxis.m_InputAxisValue = 0f;
            return;
        }

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        _pov.m_HorizontalAxis.m_InputAxisValue = mouseX;
        _pov.m_VerticalAxis.m_InputAxisValue = invertY ? mouseY : -mouseY;
    }
}

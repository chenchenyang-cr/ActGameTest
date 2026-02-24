using UnityEngine;

public class ThirdPersonOrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Tooltip("Camera looks at target.position + focusOffset. Increase Y to place the character lower in frame.")]
    public Vector3 focusOffset = new Vector3(0f, 1.35f, 0f);

    [Header("Distance")]
    public float distance = 4.5f;
    public float minDistance = 2f;
    public float maxDistance = 8f;

    [Header("Rotation (Old Input System)")]
    public bool rotateOnlyWhenRightMouse = true;
    public float yawSpeed = 180f;
    public float pitchSpeed = 120f;
    public bool invertY = false;

    [Header("Pitch Clamp")]
    public float minPitch = -15f;
    public float maxPitch = 65f;

    [Header("Follow")]
    public bool smoothFollow = true;
    [Min(0f)] public float positionSmoothTime = 0.06f;

    private float _yaw;
    private float _pitch;
    private Vector3 _focusPoint;
    private Vector3 _focusVelocity;

    void Awake()
    {
        Vector3 euler = transform.eulerAngles;
        _yaw = euler.y;
        _pitch = NormalizeAngle(euler.x);
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        if (target != null)
            _focusPoint = target.position + focusOffset;
    }

    void OnEnable()
    {
        if (target != null)
            _focusPoint = target.position + focusOffset;
    }

    void LateUpdate()
    {
        if (target == null) return;

        UpdateRotationInput();
        UpdateCameraTransform();
    }

    private void UpdateRotationInput()
    {
        bool canRotate = !rotateOnlyWhenRightMouse || Input.GetMouseButton(1);
        if (!canRotate) return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        _yaw += mouseX * yawSpeed * Time.deltaTime;

        float ySign = invertY ? 1f : -1f;
        _pitch += mouseY * pitchSpeed * Time.deltaTime * ySign;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
    }

    private void UpdateCameraTransform()
    {
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        Vector3 desiredFocusPoint = target.position + focusOffset;
        if (smoothFollow && positionSmoothTime > 0f)
        {
            _focusPoint = Vector3.SmoothDamp(_focusPoint, desiredFocusPoint, ref _focusVelocity, positionSmoothTime);
        }
        else
        {
            _focusPoint = desiredFocusPoint;
            _focusVelocity = Vector3.zero;
        }

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 desiredPosition = _focusPoint - rotation * Vector3.forward * distance;

        // V1 intentionally skips camera collision/anti-clipping.
        transform.position = desiredPosition;

        transform.rotation = rotation;
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}

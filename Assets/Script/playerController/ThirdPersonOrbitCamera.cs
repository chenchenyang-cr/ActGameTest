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

    [Header("Idle Auto Rotation")]
    public bool enableIdleAutoRotation = true;
    [Min(0f)] public float idleRotateDelay = 3f;
    [Min(0f)] public float idleRotateYawSpeed = 120f;
    [Min(0f)] public float idleRotatePitchSpeed = 90f;
    [Tooltip("Camera will rotate to this local euler (X=pitch, Y=yaw) after idle delay.")]
    public Vector3 idleTargetRotation = new Vector3(15f, 0f, 0f);

    private float _yaw;
    private float _pitch;
    private Vector3 _focusPoint;
    private Vector3 _focusVelocity;
    private float _lastCameraInputTime;

    void Awake()
    {
        Vector3 euler = transform.eulerAngles;
        _yaw = euler.y;
        _pitch = NormalizeAngle(euler.x);
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        _lastCameraInputTime = Time.time;

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

        float mouseX = 0f;
        float mouseY = 0f;
        bool hasManualRotationInput = false;

        if (canRotate)
        {
            mouseX = Input.GetAxis("Mouse X");
            mouseY = Input.GetAxis("Mouse Y");
            hasManualRotationInput = Mathf.Abs(mouseX) > 0.001f || Mathf.Abs(mouseY) > 0.001f;
        }

        if (hasManualRotationInput)
        {
            _lastCameraInputTime = Time.time;

            _yaw += mouseX * yawSpeed * Time.deltaTime;

            float ySign = invertY ? 1f : -1f;
            _pitch += mouseY * pitchSpeed * Time.deltaTime * ySign;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
            return;
        }

        if (!enableIdleAutoRotation) return;
        if (Time.time - _lastCameraInputTime < idleRotateDelay) return;

        float targetYaw = idleTargetRotation.y;
        float targetPitch = Mathf.Clamp(NormalizeAngle(idleTargetRotation.x), minPitch, maxPitch);

        _yaw = Mathf.MoveTowardsAngle(_yaw, targetYaw, idleRotateYawSpeed * Time.deltaTime);
        _pitch = Mathf.MoveTowards(_pitch, targetPitch, idleRotatePitchSpeed * Time.deltaTime);
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

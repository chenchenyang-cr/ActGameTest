using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RootMotionDriver : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;
    public ThirdPersonMover mover;

    [Header("Root Motion")]
    public float rootMotionWeight = 1f;
    public bool useRootRotation = true;
    public float rotationWeight = 1f;

    [Header("Facing Rotation")]
    [Tooltip("Maximum facing rotation speed in degrees per second.")]
    public float maxRotationSpeed = 540f;
    [Tooltip("If angle to target is below this value, do not rotate.")]
    public float rotationDeadZoneAngle = 2f;
    [Tooltip("X: normalized angle (0~1 for 0~180 degrees), Y: speed multiplier (0~1).")]
    public AnimationCurve rotationSpeedByAngle = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(1f, 1f)
    );

    private Rigidbody _rb;
    private Vector3 _animDeltaPos;
    private Quaternion _animDeltaRot = Quaternion.identity;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (mover == null) mover = GetComponent<ThirdPersonMover>();

        if (animator != null)
            animator.applyRootMotion = true;
    }

    void OnAnimatorMove()
    {
        if (animator == null) return;

        _animDeltaPos = animator.deltaPosition * rootMotionWeight;
        _animDeltaRot = Quaternion.Slerp(Quaternion.identity, animator.deltaRotation, rotationWeight);
    }

    void FixedUpdate()
    {
        ApplyMovement();
    }

    private void ApplyMovement()
    {
        Vector3 delta = _animDeltaPos;
        _rb.MovePosition(_rb.position + delta);

        bool hasMoveFacing = mover != null
                             && mover.faceMoveDirection
                             && mover.MoveDirection.sqrMagnitude > 0.0001f;

        if (hasMoveFacing)
        {
            ApplyFacingRotationByAngle();
        }
        else if (useRootRotation)
        {
            Quaternion targetRot = _rb.rotation * _animDeltaRot;
            _rb.MoveRotation(targetRot);
        }

        _animDeltaPos = Vector3.zero;
        _animDeltaRot = Quaternion.identity;
    }

    private void ApplyFacingRotationByAngle()
    {
        Vector3 moveDir = mover.MoveDirection;
        moveDir.y = 0f;
        if (moveDir.sqrMagnitude <= 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized, Vector3.up);
        float angle = Quaternion.Angle(_rb.rotation, targetRot);
        if (angle <= rotationDeadZoneAngle) return;

        float angle01 = Mathf.Clamp01(angle / 180f);
        float multiplier = Mathf.Clamp01(rotationSpeedByAngle.Evaluate(angle01));
        float degPerSecond = Mathf.Max(0f, maxRotationSpeed) * multiplier;
        if (degPerSecond <= 0f) return;

        Quaternion rotated = Quaternion.RotateTowards(_rb.rotation, targetRot, degPerSecond * Time.fixedDeltaTime);
        _rb.MoveRotation(rotated);
    }
}

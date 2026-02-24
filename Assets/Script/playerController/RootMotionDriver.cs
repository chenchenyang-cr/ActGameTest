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

        if (useRootRotation)
        {
            Quaternion targetRot = _rb.rotation * _animDeltaRot;
            _rb.MoveRotation(targetRot);
        }
        else if (mover != null && mover.MoveDirection.sqrMagnitude > 0.0001f && mover.faceMoveDirection)
        {
            Quaternion targetRot = Quaternion.LookRotation(mover.MoveDirection, Vector3.up);
            Quaternion blended = Quaternion.Slerp(_rb.rotation, targetRot, mover.rotationSpeed * Time.fixedDeltaTime);
            _rb.MoveRotation(blended);
        }

        _animDeltaPos = Vector3.zero;
        _animDeltaRot = Quaternion.identity;
    }
}

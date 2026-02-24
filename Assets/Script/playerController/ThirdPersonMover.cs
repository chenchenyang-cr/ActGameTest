using UnityEngine;

public class ThirdPersonMover : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;
    public string paramMoveX = "MoveX";
    public string paramMoveZ = "MoveZ";
    public string paramSpeed = "Speed";
    public float paramDamp = 0.08f;

    [Header("Move Input")]
    public float maxSpeed = 1.0f;

    [Tooltip("Rotation speed; higher is faster.")]
    public float rotationSpeed = 12f;

    [Tooltip("If true, face move direction.")]
    public bool faceMoveDirection = true;

    public Vector3 MoveDirection { get; private set; }
    public float MoveSpeed01 { get; private set; }

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // Input: camera-relative movement on XZ plane.
        float h = Input.GetAxisRaw("Horizontal"); // A/D
        float v = Input.GetAxisRaw("Vertical");   // W/S

        Vector3 input = new Vector3(h, 0f, v);
        if (input.sqrMagnitude > 1f) input.Normalize();

        Vector3 moveDir = input;
        if (Camera.main != null)
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();
            moveDir = camForward * input.z + camRight * input.x;
        }

        MoveDirection = moveDir;
        MoveSpeed01 = Mathf.Clamp01(input.magnitude) * maxSpeed;

        // Only set Animator parameters; actual motion is from Root Motion.
        if (animator != null)
        {
            animator.SetFloat(paramMoveX, MoveDirection.x, paramDamp, Time.deltaTime);
            animator.SetFloat(paramMoveZ, MoveDirection.z, paramDamp, Time.deltaTime);
            animator.SetFloat(paramSpeed, MoveSpeed01, paramDamp, Time.deltaTime);
        }
    }
}

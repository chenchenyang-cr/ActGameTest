using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonMover : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 5.5f;

    [Tooltip("转向速度（越大转得越快）。")]
    public float rotationSpeed = 12f;

    [Tooltip("是否朝向移动方向。第三人称一般建议开启。")]
    public bool faceMoveDirection = true;

    [Header("Gravity")]
    public float gravity = -20f;
    public float groundedStick = -2f; // 贴地速度，避免轻微离地导致抖动

    public bool IsGrounded => _cc.isGrounded;

    private CharacterController _cc;
    private Vector3 _verticalVel;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        // 1) 输入：世界坐标轴移动（不依赖相机）
        float h = Input.GetAxisRaw("Horizontal"); // A/D
        float v = Input.GetAxisRaw("Vertical");   // W/S

        Vector3 input = new Vector3(h, 0f, v);
        if (input.sqrMagnitude > 1f) input.Normalize();

        Vector3 moveDir = input; // 世界 XZ 方向

        // 2) 朝向移动方向（可选）
        if (faceMoveDirection && moveDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // 3) 重力（保证 grounded 稳定）
        if (_cc.isGrounded && _verticalVel.y < 0f)
            _verticalVel.y = groundedStick;

        _verticalVel.y += gravity * Time.deltaTime;

        // 4) Move
        Vector3 velocity = moveDir * moveSpeed + _verticalVel;
        _cc.Move(velocity * Time.deltaTime);
    }
}
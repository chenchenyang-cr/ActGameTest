using UnityEngine;

public class PlayerCombatBrain_TPS : MonoBehaviour
{
    [Header("Refs")]
    public ThirdPersonMover mover;
    public MoveExecutor moveExecutor;

    [Header("Input Buffer")]
    public float bufferTime = 0.20f;

    [Header("Keys (demo)")]
    public KeyCode attackKey = KeyCode.Mouse0; // 左键攻击
    public KeyCode launchKey = KeyCode.E;      // 预留：上挑
    public KeyCode dodgeKey = KeyCode.Space;   // 预留：闪避

    private InputBuffer _buffer;
    private CommandParser _parser;

    void Awake()
    {
        if (mover == null) mover = GetComponent<ThirdPersonMover>();
        if (moveExecutor == null) moveExecutor = GetComponent<MoveExecutor>();

        _buffer = new InputBuffer { bufferTime = bufferTime };
        _parser = new CommandParser();
    }

    void Update()
    {
        // 1) 采集输入（你换新 Input System 时，把 Press 放在回调里即可）
        if (Input.GetKeyDown(attackKey)) _buffer.Press(InputActionId.Attack);
        if (Input.GetKeyDown(launchKey)) _buffer.Press(InputActionId.Launch);
        if (Input.GetKeyDown(dodgeKey)) _buffer.Press(InputActionId.Dodge);

        // 2) Context（先全开放，后面做取消/受击再限制）
        var ctx = new CommandParser.Context
        {
            grounded = mover != null ? mover.IsGrounded : true,
            canAttack = true,
            canLaunch = true,
            canDodge = true,
            canJump = true,
            lockOnAvailable = false
        };

        // 3) 解析并消费一条指令
        if (_parser.TryDequeueCommand(_buffer, ctx, out var cmd))
        {
            switch (cmd.type)
            {
                case CommandType.Attack:
                    moveExecutor.NotifyAttackPressed(); // 驱动 A1/A2/A3
                    break;

                    // 下面先预留，你下一步做取消/上挑就用：
                    // case CommandType.Dodge: ...
                    // case CommandType.Launch: ...
            }
        }
    }
}
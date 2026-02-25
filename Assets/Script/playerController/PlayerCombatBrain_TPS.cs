using UnityEngine;

public class PlayerCombatBrain_TPS : MonoBehaviour
{
    [Header("Refs")]
    public MoveExecutor moveExecutor;

    [Header("Input Buffer")]
    public float bufferTime = 0.20f;

    [Header("Keys (demo)")]
    public KeyCode attackKey = KeyCode.Mouse0;
    public KeyCode launchKey = KeyCode.E;
    public KeyCode dodgeKey = KeyCode.Space;

    private InputBuffer _buffer;
    private CommandParser _parser;

    void Awake()
    {
        if (moveExecutor == null) moveExecutor = GetComponent<MoveExecutor>();

        _buffer = new InputBuffer { bufferTime = bufferTime };
        _parser = new CommandParser();
    }

    void Update()
    {
        if (Input.GetKeyDown(attackKey)) _buffer.Press(InputActionId.Attack);
        if (Input.GetKeyDown(launchKey)) _buffer.Press(InputActionId.Launch);
        if (Input.GetKeyDown(dodgeKey)) _buffer.Press(InputActionId.Dodge);

        bool canConsumeAttack = moveExecutor == null || moveExecutor.CanAcceptAttackCommandNow();

        var ctx = new CommandParser.Context
        {
            grounded = true,
            canAttack = canConsumeAttack,
            canLaunch = false,
            canDodge = false,
            canJump = false,
            lockOnAvailable = false
        };

        if (_parser.TryDequeueCommand(_buffer, ctx, out var cmd))
        {
            switch (cmd.type)
            {
                case CommandType.Attack:
                    if (moveExecutor != null)
                        moveExecutor.NotifyAttackPressed();
                    break;

                // Reserved for later combat actions.
                // case CommandType.Dodge: ...
                // case CommandType.Launch: ...
            }
        }
    }
}

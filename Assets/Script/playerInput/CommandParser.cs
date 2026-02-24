using System;
using UnityEngine;

public enum CommandType
{
    None,
    Attack,
    Launch,
    Dodge,
    Jump,
    Style,
    LockOnToggle
}

public struct Command
{
    public CommandType type;
    public InputActionId sourceAction;
    public float time;       // 输入发生的时间（按下时刻）
    public bool isHold;      // 是否被判定为“长按”
    public int priority;     // 计算后的优先级

    public override string ToString() => $"{type} (src={sourceAction}, hold={isHold}, prio={priority}, t={time:F3})";
}

/// <summary>
/// 将缓冲输入转换为高层命令，并执行简单的“允许/禁止”过滤与优先级选择。
/// 之后可以在这里加入“连段窗口、取消窗口”等战斗规则。
/// </summary>
public sealed class CommandParser
{
    public struct Context
    {
        public bool grounded;
        public bool canAttack;
        public bool canDodge;
        public bool canJump;
        public bool canLaunch;
        public bool lockOnAvailable;

        // 可扩展的状态字段示例：
        // public bool inComboWindow;   // 是否处于连段输入窗口
        // public bool inCancelWindow;  // 是否处于取消窗口
        // public bool isStunned;       // 是否处于硬直
        // public bool isAttacking;     // 是否正在攻击
    }

    // 优先级：数值越大越优先。可根据战斗设计调整。
    public int prioDodge = 100;
    public int prioLaunch = 90;
    public int prioAttack = 80;
    public int prioJump = 70;
    public int prioStyle = 60;
    public int prioLockOn = 10;

    private static readonly InputActionId[] _defaultActionSet =
    {
        InputActionId.Dodge,
        InputActionId.Launch,
        InputActionId.Attack,
        InputActionId.Jump,
        InputActionId.Style,
        InputActionId.LockOn
    };

    /// <summary>
    /// 根据当前战斗上下文，从输入缓冲中取出一个指令。
    /// 建议每帧调用一次；如果想一帧消费多个指令，可多次调用。
    /// </summary>
    public bool TryDequeueCommand(InputBuffer buffer, in Context ctx, out Command cmd)
    {
        // 根据当前上下文筛选允许的动作。
        // 后续可在此加入连段/取消窗口等规则。
        Span<InputActionId> allowed = stackalloc InputActionId[6];
        int count = 0;

        if (ctx.canDodge) allowed[count++] = InputActionId.Dodge;
        if (ctx.canLaunch) allowed[count++] = InputActionId.Launch;
        if (ctx.canAttack) allowed[count++] = InputActionId.Attack;
        if (ctx.canJump) allowed[count++] = InputActionId.Jump;
        // 风格动作通常允许在攻击中触发（按设计需求调整）
        allowed[count++] = InputActionId.Style;

        if (ctx.lockOnAvailable) allowed[count++] = InputActionId.LockOn;

        if (count == 0)
        {
            cmd = default;
            return false;
        }

        ReadOnlySpan<InputActionId> allowedSpan = allowed.Slice(0, count);

        bool ok = buffer.TryConsumeAny(
            allowedSpan,
            PriorityOf,
            out var ev);

        if (!ok)
        {
            cmd = default;
            return false;
        }

        cmd = new Command
        {
            type = MapToCommandType(ev.action),
            sourceAction = ev.action,
            time = ev.time,
            isHold = buffer.IsHold(ev.action),
            priority = PriorityOf(ev.action)
        };

        // 额外过滤示例（按需求启用）：
        // - 例如不在地面时禁用地面专用动作：
        //   if (!ctx.grounded && cmd.type == CommandType.Launch) { cmd = default; return false; }

        return true;
    }

    private int PriorityOf(InputActionId a)
    {
        return a switch
        {
            InputActionId.Dodge => prioDodge,
            InputActionId.Launch => prioLaunch,
            InputActionId.Attack => prioAttack,
            InputActionId.Jump => prioJump,
            InputActionId.Style => prioStyle,
            InputActionId.LockOn => prioLockOn,
            _ => 0
        };
    }

    private CommandType MapToCommandType(InputActionId a)
    {
        return a switch
        {
            InputActionId.Attack => CommandType.Attack,
            InputActionId.Launch => CommandType.Launch,
            InputActionId.Dodge => CommandType.Dodge,
            InputActionId.Jump => CommandType.Jump,
            InputActionId.Style => CommandType.Style,
            InputActionId.LockOn => CommandType.LockOnToggle,
            _ => CommandType.None
        };
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

public enum InputActionId
{
    Attack,
    Launch,
    Dodge,
    Jump,
    LockOn,
    Style
}

/// <summary>
/// 输入缓冲区：短时间保存“按下”事件（缓冲窗口），
/// 让战斗系统在允许的时机（连段窗口、取消窗口等）统一取用。
/// </summary>
public sealed class InputBuffer
{
    public struct BufferedEvent
    {
        public InputActionId action;
        public float time;         // 记录按下发生的 Time.time
        public bool isPressed;     // 是否为“按下”事件（当前总为 true，预留给释放事件）
        public int sequence;       // 自增序号，用于同一时间戳下的顺序判定

        public override string ToString() => $"{action} t={time:F3} seq={sequence}";
    }

    /// <summary>按下事件的有效时间（缓冲窗口长度）。</summary>
    public float bufferTime = 0.20f;

    /// <summary>判定“长按”的阈值（秒）。</summary>
    public float holdThreshold = 0.25f;

    private readonly List<BufferedEvent> _events = new List<BufferedEvent>(32);
    private readonly Dictionary<InputActionId, float> _pressTime = new Dictionary<InputActionId, float>();
    private int _seq;

    public void Clear()
    {
        _events.Clear();
        _pressTime.Clear();
        _seq = 0;
    }

    /// <summary>当输入动作被按下时调用（建议在输入回调里调用）。</summary>
    public void Press(InputActionId action)
    {
        float now = Time.time;
        _pressTime[action] = now;

        _events.Add(new BufferedEvent
        {
            action = action,
            time = now,
            isPressed = true,
            sequence = ++_seq
        });

        PruneExpired(now);
    }

    /// <summary>
    /// 可选：在释放时调用，用于更精确的长按判断。
    /// 目前缓冲区只存“按下”事件，这里主要用于维护按下时间。
    /// </summary>
    public void Release(InputActionId action)
    {
        // 缓冲区仅使用“按下”事件；释放只用于长按时长计算。
        // 若没有对应的按下记录，直接忽略。
        if (_pressTime.ContainsKey(action) == false)
            return;

        // 这里选择保留按下时间；如果希望释放后 IsHeld 立即变为 false，
        // 可以在此移除 _pressTime[action]。
    }

    /// <summary>
    /// 当前是否处于按住状态（基于最后一次按下时间与 Release 的管理策略）。
    /// </summary>
    public bool IsHeld(InputActionId action)
    {
        if (!_pressTime.TryGetValue(action, out float t))
            return false;

        // 若在 Release 中移除了 _pressTime，则此处会返回 false。
        return (Time.time - t) >= 0f;
    }

    public float HeldDuration(InputActionId action)
    {
        if (!_pressTime.TryGetValue(action, out float t))
            return 0f;
        return Mathf.Max(0f, Time.time - t);
    }

    public bool IsHold(InputActionId action) => HeldDuration(action) >= holdThreshold;

    /// <summary>
    /// 消费指定动作中最早的有效“按下”事件。
    /// 返回 true 表示成功消费并从缓冲中移除。
    /// </summary>
    public bool TryConsume(InputActionId action, out BufferedEvent consumed)
    {
        float now = Time.time;
        PruneExpired(now);

        for (int i = 0; i < _events.Count; i++)
        {
            var e = _events[i];
            if (!e.isPressed) continue;
            if (e.action != action) continue;

            consumed = e;
            _events.RemoveAt(i);
            return true;
        }

        consumed = default;
        return false;
    }

    /// <summary>
    /// 在一组动作中，按“优先级+时间+序号”的规则消费最合适的按下事件：
    /// 1) 优先级高的先选
    /// 2) 优先级相同，时间更早的先选
    /// 3) 仍相同，序号更小的先选
    /// </summary>
    public bool TryConsumeAny(ReadOnlySpan<InputActionId> actions, Func<InputActionId, int> priority, out BufferedEvent consumed)
    {
        float now = Time.time;
        PruneExpired(now);

        int bestIndex = -1;
        int bestPrio = int.MinValue;
        float bestTime = float.MaxValue;
        int bestSeq = int.MaxValue;

        for (int i = 0; i < _events.Count; i++)
        {
            var e = _events[i];
            if (!e.isPressed) continue;

            bool allowed = false;
            for (int a = 0; a < actions.Length; a++)
            {
                if (e.action == actions[a]) { allowed = true; break; }
            }
            if (!allowed) continue;

            int p = priority(e.action);

            // 选择规则：高优先级胜出；同优先级取更早时间；仍相同取更小序号
            if (p > bestPrio ||
                (p == bestPrio && e.time < bestTime) ||
                (p == bestPrio && Mathf.Approximately(e.time, bestTime) && e.sequence < bestSeq))
            {
                bestIndex = i;
                bestPrio = p;
                bestTime = e.time;
                bestSeq = e.sequence;
            }
        }

        if (bestIndex >= 0)
        {
            consumed = _events[bestIndex];
            _events.RemoveAt(bestIndex);
            return true;
        }

        consumed = default;
        return false;
    }

    /// <summary>清理已过期的缓冲事件。</summary>
    private void PruneExpired(float now)
    {
        for (int i = _events.Count - 1; i >= 0; i--)
        {
            if (now - _events[i].time > bufferTime)
                _events.RemoveAt(i);
        }
    }
}

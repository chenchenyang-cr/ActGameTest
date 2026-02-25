using System;
using UnityEngine;

public enum MoveState { Idle, Running }

public class MoveExecutor : MonoBehaviour
{
    [Header("Data")]
    public MoveSet moveSet;

    [Header("Animation (optional)")]
    public Animator animator;
    [Tooltip("If true: use Animator.Play(stateName). If false: use Animator.SetTrigger(triggerName).")]
    public bool playByStateName = true;

    [Header("Debug")]
    public bool logTransitions = false;

    public MoveState State { get; private set; } = MoveState.Idle;
    public MoveData CurrentMove { get; private set; }

    public event Action<MoveData> OnMoveEnter;
    public event Action<MoveData> OnMoveExit;

    private bool _chainRequested;
    private bool _restartComboRequested;
    private bool _comboWindowOpen;

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    public void NotifyAttackPressed()
    {
        if (State == MoveState.Idle)
        {
            TryStartMove(moveSet != null ? moveSet.A1 : null);
            return;
        }

        if (CurrentMove == null)
            return;

        if (IsCurrentLightAttack() && _comboWindowOpen)
        {
            _chainRequested = true;
            return;
        }

        if (IsCurrentLightEnd())
        {
            _restartComboRequested = true;
        }
    }

    public bool CanAcceptAttackCommandNow()
    {
        if (State == MoveState.Idle) return true;
        if (CurrentMove == null) return false;
        if (IsCurrentLightEnd()) return true;
        if (IsCurrentLightAttack()) return _comboWindowOpen;
        return false;
    }

    public bool TryStartMove(MoveData move)
    {
        if (move == null)
        {
            Debug.LogWarning("[MoveExecutor] TryStartMove: move is null.");
            return false;
        }

        if (State != MoveState.Idle) return false;

        EnterMove(move);
        return true;
    }

    private void EnterMove(MoveData move)
    {
        if (CurrentMove != null)
            OnMoveExit?.Invoke(CurrentMove);

        State = MoveState.Running;
        CurrentMove = move;

        _chainRequested = false;
        _restartComboRequested = false;
        _comboWindowOpen = false;

        PlayMoveAnimation(move);
        OnMoveEnter?.Invoke(move);

        if (logTransitions)
            Debug.Log($"[MoveExecutor] EnterMove: {move.moveId}");
    }

    private void ExitMove()
    {
        if (CurrentMove != null && logTransitions)
            Debug.Log($"[MoveExecutor] ExitMove: {CurrentMove.moveId}");

        if (CurrentMove != null)
            OnMoveExit?.Invoke(CurrentMove);

        CurrentMove = null;
        State = MoveState.Idle;

        _chainRequested = false;
        _restartComboRequested = false;
        _comboWindowOpen = false;
    }

    private void PlayMoveAnimation(MoveData move)
    {
        if (animator == null) return;
        if (string.IsNullOrEmpty(move.animStateOrTrigger)) return;

        if (playByStateName)
        {
            animator.Play(move.animStateOrTrigger, 0, 0f);
        }
        else
        {
            animator.ResetTrigger(move.animStateOrTrigger);
            animator.SetTrigger(move.animStateOrTrigger);
        }
    }

    private bool IsCurrentLightAttack()
    {
        return moveSet != null && moveSet.IsLightAttack(CurrentMove);
    }

    private bool IsCurrentLightEnd()
    {
        return moveSet != null && moveSet.IsLightEnd(CurrentMove);
    }

    // Animation Events (hook these in clips / Animation Window)
    public void AE_ComboWindowOpen()
    {
        if (!IsCurrentLightAttack()) return;
        _comboWindowOpen = true;
    }

    public void AE_ComboWindowClose()
    {
        _comboWindowOpen = false;
    }

    public void AE_AttackMoveEnd()
    {
        if (!IsCurrentLightAttack()) return;

        if (TryHandleChainFromAttackEndEvent())
            return;

        if (TryEnterCurrentLightEnd())
            return;

        ExitMove();
    }

    public void AE_EndMoveEnd()
    {
        if (!IsCurrentLightEnd()) return;

        if (TryRestartComboFromEnd())
            return;

        ExitMove();
    }

    private bool TryHandleChainFromAttackEndEvent()
    {
        if (!_chainRequested) return false;

        var next = moveSet != null ? moveSet.GetNextLight(CurrentMove) : null;
        _chainRequested = false;
        if (next == null) return false;

        if (logTransitions)
            Debug.Log($"[MoveExecutor] Chain(Event): {CurrentMove.moveId} -> {next.moveId}");

        EnterMove(next);
        return true;
    }

    private bool TryEnterCurrentLightEnd()
    {
        if (!IsCurrentLightAttack()) return false;

        var endMove = moveSet != null ? moveSet.GetLightEnd(CurrentMove) : null;
        if (endMove == null)
            return false;

        if (logTransitions)
            Debug.Log($"[MoveExecutor] End(Event): {CurrentMove.moveId} -> {endMove.moveId}");

        EnterMove(endMove);
        return true;
    }

    private bool TryRestartComboFromEnd()
    {
        if (!IsCurrentLightEnd() || !_restartComboRequested)
            return false;

        var restart = moveSet != null ? moveSet.A1 : null;
        _restartComboRequested = false;
        if (restart == null)
            return false;

        if (logTransitions)
            Debug.Log($"[MoveExecutor] RestartCombo(Event): {CurrentMove.moveId} -> {restart.moveId}");

        EnterMove(restart);
        return true;
    }
}

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
    public PhaseType CurrentPhaseType => _currentPhase.type;
    public float PhaseTime => _phaseTime;

    // Events (optional for later integration)
    public event Action<MoveData> OnMoveEnter;
    public event Action<MoveData> OnMoveExit;
    public event Action<MoveData, PhaseType> OnPhaseEnter;

    private int _phaseIndex;
    private float _phaseTime;
    private PhaseData _currentPhase;

    // buffered "want to chain" flag (set when Attack pressed during ChainWindow)
    private bool _chainRequested;

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        Tick(Time.deltaTime);
    }

    /// <summary>
    /// External systems call this when they parsed an Attack command.
    /// (In later steps you'll pass in Command and handle dodge/jump cancel etc.)
    /// </summary>
    public void NotifyAttackPressed()
    {
        // If idle -> start A1 immediately.
        if (State == MoveState.Idle)
        {
            TryStartMove(moveSet != null ? moveSet.A1 : null);
            return;
        }

        // If running -> if we're in chain window, mark request; otherwise ignore (buffer already handled earlier).
        if (IsInWindow(WindowType.Chain))
        {
            _chainRequested = true;
        }
    }

    public bool TryStartMove(MoveData move)
    {
        if (move == null) return false;

        // For now: don't allow starting a new move while running (except chaining at phase transitions)
        if (State != MoveState.Idle) return false;

        EnterMove(move);
        return true;
    }

    public void Tick(float dt)
    {
        if (State == MoveState.Idle || CurrentMove == null)
            return;

        _phaseTime += dt;

        // Phase end?
        if (_phaseTime >= _currentPhase.duration)
        {
            // At boundary: if chain requested and next exists -> chain now
            if (_chainRequested)
            {
                var next = moveSet != null ? moveSet.GetNextLight(CurrentMove) : null;
                if (next != null)
                {
                    if (logTransitions) Debug.Log($"[MoveExecutor] Chain: {CurrentMove.moveId} -> {next.moveId}");
                    _chainRequested = false;
                    EnterMove(next); // chaining = enter new move immediately
                    return;
                }

                // No next -> consume request but keep flowing
                _chainRequested = false;
            }

            // advance to next phase
            _phaseIndex++;
            if (_phaseIndex >= CurrentMove.PhaseCount)
            {
                ExitMove();
                return;
            }

            EnterPhase(_phaseIndex);
        }
    }

    // -------------------------
    // Window helpers
    // -------------------------
    public bool IsInWindow(WindowType type)
    {
        if (State != MoveState.Running) return false;
        return _currentPhase.HasWindow(type, _phaseTime);
    }

    // -------------------------
    // Internals
    // -------------------------
    private void EnterMove(MoveData move)
    {
        // Exit previous (if chaining directly, exit old first)
        if (CurrentMove != null)
        {
            OnMoveExit?.Invoke(CurrentMove);
        }

        State = MoveState.Running;
        CurrentMove = move;

        _chainRequested = false;
        _phaseIndex = 0;

        // play animation (optional)
        PlayMoveAnimation(move);

        OnMoveEnter?.Invoke(move);

        EnterPhase(_phaseIndex);

        if (logTransitions)
            Debug.Log($"[MoveExecutor] EnterMove: {move.moveId}");
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

    private void EnterPhase(int index)
    {
        _phaseIndex = index;
        _currentPhase = CurrentMove.GetPhase(index);
        _phaseTime = 0f;

        OnPhaseEnter?.Invoke(CurrentMove, _currentPhase.type);

        if (logTransitions)
            Debug.Log($"[MoveExecutor] Phase: {CurrentMove.moveId} -> {_currentPhase.type}");
    }

    private void ExitMove()
    {
        if (logTransitions)
            Debug.Log($"[MoveExecutor] ExitMove: {CurrentMove.moveId}");

        OnMoveExit?.Invoke(CurrentMove);

        CurrentMove = null;
        State = MoveState.Idle;

        _phaseIndex = 0;
        _phaseTime = 0f;
        _currentPhase = default;
        _chainRequested = false;
    }
}
using UnityEngine;

public class ThirdPersonMover : MonoBehaviour
{
    public enum LocomotionState
    {
        Idle,
        WalkStart,
        RunStart,
        Walk,
        Run,
        End
    }

    [Header("Animator")]
    public Animator animator;
    public MoveExecutor combatExecutor;

    [Header("Animation State Names")]
    public string idleStateName = "Idle";
    public string walkStartStateName = "WalkStart";
    public string runStartStateName = "RunStart";
    public string walkStateName = "WalkLoop";
    public string runStateName = "RunLoop";
    public string endStateName = "End";

    [Header("CrossFade")]
    public float locomotionCrossFade = 0.10f;
    public float walkRunSwitchCrossFade = 0.20f;
    public float actionCrossFade = 0.12f;

    [Tooltip("Rotation speed; higher is faster.")]
    public float rotationSpeed = 12f;

    [Tooltip("If true, face move direction.")]
    public bool faceMoveDirection = true;

    [Header("Combat Lock")]
    [Tooltip("If true, block locomotion state updates while combat move is running.")]
    public bool blockDuringCombatMove = true;

    public bool logLocomotionTransitions = false;

    public Vector3 MoveDirection { get; private set; }
    public LocomotionState CurrentLocomotionState { get; private set; } = LocomotionState.Idle;

    private bool _hasMoveInput;
    private bool _runHeld;
    private bool _wasBlockedByCombat;

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (combatExecutor == null) combatExecutor = GetComponent<MoveExecutor>();
    }

    void Update()
    {
        if (ShouldBlockLocomotionForCombat())
        {
            SuppressLocomotionForCombat();
            return;
        }

        if (_wasBlockedByCombat)
        {
            ReadInputAndBuildMoveDirection();
            ForceResyncLocomotionAfterCombat();
            _wasBlockedByCombat = false;
            return;
        }

        ReadInputAndBuildMoveDirection();
        UpdateLocomotionStateMachine();
    }

    private bool ShouldBlockLocomotionForCombat()
    {
        if (!blockDuringCombatMove) return false;
        if (combatExecutor == null) return false;
        if (combatExecutor.State != MoveState.Running) return false;

        if (HasMoveInputNow() && combatExecutor.TryInterruptEndByMovement())
            return false;

        return true;
    }

    private static bool HasMoveInputNow()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        return (h * h + v * v) > 0.0001f;
    }

    private void SuppressLocomotionForCombat()
    {
        MoveDirection = Vector3.zero;
        _hasMoveInput = false;
        _runHeld = false;
        _wasBlockedByCombat = true;
    }

    private void ForceResyncLocomotionAfterCombat()
    {
        if (animator == null) return;

        if (!_hasMoveInput)
        {
            ForceEnterState(LocomotionState.Idle);
            return;
        }

        ForceEnterState(_runHeld ? LocomotionState.RunStart : LocomotionState.WalkStart);
    }

    private void ReadInputAndBuildMoveDirection()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

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
        _hasMoveInput = input.sqrMagnitude > 0.0001f;
        _runHeld = _hasMoveInput && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
    }

    private void UpdateLocomotionStateMachine()
    {
        if (animator == null) return;

        switch (CurrentLocomotionState)
        {
            case LocomotionState.Idle:
                if (_hasMoveInput)
                {
                    EnterState(_runHeld ? LocomotionState.RunStart : LocomotionState.WalkStart);
                }
                break;

            case LocomotionState.WalkStart:
            case LocomotionState.RunStart:
                if (!_hasMoveInput)
                {
                    EnterState(LocomotionState.End);
                    break;
                }

                if (IsNonLoopAnimationComplete(CurrentLocomotionState == LocomotionState.RunStart ? runStartStateName : walkStartStateName))
                {
                    EnterState(_runHeld ? LocomotionState.Run : LocomotionState.Walk);
                }
                break;

            case LocomotionState.Walk:
            case LocomotionState.Run:
                if (!_hasMoveInput)
                {
                    EnterState(LocomotionState.End);
                    break;
                }

                if (CurrentLocomotionState == LocomotionState.Walk && _runHeld)
                {
                    EnterState(LocomotionState.Run);
                }
                else if (CurrentLocomotionState == LocomotionState.Run && !_runHeld)
                {
                    EnterState(LocomotionState.Walk);
                }
                break;

            case LocomotionState.End:
                if (_hasMoveInput)
                {
                    EnterState(_runHeld ? LocomotionState.RunStart : LocomotionState.WalkStart);
                    break;
                }

                if (IsNonLoopAnimationComplete(endStateName))
                {
                    EnterState(LocomotionState.Idle);
                }
                break;
        }
    }

    private void EnterState(LocomotionState nextState)
    {
        if (CurrentLocomotionState == nextState) return;
        EnterStateInternal(nextState);
    }

    private void ForceEnterState(LocomotionState nextState)
    {
        EnterStateInternal(nextState);
    }

    private void EnterStateInternal(LocomotionState nextState)
    {
        if (animator == null) return;

        CurrentLocomotionState = nextState;

        string animState = GetAnimatorStateName(nextState);
        if (!string.IsNullOrEmpty(animState))
        {
            float fade = GetCrossFadeDuration(nextState);
            animator.CrossFade(animState, fade, 0);
        }

        if (logLocomotionTransitions)
            Debug.Log("[ThirdPersonMover] -> " + nextState);
    }

    private static bool IsActionState(LocomotionState state)
    {
        return state == LocomotionState.WalkStart
            || state == LocomotionState.RunStart
            || state == LocomotionState.End;
    }

    private float GetCrossFadeDuration(LocomotionState nextState)
    {
        if ((CurrentLocomotionState == LocomotionState.Walk && nextState == LocomotionState.Run) ||
            (CurrentLocomotionState == LocomotionState.Run && nextState == LocomotionState.Walk))
        {
            return walkRunSwitchCrossFade;
        }

        return IsActionState(nextState) ? actionCrossFade : locomotionCrossFade;
    }

    private string GetAnimatorStateName(LocomotionState state)
    {
        switch (state)
        {
            case LocomotionState.Idle: return idleStateName;
            case LocomotionState.WalkStart: return walkStartStateName;
            case LocomotionState.RunStart: return runStartStateName;
            case LocomotionState.Walk: return walkStateName;
            case LocomotionState.Run: return runStateName;
            case LocomotionState.End: return endStateName;
            default: return string.Empty;
        }
    }

    private bool IsNonLoopAnimationComplete(string expectedStateName)
    {
        if (string.IsNullOrEmpty(expectedStateName)) return true;
        if (animator == null) return true;
        if (animator.IsInTransition(0)) return false;

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        if (!MatchesState(info, expectedStateName)) return false;

        return info.normalizedTime >= 1f;
    }

    private static bool MatchesState(AnimatorStateInfo info, string stateName)
    {
        return info.IsName(stateName) || info.shortNameHash == Animator.StringToHash(stateName);
    }
}

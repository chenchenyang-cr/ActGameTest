using System;
using UnityEngine;

public enum PhaseType { Startup, Active, Recovery }
public enum WindowType { Chain, CancelToDodge, CancelToJump }

[Serializable]
public struct WindowData
{
    public WindowType type;

    [Tooltip("Phase 内相对时间（秒）")]
    public float from;
    [Tooltip("Phase 内相对时间（秒）")]
    public float to;

    public bool Contains(float t) => t >= from && t <= to;
}

[Serializable]
public struct PhaseData
{
    public PhaseType type;

    [Min(0f)]
    public float duration;

    [Tooltip("该阶段内开放的窗口（连段/取消等）")]
    public WindowData[] windows;

    public bool HasWindow(WindowType w, float phaseTime)
    {
        if (windows == null) return false;
        for (int i = 0; i < windows.Length; i++)
            if (windows[i].type == w && windows[i].Contains(phaseTime))
                return true;
        return false;
    }
}

[CreateAssetMenu(menuName = "DMC/Moves/MoveData", fileName = "MoveData")]
public class MoveData : ScriptableObject
{
    [Header("Identity")]
    public string moveId = "A1";

    [Header("Animation (optional)")]
    [Tooltip("Animator state name OR trigger name (you decide how to play).")]
    public string animStateOrTrigger;

    [Header("Phases (must include Startup/Active/Recovery)")]
    public PhaseData[] phases;

    public int PhaseCount => phases?.Length ?? 0;

    public PhaseData GetPhase(int index)
    {
        if (phases == null || index < 0 || index >= phases.Length)
            throw new IndexOutOfRangeException($"Move {moveId}: phase index out of range: {index}");
        return phases[index];
    }

    public int FindPhaseIndex(PhaseType type)
    {
        if (phases == null) return -1;
        for (int i = 0; i < phases.Length; i++)
            if (phases[i].type == type) return i;
        return -1;
    }
}
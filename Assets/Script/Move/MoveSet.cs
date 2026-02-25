using UnityEngine;

[CreateAssetMenu(menuName = "DMC/Moves/MoveSet", fileName = "MoveSet")]
public class MoveSet : ScriptableObject
{
    [Header("Ground Light Combo")]
    public MoveData A1;
    public MoveData A2;
    public MoveData A3;
    public MoveData A4;

    [Header("Ground Light Combo End")]
    public MoveData A1End;
    public MoveData A2End;
    public MoveData A3End;
    public MoveData A4End;

    public MoveData GetNextLight(MoveData current)
    {
        if (current == null) return A1;
        if (current == A1) return A2;
        if (current == A2) return A3;
        if (current == A3) return A4;
        return null; // A4 has no next (or next not configured)
    }

    public MoveData GetLightEnd(MoveData current)
    {
        if (current == A1) return A1End;
        if (current == A2) return A2End;
        if (current == A3) return A3End;
        if (current == A4) return A4End;
        return null;
    }

    public bool IsLightAttack(MoveData move)
    {
        return move != null && (move == A1 || move == A2 || move == A3 || move == A4);
    }

    public bool IsLightEnd(MoveData move)
    {
        return move != null && (move == A1End || move == A2End || move == A3End || move == A4End);
    }
}

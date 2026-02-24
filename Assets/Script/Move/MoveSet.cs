using UnityEngine;

[CreateAssetMenu(menuName = "DMC/Moves/MoveSet", fileName = "MoveSet")]
public class MoveSet : ScriptableObject
{
    [Header("Ground Light Combo")]
    public MoveData A1;
    public MoveData A2;
    public MoveData A3;

    public MoveData GetNextLight(MoveData current)
    {
        if (current == null) return A1;
        if (current == A1) return A2;
        if (current == A2) return A3;
        return null; // A3 has no next
    }
}
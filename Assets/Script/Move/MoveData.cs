using UnityEngine;

[CreateAssetMenu(menuName = "DMC/Moves/MoveData", fileName = "MoveData")]
public class MoveData : ScriptableObject
{
    [Header("Identity")]
    public string moveId = "A1";

    [Header("Animation")]
    [Tooltip("Animator state name OR trigger name (you decide how to play).")]
    public string animStateOrTrigger;
}

using UnityEngine;

public class RightFingerTouchZone : MonoBehaviour
{
    public enum FingerType { Thumb, Index, Middle }

    [Header("Which finger this zone belongs to")]
    [Tooltip("Used to gate retargeting so only the selected finger (confirmedMotorID) can retarget")]
    public FingerType finger = FingerType.Thumb;

    [Header("Right-hand finger")]
    public FingerPath rightFinger;

    [Header("Mapped claw finger")]
    public FingerPath clawFinger;

    /// <summary>
    /// Returns true if this zone's finger matches the given confirmed motor ID.
    /// Thumb: 1-4 / 13, Index: 5-8 / 14, Middle: 9-12 / 15.
    /// </summary>
    public bool MatchesMotorID(int motorID)
    {
        switch (finger)
        {
            case FingerType.Thumb:
                return (motorID >= 1 && motorID <= 4) || motorID == 13;
            case FingerType.Index:
                return (motorID >= 5 && motorID <= 8) || motorID == 14;
            case FingerType.Middle:
                return (motorID >= 9 && motorID <= 12) || motorID == 15;
            default:
                return false;
        }
    }
}
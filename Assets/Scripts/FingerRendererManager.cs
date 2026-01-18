using UnityEngine;

/// <summary>
/// Manages which finger LineRenderers are visible based on modeSelect state
/// </summary>
public class FingerRendererManager : MonoBehaviour
{
    [Header("Mode Control")]
    public ModeSwitching modeSwitching;

    [Header("5-Point Finger Renderers (for modeSelect=true)")]
    public LineRenderer[] fivePointRenderers;

    [Header("2-Point Finger Renderers (for modeSelect=false)")]
    public LineRenderer[] twoPointRenderers;

    [Header("5-Point Colliders (for modeSelect=true)")]
    public Collider[] fivePointColliders;

    [Header("2-Point Colliders (for modeSelect=false)")]
    public Collider[] twoPointColliders;

    void Update()
    {
        if (modeSwitching == null) return;

        bool showFivePoints = modeSwitching.modeSelect;
        bool showTwoPoints = !modeSwitching.modeSelect;

        // Enable/disable 5-point renderers
        if (fivePointRenderers != null)
        {
            foreach (var renderer in fivePointRenderers)
            {
                if (renderer != null)
                    renderer.enabled = showFivePoints;
            }
        }

        // Enable/disable 2-point renderers
        if (twoPointRenderers != null)
        {
            foreach (var renderer in twoPointRenderers)
            {
                if (renderer != null)
                    renderer.enabled = showTwoPoints;
            }
        }

        // Enable/disable 5-point colliders
        if (fivePointColliders != null)
        {
            foreach (var collider in fivePointColliders)
            {
                if (collider != null)
                    collider.enabled = showFivePoints;
            }
        }

        // Enable/disable 2-point colliders
        if (twoPointColliders != null)
        {
            foreach (var collider in twoPointColliders)
            {
                if (collider != null)
                    collider.enabled = showTwoPoints;
            }
        }
    }
}

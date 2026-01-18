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
    }
}

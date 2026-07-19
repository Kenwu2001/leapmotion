using UnityEngine;
using TMPro;

public class HUDText : MonoBehaviour
{
    private TextMeshPro textMesh;
    public ClawModuleController clawModuleController;
    public TriggerRightWrist triggerRightWrist;

    void Start()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    void Update()
    {
        if (textMesh == null || clawModuleController == null)
        {
            return;
        }

        bool isReset = clawModuleController.IsResetState;
        bool isFullRangeMapping = clawModuleController.isFullRangeMapping;
        bool isIndexMiddleIndividual = clawModuleController.useIndexMiddleIndividualMode;
        bool isEngaged = triggerRightWrist != null && triggerRightWrist.IsEngaged;
        bool currentSnappingEnabled = clawModuleController.IsCurrentSnappingEnabled();
        string fullRangeText = isFullRangeMapping ? "" : "Small Range Mapping"; // "Full Range Mapping"
        string indexMiddleText = isIndexMiddleIndividual ? "Index Middle Individual" : ""; // "Index Middle Coupled"
        string engagementText = isEngaged ? "Engagement" : "Disengagement";
        string currentSnappingColor = currentSnappingEnabled ? "green" : "white";
        string fullRangeColor = isFullRangeMapping ? "white" : "green";
        string resetColor = isReset ? "green" : "white";
        string individualColor = isIndexMiddleIndividual ? "green" : "white";
        string engagementColor = isEngaged ? "green" : "red";

        textMesh.text =
            $"<color={engagementColor}>{engagementText}</color>\n" +
            (currentSnappingEnabled
                ? $"<color={currentSnappingColor}>Snapping</color>\n"
                : string.Empty) +
            $"<color={fullRangeColor}>{fullRangeText}</color>\n" +
            $"<color={individualColor}>{indexMiddleText}</color>\n";

    }
}
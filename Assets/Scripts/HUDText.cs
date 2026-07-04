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
        bool showThumbMiddle180 = clawModuleController.thumbMiddle180SnappingVisible;
        bool thumbMiddle180Enabled = clawModuleController.isThumbMiddle180SnappingEnabled;
        string fullRangeText = isFullRangeMapping ? "Full Range Mapping" : "Small Range Mapping";
        string indexMiddleText = isIndexMiddleIndividual ? "Index Middle Individual" : "Index Middle Coupled";
        string engagementText = isEngaged ? "Engagement" : "Disengagement";
        string thumbMiddle180Color = thumbMiddle180Enabled ? "green" : "white";
        string fullRangeColor = isFullRangeMapping ? "white" : "green";
        string resetColor = isReset ? "green" : "white";
        string individualColor = isIndexMiddleIndividual ? "green" : "white";
        string engagementColor = isEngaged ? "green" : "white";

        textMesh.text =
            $"<color={engagementColor}>{engagementText}</color>\n" +
            $"<color={fullRangeColor}>{fullRangeText}</color>\n" +
            $"<color={individualColor}>{indexMiddleText}</color>\n" +
            (showThumbMiddle180
                ? $"<color={thumbMiddle180Color}>thumbMiddle180Snapping</color>\n"
                : string.Empty);
    }
}
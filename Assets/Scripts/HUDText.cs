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
        bool show180Snapping = clawModuleController.hasAny180SnappingVisible;
        bool current180Enabled = clawModuleController.IsCurrent180SnappingEnabled();
        string current180Text = clawModuleController.GetCurrent180SnappingText();
        string fullRangeText = isFullRangeMapping ? "Full Range Mapping" : "Small Range Mapping";
        string indexMiddleText = isIndexMiddleIndividual ? "Index Middle Individual" : "Index Middle Coupled";
        string engagementText = isEngaged ? "Engagement" : "Disengagement";
        string current180Color = current180Enabled ? "green" : "white";
        string fullRangeColor = isFullRangeMapping ? "white" : "green";
        string resetColor = isReset ? "green" : "white";
        string individualColor = isIndexMiddleIndividual ? "green" : "white";
        string engagementColor = isEngaged ? "green" : "white";

        textMesh.text =
            $"<color={engagementColor}>{engagementText}</color>\n" +
            $"<color={fullRangeColor}>{fullRangeText}</color>\n" +
            $"<color={individualColor}>{indexMiddleText}</color>\n" +
            (show180Snapping
                ? $"<color={current180Color}>{current180Text}</color>\n"
                : string.Empty);
    }
}
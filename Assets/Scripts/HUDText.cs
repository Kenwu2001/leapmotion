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
        string fullRangeText = isFullRangeMapping ? "Full Range Mapping" : "Small Range Mapping";
        string indexMiddleText = isIndexMiddleIndividual ? "Index Middle Individual" : "Index Middle coupled";
        string engagementText = isEngaged ? "Engagement" : "Disengagement";
        string fullRangeColor = isFullRangeMapping ? "green" : "red";
        string resetColor = isReset ? "green" : "red";
        string individualColor = isIndexMiddleIndividual ? "green" : "red";
        string engagementColor = isEngaged ? "green" : "red";

        textMesh.text =
            $"<color={fullRangeColor}>{fullRangeText}</color>\n" +
            $"<color={individualColor}>{indexMiddleText}</color>\n" +
            $"<color={engagementColor}>{engagementText}</color>";
    }
}
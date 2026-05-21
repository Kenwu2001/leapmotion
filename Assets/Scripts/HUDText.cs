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
        bool isEngaged = triggerRightWrist != null && triggerRightWrist.IsEngaged;
        string fullRangeColor = isFullRangeMapping ? "green" : "red";
        string resetColor = isReset ? "green" : "red";
        string engagementColor = isEngaged ? "green" : "red";

        textMesh.text =
            $"FullRangeMapping: <color={fullRangeColor}>{isFullRangeMapping}</color>\n" +
            $"Reset: <color={resetColor}>{isReset}</color>\n" +
            $"Engagement: <color={engagementColor}>{isEngaged}</color>";
    }
}
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
        bool isEngaged = triggerRightWrist != null && triggerRightWrist.IsEngaged;

        textMesh.text =
            $"FullRangeMapping: {clawModuleController.isFullRangeMapping}\n" +
            $"Engagement: {isEngaged}";
    }
}
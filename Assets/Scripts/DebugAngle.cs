using TMPro; // 引入 TextMeshPro 命名空間
using UnityEngine;

public class DebugAngle : MonoBehaviour
{
    public TextMeshPro angleText; // 在 Inspector 裡手動拖進來

    public JointAngle jointAngle; // 拖入角度來源腳本
    public ClawModuleController clawModuleController;

    public TriggerRightMiddleTip rightMiddleTip;
    public TriggerRightIndexTip rightIndexTip;

    void Update()
    {
        if (angleText != null && jointAngle != null)
        {
            angleText.text = "angle is : "
                + ((int)jointAngle.thumbAngle0).ToString() + " "
                + ((int)jointAngle.thumbAngle1).ToString() + "\n"
                + ((int)jointAngle.indexAngle0).ToString() + " "
                + ((int)jointAngle.indexAngle1).ToString() + " "
                + ((int)jointAngle.indexAngle2).ToString() + "\n"
                + ((int)jointAngle.middleAngle0).ToString()
                + " " + ((int)jointAngle.middleAngle1).ToString()
                + " " + ((int)jointAngle.middleAngle2).ToString() + "\n"
                + "distance: " + jointAngle.indexMiddleDistance.ToString("F2") + "\n"
                + "RightIndexTip: " + rightIndexTip.isRightIndexTipTouched.ToString() + "\n"
                + "RightMiddleTip: " + rightMiddleTip.isRightMiddleTipTouched.ToString() + "\n" +
                "maxIndexYAxisAngle: " + clawModuleController.maxIndexYAxisAngle.ToString("F3") + "\n" +
                "tt: " + clawModuleController.tt.ToString() + "\n" +
                "joints[Index2].localRotation.eulerAngles.x: " + jointAngle.joints["Index2"].localRotation.eulerAngles.x.ToString("F4") + "°\n" +
                "jointAngleValueDebug: " + clawModuleController.jointAngleValueDebug.ToString("F4") + "°\n" +
                "currentTipRotationDebug: " + clawModuleController.currentTipRotationDebug.ToString("F4") + "°\n";
        }
    }
}

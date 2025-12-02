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
            angleText.text = "" +
                // + ((int)jointAngle.thumbAngle0).ToString() + " "
                // + ((int)jointAngle.thumbAngle1).ToString() + "\n"
                // + ((int)jointAngle.indexAngle0).ToString() + " "
                "distance: " + jointAngle.indexMiddleDistance.ToString("F2") + "\n" +
                // + ((int)jointAngle.indexAngle1).ToString() + " "
                // "indexAngle2: " + jointAngle.indexAngle2.ToString("F3") + "\n" +
                // "joints[Index2].localRotation.eulerAngles.x: " + jointAngle.joints["Index2"].localRotation.eulerAngles.x.ToString("F4") + "°\n" +
                // + ((int)jointAngle.middleAngle0).ToString()
                // + " " + ((int)jointAngle.middleAngle1).ToString()
                // "middleAngle2: " + jointAngle.middleAngle2.ToString("F3") + "\n" +
                // "joints[Middle2].localRotation.eulerAngles.x: " + jointAngle.joints["Middle2"].localRotation.eulerAngles.x.ToString("F4") + "°\n" +
                // "RightIndexTipTouched: " + rightIndexTip.isRightIndexTipTouched.ToString() + "\n" +
                // "RightMiddleTipTouched: " + rightMiddleTip.isRightMiddleTipTouched.ToString() + "\n" +
                // "maxIndexYAxisAngle: " + clawModuleController.maxIndexYAxisAngle.ToString("F3") + "\n" +
                // "tt: " + clawModuleController.tt.ToString() + "\n" +
                // "jointAngleValueDebug: " + clawModuleController.jointAngleValueDebug.ToString("F4") + "°\n" +
                // "currentTipRotationDebug: " + clawModuleController.currentTipRotationDebug.ToString("F4") + "°\n" +
                // "isPlaneActive" + jointAngle.isPlaneActive.ToString() + "\n" +
                // "isClockWise: " + jointAngle.isClockWise.ToString() + "\n";
                "jointAngle.isClockWise: " + jointAngle.isClockWise.ToString() + "\n" +
                "maxMiddleZAxisAngle: " + clawModuleController.maxMiddleZAxisAngle.ToString("F3") + "\n" +
                "currentMiddleRotationZ: " + clawModuleController.currentMiddleRotationZ.ToString("F3") + "\n" + 
                "thumbPalmAngle: " + jointAngle.thumbPalmAngle.ToString("F2") + "\n";
        }
    }
}

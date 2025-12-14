using TMPro; // 引入 TextMeshPro 命名空間
using UnityEngine;

public class DebugAngle : MonoBehaviour
{
    public TextMeshPro angleText; // 在 Inspector 裡手動拖進來

    public JointAngle jointAngle; // 拖入角度來源腳本
    public ClawModuleController clawModuleController;

    public TriggerRightThumbTip triggerRightThumbTip;
    public TriggerRightIndexTip rightIndexTip;
    public TriggerRightMiddleTip rightMiddleTip;

    void Update()
    {
        if (angleText != null && jointAngle != null)
        {
            angleText.text = "" +
                // + ((int)jointAngle.thumbAngle0).ToString() + " "
                // + ((int)jointAngle.thumbAngle1).ToString() + "\n"
                // + ((int)jointAngle.indexAngle0).ToString() + " "
                // "currentThumbRotationZ" + clawModuleController.currentThumbRotationZ.ToString("F3") + "\n" +
                "isRightThumbTipTouched" + triggerRightThumbTip.isRightThumbTipTouched.ToString() + "\n" +
                "isRightIndexTipTouched: " + rightIndexTip.isRightIndexTipTouched.ToString() + "\n" +
                "isRightMiddleTipTouched" + rightMiddleTip.isRightMiddleTipTouched.ToString() + "\n" +
                "distance: " + jointAngle.indexMiddleDistance.ToString("F2") + "\n" +
                "Index0 angle" +  jointAngle.joints["Index0"].localRotation.eulerAngles.z.ToString("F4") + "°\n" 
                + "Index1 angle" + jointAngle.joints["Index1"].localRotation.eulerAngles.z.ToString("F4") + "°\n" +
                "Index2 angle" + jointAngle.joints["Index2"].localRotation.eulerAngles.x.ToString("F4") + "°\n" +
                // "wristThumbAngle" + jointAngle.wristThumbAngle.ToString("F2") + "\n" +
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
                // "jointAngle.isClockWise: " + jointAngle.isClockWise.ToString() + "\n" +
                // "maxMiddleZAxisAngle: " + clawModuleController.maxMiddleZAxisAngle.ToString("F3") + "\n" +
                // "currentMiddleRotationZ: " + clawModuleController.currentMiddleRotationZ.ToString("F3") + "\n" + 
                // "thumbPalmAngle: " + jointAngle.thumbPalmAngle.ToString("F2") + "\n";
                // "isThumb1Triggered: " + clawModuleController.isThumb1Triggered.ToString() + "\n" +
                // "isThumb2Triggered: " + clawModuleController.isThumb2Triggered.ToString() + "\n" +
                // "isThumb4Triggered: " + clawModuleController.isThumb4Triggered.ToString() + "\n" +
                // "isIndex1Triggered: " + clawModuleController.isIndex1Triggered.ToString() + "\n" +
                // "isIndex2Triggered: " + clawModuleController.isIndex2Triggered.ToString() + "\n" +
                // "isIndex4Triggered: " + clawModuleController.isIndex4Triggered.ToString() + "\n" +
                // "isMiddle1Triggered: " + clawModuleController.isMiddle1Triggered.ToString() + "\n" +
                // "isMiddle2Triggered: " + clawModuleController.isMiddle2Triggered.ToString() + "\n" +
                // "isMiddle4Triggered: " + clawModuleController.isMiddle4Triggered.ToString() + "\n" +
                // "isAnyMotorTriggered" + clawModuleController.isAnyMotorTriggered.ToString() + "\n"+
                // "isRightIndexTwistTouched: " + clawModuleController.triggerRightIndexTwist.isRightIndexTwistTouched.ToString() + "\n";
                "thumbPalmAngle " + jointAngle.thumbPalmAngle.ToString("F2") + "°\n" +
                "wristThumbAngle " + jointAngle.wristThumbAngle.ToString("F2") + "°\n" +
                // "(int)clawModuleController.ThumbAngle1Center.localRotation.eulerAngles.y: " + ((int)clawModuleController.ThumbAngle1Center.localRotation.eulerAngles.y).ToString() + "°\n" +
                // "(int)clawModuleController.MiddleAngle4Center.localRotation.eulerAngles.x: " + ((int)clawModuleController.MiddleAngle4Center.localRotation.eulerAngles.x).ToString() + "°\n";
                // "(int)clawModuleController.ThumbAngle2Center.localRotation.eulerAngles.z: " + ((int)clawModuleController.ThumbAngle2Center.localRotation.eulerAngles.z).ToString() + "°\n";
                // "(int)clawModuleController.MiddleAngle1Center.localRotation.eulerAngles.y: " + ((int)clawModuleController.MiddleAngle1Center.localRotation.eulerAngles.y).ToString() + "°\n" +
                // "(int)clawModuleController.MiddleAngle2Center.localRotation.eulerAngles.z,: " + ((int)clawModuleController.MiddleAngle2Center.localRotation.eulerAngles.z).ToString() + "°\n";
                "(int)clawModuleController.IndexAngle4Center.localRotation.eulerAngles.x: " + ((int)clawModuleController.IndexAngle4Center.localRotation.eulerAngles.x).ToString() + "°\n" +
                "(int)clawModuleController.MiddleAngle4Center.localRotation.eulerAngles.x: " + ((int)clawModuleController.MiddleAngle4Center.localRotation.eulerAngles.x).ToString() + "°\n";
        }
    }
}

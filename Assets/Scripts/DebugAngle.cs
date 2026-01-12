using TMPro; // Import TextMeshPro namespace
using UnityEngine;

public class DebugAngle : MonoBehaviour
{
    public TextMeshPro angleText; // Manually drag into Inspector
    public JointAngle jointAngle; // Drag in the angle source script
    public ClawModuleController clawModuleController;
    public TriggerRightThumbTip triggerRightThumbTip;
    public TriggerRightIndexTip rightIndexTip;
    public TriggerRightMiddleTip rightMiddleTip;
    public TriggerIndexInnerExtension triggerIndexInnerExtension;
    public TriggerThumbInnerExtension triggerThumbInnerExtension;
    public Transform clawIndexFingerTip;
    public Transform R_IndexTriggerTip;
    public ThreeFingerCollisionDetector threeFingerCollisionDetector;
    public RetargetIndex retargetIndex;

    void Update()
    {
        if (angleText != null && jointAngle != null)
        {
            angleText.text = "" +
                // + ((int)jointAngle.thumbAngle0).ToString() + " "
                // + ((int)jointAngle.thumbAngle1).ToString() + "\n"
                // + ((int)jointAngle.indexAngle0).ToString() + " "
                // "currentThumbRotationZ" + clawModuleController.currentThumbRotationZ.ToString("F3") + "\n" +
                // "isRightThumbTipTouched" + triggerRightThumbTip.isRightThumbTipTouched.ToString() + "\n" +
                // "isRightIndexTipTouched: " + rightIndexTip.isRightIndexTipTouched.ToString() + "\n" +
                // "isRightMiddleTipTouched" + rightMiddleTip.isRightMiddleTipTouched.ToString() + "\n" +
                // "distance: " + jointAngle.indexMiddleDistance.ToString("F2") + "\n" +
                // "Thumb0 angle" +  jointAngle.joints["Thumb0"].localRotation.eulerAngles.z.ToString("F4") + "°\n" +
                // "Thumb1 angle" + jointAngle.joints["Thumb1"].localRotation.eulerAngles.z.ToString("F4") + "°\n" +
                // "Index0 angle" +  jointAngle.joints["Index0"].localRotation.eulerAngles.z.ToString("F4") + "°\n" +
                // "Index1 angle" + jointAngle.joints["Index1"].localRotation.eulerAngles.z.ToString("F4") + "°\n" +
                // "Index2 angle" + jointAngle.joints["Index2"].localRotation.eulerAngles.z.ToString("F4") + "°\n" +
                "Middle0 angle" +  jointAngle.joints["Middle0"].localRotation.eulerAngles.z.ToString("F4") + "°\n" +
                "Middle1 angle" + jointAngle.joints["Middle1"].localRotation.eulerAngles.z.ToString("F4") + "°\n" +
                "Middle2 angle" + jointAngle.joints["Middle2"].localRotation.eulerAngles.z.ToString("F4") + "°\n" +
                // "totalAngleChange: " + clawModuleController.totalAngleChange.ToString("F4") + "°\n" +
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
                // "isIndexInnerExtensionTouched: " + triggerIndexInnerExtension.isIndexInnerExtensionTouched.ToString() + "\n" +
                // "wristThumbAngle " + jointAngle.wristThumbAngle.ToString("F2") + "°\n" +
                // "initialThumbPalmCrossProduct" + jointAngle.initialThumbPalmCrossProduct.ToString("F2") + "°\n" + 
                // "isThumbInnerExtensionTouched: " + triggerIndexInnerExtension.isIndexInnerExtensionTouched.ToString() + "\n" +
                // "currentIndexInnerExtensionRotationZ: " + clawModuleController.currentIndexInnerExtensionRotationZ.ToString("F3") + "°\n" +
                // "currentIndexTipRotationZ: " + clawModuleController.currentIndexTipRotationZ.ToString("F3") + "°\n" +
                // "isThumbInnerExtensionTouched: " + triggerThumbInnerExtension.isThumbInnion.eulerAngles.x: " + jointAngle.joints["Wrist"].localEulerAngles.x.ToString("F4") + "°\n" +
                // "Wrist.rotation.eulerAngles.x: " + jointAngle.joints["Wrist"].eulerAngles.x.ToString("F4") + "°\n" +
                // "Elbow.localRotation.eulerAngles.x: " + jointAngle.joints["Elbow"].localEulerAngles.x.ToString("F4") + "°\n" +
                // "Elbow.rotation.eulerAngles.x: " + jointAngle.joints["Elbow"].eulerAngles.x.ToString("F4") + "°\n" +
                "thumbAngle0: " + jointAngle.thumbAngle0.ToString("F2") + "°\n" +
                "clawIndexFingerTip.Transform" + clawIndexFingerTip.position.ToString("F4") + "\n" +
                "R_IndexTriggerTip.Transform" + R_IndexTriggerTip.position.ToString("F4") + "\n" +
                "thumbPalmAngle " + jointAngle.thumbPalmAngle.ToString("F2") + "°\n" +
                "IndexAngle1Center.localRotation.eulerAngles.y: " + clawModuleController.IndexAngle1Center.localEulerAngles.y.ToString("F4") + "°\n" +
                "MiddleAngle1Center.localRotation.eulerAngles.y: " + clawModuleController.MiddleAngle1Center.localEulerAngles.y.ToString("F4") + "°\n" +
                "hasRecordedPositions: " + retargetIndex.hasRecordedPositions.ToString() + "\n" +
                "gripperIndexTip position: " + retargetIndex.gripperIndexTip.position.ToString("F4") + "\n" +
                "publiAaverageRotation > 0.5 :" + jointAngle.publiAaverageRotation.ToString("F4") + "\n" +
                "cumulativeRotation > 0.02 :" + jointAngle.cumulativeRotation.ToString("F4") + "\n" +
                "rotationChangeTimer > 0.3 :" + jointAngle.rotationChangeTimer.ToString("F4") + "\n" +
                "isClockWise :" + jointAngle.isClockWise.ToString() + "\n" +
                // "colliders[1] position: " + threeFingerCollisionDetector.colliders[1].position.ToString("F4") + "\n" +
                // "colliders[3] position: " + threeFingerCollisionDetector.colliders[3].position.ToString("F4") + "\n" +
                // "d[1,3]: " + threeFingerCollisionDetector.allDistances[1, 3].ToString("F2");
                // "joints[Index0].localRotation.eulerAngles.z: " + jointAngle.joints["Index0"].localEulerAngles.z.ToString("F4") + "°\n";
                // "indexAngle0: " + jointAngle.indexAngle0.ToString("F2") + "°\n";
                // "joints[Middle0].localRerExtensionTouched.ToString() + "\n" +
                "isRightThumbTipTouched: " + triggerRightThumbTip.isRightThumbTipTouched.ToString() + "\n";
                // "(int)clawModuleController.ThumbAngle1Center.localRotation.eulerAngles.y: " + ((int)clawModuleController.ThumbAngle1Center.localRotation.eulerAngles.y).ToString() + "°\n" +
                // "(int)clawModuleController.MiddleAngle4Center.localRotation.eulerAngles.x: " + ((int)clawModuleController.MiddleAngle4Center.localRotation.eulerAngles.x).ToString() + "°\n";
                // "(int)clawModuleController.ThumbAngle2Center.localRotation.eulerAngles.z: " + ((int)clawModuleController.ThumbAngle2Center.localRotation.eulerAngles.z).ToString() + "°\n";
                // "(int)clawModuleController.MiddleAngle1Center.localRotation.eulerAngles.y: " + ((int)clawModuleController.MiddleAngle1Center.localRotation.eulerAngles.y).ToString() + "°\n" +
                // "(int)clawModuleController.MiddleAngle2Center.localRotation.eulerAngles.z,: " + ((int)clawModuleController.MiddleAngle2Center.localRotation.eulerAngles.z).ToString() + "°\n";
                // "(int)clawModuleController.IndexAngle4Center.localRotation.eulerAngles.x: " + ((int)clawModuleController.IndexAngle4Center.localRotation.eulerAngles.x).ToString() + "°\n" +
                // "(int)clawModuleController.MiddleAngle4Center.localRotation.eulerAngles.x: " + ((int)clawModuleController.MiddleAngle4Center.localRotation.eulerAngles.x).ToString() + "°\n";
                // "Wrist.localRotatotation.eulerAngles.z: " + jointAngle.joints["Middle0"].localEulerAngles.z.ToString("F4") + "°\n";

        }
    }
}

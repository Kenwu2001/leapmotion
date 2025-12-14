using System.Text;
using UnityEngine;
using System.Collections;

public class SerialSender : MonoBehaviour {
    public JointAngle jointAngle;
    private SerialManager serialmanager;
    public float updateFrequency = 20f; // 20 updates per second
    public ClawModuleController clawModuleController;
    
    private void Awake()
    {
        serialmanager = GetComponent<SerialManager>();
    }

    private void Start()
    {
        StartCoroutine(SendDataCoroutine());
    }
    
    void Update()
    {
        // Debug.Log("(int)clawModuleController.IndexAngle4Center.localRotation.eulerAngles.x: " + (int)clawModuleController.IndexAngle4Center.localRotation.eulerAngles.x);
    }

    IEnumerator SendDataCoroutine() {
        while(true) {
            if (serialmanager != null && serialmanager.serialPort != null && serialmanager.serialPort.IsOpen) {
                // Format data with all angle variables including LR angles
                string dataString = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13}\n",
                    0,
                    (int)clawModuleController.ThumbAngle1Center.localRotation.eulerAngles.y,
                    (int)clawModuleController.ThumbAngle2Center.localRotation.eulerAngles.z,
                    (int)clawModuleController.ThumbAngle3Center.localRotation.eulerAngles.x,
                    (int)clawModuleController.ThumbAngle4Center.localRotation.eulerAngles.x,
                    (int)clawModuleController.IndexAngle1Center.localRotation.eulerAngles.y,
                    (int)clawModuleController.IndexAngle2Center.localRotation.eulerAngles.z,
                    (int)clawModuleController.IndexAngle3Center.localRotation.eulerAngles.x,
                    (int)clawModuleController.IndexAngle4Center.localRotation.eulerAngles.x,
                    (int)clawModuleController.MiddleAngle1Center.localRotation.eulerAngles.y,
                    (int)clawModuleController.MiddleAngle2Center.localRotation.eulerAngles.z,
                    (int)clawModuleController.MiddleAngle3Center.localRotation.eulerAngles.x,
                    (int)clawModuleController.MiddleAngle4Center.localRotation.eulerAngles.x,
                    0
                );

                // string dataString = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13}\n",
                //     0,
                //     (int)clawModuleController.currentThumbRotationY,  // index 1: thumb abduction (-60 to 0)
                //     (int)clawModuleController.currentThumbRotationZ,  // index 2: thumb twist (-60 to 0)
                //     (int)clawModuleController.ThumbAngle3Center.localRotation.eulerAngles.x,  // index 3: thumb bend
                //     (int)clawModuleController.currentThumbTipRotationZ,  // index 4: thumb tip extension
                //     (int)clawModuleController.currentIndexRotationY,  // index 5: index abduction (-60 to 0)
                //     (int)clawModuleController.currentIndexRotationZ,  // index 6: index twist (-58 to 0)
                //     (int)clawModuleController.IndexAngle3Center.localRotation.eulerAngles.x,  // index 7: index bend
                //     (int)clawModuleController.currentIndexTipRotationZ,  // index 8: index tip extension
                //     (int)clawModuleController.currentMiddleRotationY,  // index 9: middle abduction (0 to 60)
                //     (int)clawModuleController.currentMiddleRotationZ,  // index 10: middle twist (0 to 58)
                //     (int)clawModuleController.MiddleAngle3Center.localRotation.eulerAngles.x,  // index 11: middle bend
                //     (int)clawModuleController.currentMiddleTipRotationZ,  // index 12: middle tip extension
                //     0
                // );
                
                // Debug.Log($"Sending data: {dataString}");
                
                serialmanager.serialPort.WriteLine(dataString);
            }
            
            yield return new WaitForSeconds(1f/updateFrequency);
        }
    }
}
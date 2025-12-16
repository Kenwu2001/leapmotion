using System.Text;
using UnityEngine;
using System.Collections;

public class SerialSender : MonoBehaviour {
    public JointAngle jointAngle;
    private SerialManager serialmanager;
    public float updateFrequency = 20f; // 20 updates per second
    public ClawModuleController clawModuleController;

    public DeltaUserStudy deltaUserStudy;
    
    private void Awake()
    {
        serialmanager = GetComponent<SerialManager>();
    }

    private void Start()
    {
        StartCoroutine(SendDataCoroutine());
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
                //     (int)deltaUserStudy.ThumbAngle1Center.localRotation.eulerAngles.y,
                //     (int)deltaUserStudy.ThumbAngle2Center.localRotation.eulerAngles.z,
                //     (int)deltaUserStudy.ThumbAngle3Center.localRotation.eulerAngles.x,
                //     (int)deltaUserStudy.ThumbAngle4Center.localRotation.eulerAngles.x,
                //     (int)deltaUserStudy.IndexAngle1Center.localRotation.eulerAngles.y,
                //     (int)deltaUserStudy.IndexAngle2Center.localRotation.eulerAngles.z,
                //     (int)deltaUserStudy.IndexAngle3Center.localRotation.eulerAngles.x,
                //     (int)deltaUserStudy.IndexAngle4Center.localRotation.eulerAngles.x,
                //     (int)deltaUserStudy.MiddleAngle1Center.localRotation.eulerAngles.y,
                //     (int)deltaUserStudy.MiddleAngle2Center.localRotation.eulerAngles.z,
                //     (int)deltaUserStudy.MiddleAngle3Center.localRotation.eulerAngles.x,
                //     (int)deltaUserStudy.MiddleAngle4Center.localRotation.eulerAngles.x,
                //     0
                // );
                
                serialmanager.serialPort.WriteLine(dataString);
            }
            
            yield return new WaitForSeconds(1f/updateFrequency);
        }
    }
}
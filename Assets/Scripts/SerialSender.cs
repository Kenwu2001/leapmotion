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
        Debug.Log("(int)clawModuleController.IndexAngle4Center.localRotation.eulerAngles.x: " + (int)clawModuleController.IndexAngle4Center.localRotation.eulerAngles.x);
    }

    IEnumerator SendDataCoroutine() {
        while(true) {
            if (serialmanager != null && serialmanager.serialPort != null && serialmanager.serialPort.IsOpen) {
                // Format data with all angle variables including LR angles
                string dataString = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13}\n",
                    0,
                    (int)clawModuleController.ThumbAngle1Center.localRotation.eulerAngles.y, // thumb first joint
                    // (int)jointAngle.GetJoint("Thumb0").localRotation.eulerAngles.x, // Use safe accessor
                    (int)clawModuleController.ThumbAngle2Center.localRotation.eulerAngles.z,
                    (int)clawModuleController.ThumbAngle3Center.localRotation.eulerAngles.x,
                    // (int)Mathf.Clamp(jointAngle.thumbAngle1, 0, 180),
                    (int)clawModuleController.ThumbAngle4Center.localRotation.eulerAngles.x,
                    (int)clawModuleController.IndexAngle1Center.localRotation.eulerAngles.y,
                    (int)clawModuleController.IndexAngle2Center.localRotation.eulerAngles.z,
                    (int)clawModuleController.IndexAngle3Center.localRotation.eulerAngles.x,
                    // (int)Mathf.Clamp(jointAngle.indexAngle1, 0, 180),
                    // (int)Mathf.Clamp(jointAngle.indexAngle2, 0, 180),
                    (int)clawModuleController.IndexAngle4Center.localRotation.eulerAngles.x,
                    // 30,
                    (int)clawModuleController.MiddleAngle1Center.localRotation.eulerAngles.y,
                    (int)clawModuleController.MiddleAngle2Center.localRotation.eulerAngles.z,
                    (int)clawModuleController.MiddleAngle3Center.localRotation.eulerAngles.x,
                    // (int)Mathf.Clamp(jointAngle.middleAngle1, 0, 180),
                    // (int)Mathf.Clamp(jointAngle.middleAngle2, 0, 180),
                    (int)clawModuleController.MiddleAngle4Center.localRotation.eulerAngles.x,
                    0
                );
                
                Debug.Log($"Sending data: {dataString}");
                
                serialmanager.serialPort.WriteLine(dataString);
            }
            
            yield return new WaitForSeconds(1f/updateFrequency);
        }
    }
}
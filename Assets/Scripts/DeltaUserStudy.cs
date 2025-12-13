using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeltaUserStudy : MonoBehaviour
{
    public JointAngle jointAngle;

    public Transform ThumbAngle1Center;
    public Transform ThumbAngle2Center;
    public Transform ThumbAngle3Center;
    public Transform ThumbAngle4Center;

    public Transform IndexAngle1Center;
    public Transform IndexAngle2Center;
    public Transform IndexAngle3Center;
    public Transform IndexAngle4Center;

    public Transform MiddleAngle1Center;
    public Transform MiddleAngle2Center;
    public Transform MiddleAngle3Center;
    public Transform MiddleAngle4Center;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (ThumbAngle3Center != null)
            ThumbAngle3Center.localRotation = Quaternion.Euler(jointAngle.thumbAngle1 + 10, 0f, 0f);

        if (ThumbAngle4Center != null)
            ThumbAngle4Center.localRotation = Quaternion.Euler(jointAngle.thumbAngle1 + 10, 0f, 0f);

        if (IndexAngle3Center != null)
            IndexAngle3Center.localRotation = Quaternion.Euler(jointAngle.indexAngle1, 0f, 0f);

        if (IndexAngle4Center != null)
            IndexAngle4Center.localRotation = Quaternion.Euler(jointAngle.indexAngle2, 0f, 0f);

        if (MiddleAngle3Center != null)
            MiddleAngle3Center.localRotation = Quaternion.Euler(jointAngle.middleAngle1, 0f, 0f);

        if (MiddleAngle4Center != null)
            MiddleAngle4Center.localRotation = Quaternion.Euler(jointAngle.middleAngle2, 0f, 0f);
    }
}

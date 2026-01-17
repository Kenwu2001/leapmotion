using UnityEngine;

[RequireComponent(typeof(FingerPath))]
public class FingerLineRenderer : MonoBehaviour
{
    public LineRenderer line;
    private FingerPath finger;

    void Awake()
    {
        finger = GetComponent<FingerPath>();
    }

    void LateUpdate()
    {
        if (line == null) return;

        line.positionCount = 5;
        for (int i = 0; i < 5; i++)
        {
            line.SetPosition(i, finger.GetJoint(i));
        }
    }
}
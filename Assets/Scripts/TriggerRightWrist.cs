using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerRightWrist : MonoBehaviour
{
    public TcpSender tcpSender;
    // public string leftPinkyName = "L_PinkyTip";
    public string leftIndexTipName = "L_IndexTip";
    public bool isRightWristTouched = false;
    public GameObject indicatorQuad; // The quad to show/hide

    private void Start()
    {
        if (indicatorQuad != null)
        {
            indicatorQuad.SetActive(false); // Hide at start
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(leftIndexTipName))
        {
            isRightWristTouched = true;
            if (tcpSender != null)
            {
                tcpSender.isSending = !tcpSender.isSending;
                
                if (indicatorQuad != null)
                {
                    indicatorQuad.SetActive(tcpSender.isSending);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(leftIndexTipName))
        {
            isRightWristTouched = false;
        }
    }
}

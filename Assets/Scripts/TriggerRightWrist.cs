using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerRightWrist : MonoBehaviour
{
    public TcpSender tcpSender;
    public string leftPinkyName = "L_PinkyTip";
    public bool isRightWristTouched = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(leftPinkyName))
        {
            isRightWristTouched = true;
            if (tcpSender != null)
            {
                tcpSender.isSending = !tcpSender.isSending;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(leftPinkyName))
        {
            isRightWristTouched = false;
        }
    }
}

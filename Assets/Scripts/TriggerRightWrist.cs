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
    [Tooltip("Minimum interval between engagement toggles to resist tracking jitter.")]
    public float toggleCooldownSeconds = 0.35f;

    private readonly HashSet<Collider> touchingIndexColliders = new HashSet<Collider>();
    private float lastToggleTime = -999f;

    public bool IsEngaged
    {
        get
        {
            return tcpSender != null && tcpSender.isSending;
        }
    }

    private void Start()
    {
        SyncIndicatorWithSender();
    }

    private void Update()
    {
        SyncIndicatorWithSender();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(leftIndexTipName))
        {
            return;
        }

        if (!touchingIndexColliders.Add(other))
        {
            return;
        }

        isRightWristTouched = touchingIndexColliders.Count > 0;

        // Only toggle on the first collider entering, not every collider fragment.
        if (touchingIndexColliders.Count == 1)
        {
            TryToggleEngagement();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(leftIndexTipName))
        {
            return;
        }

        touchingIndexColliders.Remove(other);
        isRightWristTouched = touchingIndexColliders.Count > 0;
    }

    private void TryToggleEngagement()
    {
        if (tcpSender == null)
        {
            return;
        }

        if (Time.unscaledTime - lastToggleTime < toggleCooldownSeconds)
        {
            return;
        }

        lastToggleTime = Time.unscaledTime;
        tcpSender.ToggleEngagement("wrist_touch");
        SyncIndicatorWithSender();
    }

    private void SyncIndicatorWithSender()
    {
        if (indicatorQuad == null)
        {
            return;
        }

        bool engaged = tcpSender != null && tcpSender.isSending;
        if (indicatorQuad.activeSelf != engaged)
        {
            indicatorQuad.SetActive(engaged);
        }
    }
}

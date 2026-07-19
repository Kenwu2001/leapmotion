using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerRightWrist : MonoBehaviour
{
    public TcpSender tcpSender;
    // public string leftPinkyName = "L_PinkyTip";
    // public string leftIndexTipName = "L_IndexTip";
    public string leftIndexTipName = "ToyHand";
    public bool isRightWristTouched = false;
    public GameObject indicatorQuad; // The quad to show/hide
    [Header("Wrist Touch Visual")]
    public Renderer wristTouchRenderer;
    public Material wristTouchedMaterial;
    public Material wristNotTouchedMaterial;
    [Tooltip("Minimum interval between engagement toggles to resist tracking jitter.")]
    public float toggleCooldownSeconds = 0.35f;

    private readonly HashSet<Collider> touchingIndexColliders = new HashSet<Collider>();
    private float lastToggleTime = -999f;

    public GameObject engagementSphere;
    public Renderer engagementSphereRenderer;

    public bool IsEngaged
    {
        get
        {
            return tcpSender != null && tcpSender.isSending;
        }
    }

    private void Start()
    {
        if (engagementSphereRenderer == null && engagementSphere != null)
        {
            engagementSphereRenderer = engagementSphere.GetComponent<Renderer>();
        }

        SyncIndicatorWithSender();
        SyncWristTouchMaterial();
    }

    private void Update()
    {
        SyncIndicatorWithSender();
        SyncWristTouchMaterial();
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
        SyncWristTouchMaterial();

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
        SyncWristTouchMaterial();
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
        bool engaged = tcpSender != null && tcpSender.isSending;

        // if (indicatorQuad != null && indicatorQuad.activeSelf != engaged)
        // {
        //     indicatorQuad.SetActive(engaged);
        // }

        SyncWristTouchMaterial(engaged);
    }

    private void SyncWristTouchMaterial()
    {
        SyncWristTouchMaterial(tcpSender != null && tcpSender.isSending);
    }

    private void SyncWristTouchMaterial(bool engaged)
    {
        Material targetMaterial = engaged ? wristTouchedMaterial : wristNotTouchedMaterial;
        if (targetMaterial == null)
        {
            return;
        }

        ApplyMaterial(wristTouchRenderer, targetMaterial);
        ApplyMaterial(engagementSphereRenderer, targetMaterial);
    }

    private static void ApplyMaterial(Renderer targetRenderer, Material targetMaterial)
    {
        if (targetRenderer == null || targetRenderer.sharedMaterial == targetMaterial)
        {
            return;
        }

        targetRenderer.material = targetMaterial;
    }
}

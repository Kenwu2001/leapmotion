using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ArmUIPlaneCollider : MonoBehaviour
{
    [System.Serializable]
    public class ButtonBinding
    {
        public string buttonName = "Button";
        public Collider buttonCollider;
        public bool isTouched;
        public UnityEvent onEnter;
        public UnityEvent onExit;

        public void Initialize()
        {
            // Intentionally empty: collider is assigned from Inspector.
        }
    }

    [Header("Button Setup")]
    public ButtonBinding armUIAreaButton = new ButtonBinding { buttonName = "armUIAreaButton" };

    [Header("State")]
    public bool inArmUIArea = false;

    [Header("Debug")]
    public string currentTouchedButton = "None";
    public string interactionDebug = "No button touched";
    public string lastTouchedColliderName = "None";

    private void Reset()
    {
        Collider selfCollider = GetComponent<Collider>();
        if (selfCollider != null)
        {
            selfCollider.isTrigger = true;
        }
    }

    private void Awake()
    {
        if (armUIAreaButton.buttonName == "Button")
        {
            armUIAreaButton.buttonName = "armUIAreaButton";
        }

        InitializeButton(armUIAreaButton);
        SyncAreaState();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHandleEnter(armUIAreaButton, other);
    }

    private void OnTriggerExit(Collider other)
    {
        TryHandleExit(armUIAreaButton, other);
    }

    private void InitializeButton(ButtonBinding button)
    {
        if (button == null)
        {
            return;
        }

        button.Initialize();
    }

    private bool TryHandleEnter(ButtonBinding button, Collider other)
    {
        if (button == null || button.buttonCollider == null || other != button.buttonCollider)
        {
            return false;
        }

        if (!button.isTouched)
        {
            button.isTouched = true;
            currentTouchedButton = button.buttonName;
            lastTouchedColliderName = other.name;
            interactionDebug = "Touch enter: " + button.buttonName;
            button.onEnter?.Invoke();
            SyncAreaState();
        }

        return true;
    }

    private bool TryHandleExit(ButtonBinding button, Collider other)
    {
        if (button == null || button.buttonCollider == null || other != button.buttonCollider)
        {
            return false;
        }

        if (button.isTouched)
        {
            button.isTouched = false;
            interactionDebug = "Touch exit: " + button.buttonName;
            button.onExit?.Invoke();
            SyncAreaState();
        }

        return true;
    }

    private void SyncAreaState()
    {
        inArmUIArea = armUIAreaButton != null && armUIAreaButton.isTouched;

        if (inArmUIArea)
        {
            currentTouchedButton = armUIAreaButton.buttonName;
            return;
        }

        currentTouchedButton = "None";
        lastTouchedColliderName = "None";
        if (interactionDebug.StartsWith("Touch"))
        {
            return;
        }

        interactionDebug = "No button touched";
    }
}

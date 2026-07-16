using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ArmUIPlaneCollider : MonoBehaviour
{
    private const float AreaStayTimeoutSeconds = 0.25f;

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
    public bool inArmUIArea = false; // the bigger zone, judging if the hand is in the arm UI area, if so, the toyhand should appear, otherwise, the toyhand should disappear.

    [Header("Debug")]
    public string currentTouchedButton = "None";
    public string interactionDebug = "No button touched";
    public string lastTouchedColliderName = "None";

    private float _lastAreaTouchTime = -1f;

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

    private void Update()
    {
        if (armUIAreaButton == null || !armUIAreaButton.isTouched)
        {
            return;
        }

        // If enter/exit events are missed and no stay heartbeat is received,
        // force clear to prevent sticky inArmUIArea state.
        if (_lastAreaTouchTime > 0f && (Time.time - _lastAreaTouchTime) > AreaStayTimeoutSeconds)
        {
            ForceClearAreaTouch("Touch timeout");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHandleEnter(armUIAreaButton, other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (armUIAreaButton == null || armUIAreaButton.buttonCollider == null)
        {
            return;
        }

        if (other != armUIAreaButton.buttonCollider)
        {
            return;
        }

        _lastAreaTouchTime = Time.time;
        lastTouchedColliderName = other.name;
    }

    private void OnTriggerExit(Collider other)
    {
        TryHandleExit(armUIAreaButton, other);
    }

    private void OnDisable()
    {
        ForceClearAreaTouch("Collider disabled");
    }

    private void OnDestroy()
    {
        ForceClearAreaTouch("Collider destroyed");
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
            _lastAreaTouchTime = Time.time;
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
            _lastAreaTouchTime = -1f;
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

    private void ForceClearAreaTouch(string reason)
    {
        if (armUIAreaButton == null)
        {
            inArmUIArea = false;
            return;
        }

        if (!armUIAreaButton.isTouched)
        {
            inArmUIArea = false;
            _lastAreaTouchTime = -1f;
            return;
        }

        armUIAreaButton.isTouched = false;
        _lastAreaTouchTime = -1f;
        interactionDebug = "Force clear: " + reason;
        armUIAreaButton.onExit?.Invoke();
        SyncAreaState();
    }
}

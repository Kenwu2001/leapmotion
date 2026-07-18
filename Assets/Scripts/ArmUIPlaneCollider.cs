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

    public ButtonBinding openArmUIAreaButton = new ButtonBinding { buttonName = "openArmUIAreaButton" };

    [Header("State")]
    public bool inArmUIArea = false; // the bigger zone, judging if the hand is in the arm UI area, if so, the toyhand should appear, otherwise, the toyhand should disappear.

    [Header("Debug")]
    public string currentTouchedButton = "None";
    public string interactionDebug = "No button touched";
    public string lastTouchedColliderName = "None";

    private float _lastAreaTouchTime = -1f;
    private bool _openAreaLatched;

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

        if (openArmUIAreaButton.buttonName == "Button")
        {
            openArmUIAreaButton.buttonName = "openArmUIAreaButton";
        }

        InitializeButton(armUIAreaButton);
        InitializeButton(openArmUIAreaButton);
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
        TryHandleEnter(openArmUIAreaButton, other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (armUIAreaButton != null && armUIAreaButton.buttonCollider != null && other == armUIAreaButton.buttonCollider)
        {
            _lastAreaTouchTime = Time.time;
            lastTouchedColliderName = other.name;
            return;
        }

        if (openArmUIAreaButton != null && openArmUIAreaButton.buttonCollider != null && other == openArmUIAreaButton.buttonCollider)
        {
            lastTouchedColliderName = other.name;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        TryHandleExit(armUIAreaButton, other);
        TryHandleExit(openArmUIAreaButton, other);
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

            if (button == openArmUIAreaButton && IsButtonTouched(armUIAreaButton))
            {
                _openAreaLatched = true;
            }
            else if (button == armUIAreaButton && IsButtonTouched(openArmUIAreaButton))
            {
                // Handle possible event ordering where open enter arrives before area enter.
                _openAreaLatched = true;
            }

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
        bool areaTouched = IsButtonTouched(armUIAreaButton);
        if (!areaTouched)
        {
            _openAreaLatched = false;
            inArmUIArea = false;
            currentTouchedButton = "None";
            lastTouchedColliderName = "None";
            if (interactionDebug.StartsWith("Touch"))
            {
                return;
            }

            interactionDebug = "No button touched";
            return;
        }

        if (!_openAreaLatched && IsButtonTouched(openArmUIAreaButton))
        {
            _openAreaLatched = true;
        }

        inArmUIArea = _openAreaLatched;

        if (inArmUIArea)
        {
            currentTouchedButton = openArmUIAreaButton != null && openArmUIAreaButton.isTouched
                ? openArmUIAreaButton.buttonName
                : armUIAreaButton.buttonName;
            return;
        }

        currentTouchedButton = armUIAreaButton.buttonName + " (waiting " +
            (openArmUIAreaButton != null ? openArmUIAreaButton.buttonName : "open") + ")";
    }

    private void ForceClearAreaTouch(string reason)
    {
        bool wasTouched = false;

        if (armUIAreaButton != null && armUIAreaButton.isTouched)
        {
            armUIAreaButton.isTouched = false;
            armUIAreaButton.onExit?.Invoke();
            wasTouched = true;
        }

        if (openArmUIAreaButton != null && openArmUIAreaButton.isTouched)
        {
            openArmUIAreaButton.isTouched = false;
            openArmUIAreaButton.onExit?.Invoke();
            wasTouched = true;
        }

        _openAreaLatched = false;
        _lastAreaTouchTime = -1f;
        if (wasTouched)
        {
            interactionDebug = "Force clear: " + reason;
        }

        SyncAreaState();
    }

    private bool IsButtonTouched(ButtonBinding button)
    {
        return button != null && button.isTouched;
    }
}

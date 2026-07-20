using UnityEngine;

public class DeltaUserStudyPlaneButtonInteraction : MonoBehaviour
{
    [System.Serializable]
    public class ButtonBinding
    {
        public string buttonName = "Button";
        public GameObject buttonObject;
        public Collider buttonCollider;
        public Renderer buttonRenderer;
        public Color activeColor = Color.green;
        public bool isTouched;

        [HideInInspector] public Color originalColor;

        public void Initialize()
        {
            if (buttonObject != null)
            {
                if (buttonCollider == null)
                {
                    buttonCollider = buttonObject.GetComponent<Collider>();
                }

                if (buttonRenderer == null)
                {
                    buttonRenderer = buttonObject.GetComponent<Renderer>();
                }
            }

            if (buttonRenderer == null && buttonCollider != null)
            {
                buttonRenderer = buttonCollider.GetComponent<Renderer>();
            }

            if (buttonRenderer != null)
            {
                originalColor = buttonRenderer.material.color;
                ApplyVisual(false);
            }
        }

        public void ApplyVisual(bool isActive)
        {
            if (buttonRenderer == null)
            {
                return;
            }

            buttonRenderer.material.color = isActive ? activeColor : originalColor;
        }
    }

    [Header("Button Setup")]
    public ButtonBinding deltaKeyW = new ButtonBinding { buttonName = "DeltaKeyW" };
    public ButtonBinding deltaKeyA = new ButtonBinding { buttonName = "DeltaKeyA" };
    public ButtonBinding deltaKeyS = new ButtonBinding { buttonName = "DeltaKeyS" };
    public ButtonBinding deltaKeyD = new ButtonBinding { buttonName = "DeltaKeyD" };

    [Header("Debug")]
    public string currentTouchedButton = "None";
    public string interactionDebug = "No Delta plane button touched";

    private void Awake()
    {
        InitializeButton(deltaKeyW);
        InitializeButton(deltaKeyA);
        InitializeButton(deltaKeyS);
        InitializeButton(deltaKeyD);
    }

    private void Reset()
    {
        Collider colliderComponent = GetComponent<Collider>();
        if (colliderComponent != null)
        {
            colliderComponent.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (TryHandleEnter(deltaKeyW, other)) return;
        if (TryHandleEnter(deltaKeyA, other)) return;
        if (TryHandleEnter(deltaKeyS, other)) return;
        if (TryHandleEnter(deltaKeyD, other)) return;
    }

    private void OnTriggerExit(Collider other)
    {
        if (TryHandleExit(deltaKeyW, other)) return;
        if (TryHandleExit(deltaKeyA, other)) return;
        if (TryHandleExit(deltaKeyS, other)) return;
        if (TryHandleExit(deltaKeyD, other)) return;
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
            button.ApplyVisual(true);
            currentTouchedButton = button.buttonName;
            interactionDebug = "Touch enter: " + button.buttonName;
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
            button.ApplyVisual(false);
            interactionDebug = "Touch exit: " + button.buttonName;
        }

        RefreshCurrentTouchedButton();
        return true;
    }

    private void RefreshCurrentTouchedButton()
    {
        if (deltaKeyW.isTouched)
        {
            currentTouchedButton = deltaKeyW.buttonName;
            interactionDebug = "Touch stay: " + deltaKeyW.buttonName;
            return;
        }

        if (deltaKeyA.isTouched)
        {
            currentTouchedButton = deltaKeyA.buttonName;
            interactionDebug = "Touch stay: " + deltaKeyA.buttonName;
            return;
        }

        if (deltaKeyS.isTouched)
        {
            currentTouchedButton = deltaKeyS.buttonName;
            interactionDebug = "Touch stay: " + deltaKeyS.buttonName;
            return;
        }

        if (deltaKeyD.isTouched)
        {
            currentTouchedButton = deltaKeyD.buttonName;
            interactionDebug = "Touch stay: " + deltaKeyD.buttonName;
            return;
        }

        currentTouchedButton = "None";
        interactionDebug = "No Delta plane button touched";
    }
}

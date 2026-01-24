using System.Collections.Generic;
using UnityEngine;

public class FingerSnapManager : MonoBehaviour
{
    [System.Serializable]
    public class SnapConfiguration
    {
        [Header("Transforms to Snap")]
        public Transform transform1;
        public Transform transform2;

        [Header("Angle Ranges (Y-axis)")]
        [Tooltip("Min and Max angle for transform1")]
        public Vector2 angleRange1 = new Vector2(302f, 310f); // min, max
        [Tooltip("Min and Max angle for transform2")]
        public Vector2 angleRange2 = new Vector2(51f, 59f);

        [Header("Snap Target Angles")]
        public float snapAngle1 = 306f;
        public float snapAngle2 = 55f;

        [Header("Settings")]
        [Tooltip("Which axis to snap (X, Y, or Z)")]
        public AxisType axis = AxisType.Y;
        [Tooltip("Whether this snap configuration is enabled")]
        public bool enabled = true;
        [Tooltip("Name/description for this snap configuration")]
        public string configName = "Snap Config";

        [Header("Runtime Info (Read Only)")]
        public bool isInRange = false;
        public float currentAngle1 = 0f;
        public float currentAngle2 = 0f;
    }

    public enum AxisType { X, Y, Z }

    [Header("External References")]
    [Tooltip("Reference to ModeSwitching script")]
    public ModeSwitching modeSwitching;

    [Header("Snap Configurations")]
    public List<SnapConfiguration> snapConfigurations = new List<SnapConfiguration>();

    [Header("Global Settings")]
    [Tooltip("Time threshold before applying snap (seconds)")]
    public float snapDelayThreshold = 0.1f;
    
    [Tooltip("Enables debug logging")]
    public bool debugMode = false;

    private Dictionary<SnapConfiguration, float> snapTimers = new Dictionary<SnapConfiguration, float>();
    private Dictionary<SnapConfiguration, bool> snapApplied = new Dictionary<SnapConfiguration, bool>();

    void Update()
    {
        // Only execute snap logic when in manipulate mode
        if (modeSwitching == null || !modeSwitching.modeSelect)
            return;

        foreach (var config in snapConfigurations)
        {
            if (!config.enabled || config.transform1 == null || config.transform2 == null)
                continue;

            // Get current angles using localEulerAngles
            config.currentAngle1 = GetAngle(config.transform1, config.axis);
            config.currentAngle2 = GetAngle(config.transform2, config.axis);

            // Check if both angles are in range
            bool inRange = IsInRange(config.currentAngle1, config.angleRange1) && 
                          IsInRange(config.currentAngle2, config.angleRange2);

            config.isInRange = inRange;

            // Initialize timer if not exists
            if (!snapTimers.ContainsKey(config))
                snapTimers[config] = 0f;

            if (!snapApplied.ContainsKey(config))
                snapApplied[config] = false;

            if (inRange)
            {
                snapTimers[config] += Time.deltaTime;

                if (snapTimers[config] >= snapDelayThreshold && !snapApplied[config])
                {
                    ApplySnap(config);
                    snapApplied[config] = true;
                    
                    if (debugMode)
                    {
                        Debug.Log($"[{config.configName}] Snap Applied: {config.transform1.name} -> {config.snapAngle1}°, " +
                                  $"{config.transform2.name} -> {config.snapAngle2}°");
                    }
                }
            }
            else
            {
                snapTimers[config] = 0f;
                snapApplied[config] = false;
                
                if (debugMode && snapApplied.ContainsKey(config) && snapApplied[config])
                {
                    Debug.Log($"[{config.configName}] Snap Released - Out of Range");
                }
            }

            if (debugMode && inRange)
            {
                Debug.Log($"[{config.configName}] In Range: {config.transform1.name}={config.currentAngle1:F1}° " +
                          $"({config.angleRange1.x}-{config.angleRange1.y}), " +
                          $"{config.transform2.name}={config.currentAngle2:F1}° " +
                          $"({config.angleRange2.x}-{config.angleRange2.y})");
            }
        }
    }

    private float GetAngle(Transform t, AxisType axis)
    {
        var euler = t.localEulerAngles;
        switch (axis)
        {
            case AxisType.X: return euler.x;
            case AxisType.Y: return euler.y;
            case AxisType.Z: return euler.z;
            default: return 0f;
        }
    }

    private bool IsInRange(float angle, Vector2 range)
    {
        float min = range.x;
        float max = range.y;

        // Handle wrap-around for angles (e.g., 302-310 crosses 360/0 boundary)
        if (min > max)
        {
            // Range crosses 360/0 boundary (e.g., 302-310 means 302-360 and 0-310)
            return angle >= min || angle <= max;
        }
        else
        {
            // Normal range (e.g., 51-59)
            return angle >= min && angle <= max;
        }
    }

    private void ApplySnap(SnapConfiguration config)
    {
        Vector3 euler1 = config.transform1.localEulerAngles;
        Vector3 euler2 = config.transform2.localEulerAngles;

        switch (config.axis)
        {
            case AxisType.X:
                euler1.x = config.snapAngle1;
                euler2.x = config.snapAngle2;
                break;
            case AxisType.Y:
                euler1.y = config.snapAngle1;
                euler2.y = config.snapAngle2;
                break;
            case AxisType.Z:
                euler1.z = config.snapAngle1;
                euler2.z = config.snapAngle2;
                break;
        }

        config.transform1.localEulerAngles = euler1;
        config.transform2.localEulerAngles = euler2;
    }

    // Check if a specific transform is currently being snapped
    public bool IsTransformBeingSnapped(Transform t)
    {
        if (modeSwitching == null || !modeSwitching.modeSelect)
            return false;

        foreach (var config in snapConfigurations)
        {
            if (!config.enabled)
                continue;

            if ((config.transform1 == t || config.transform2 == t) && config.isInRange)
            {
                if (!snapApplied.ContainsKey(config))
                    continue;
                    
                // Only block ClawModuleController updates while snap is active
                if (snapApplied[config])
                    return true;
            }
        }
        return false;
    }
}

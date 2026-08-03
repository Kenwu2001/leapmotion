using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(ClawModuleController))]
public class BaselineTwo : MonoBehaviour
{
    [Header("=== Keyboard Control ===")]
    [Tooltip("ON: Enable WASD+QE keyboard control for motor offsets")]
    public bool useKeyboardControl = false;
    public Color lightRedColor = new Color(1f, 0.5f, 0.5f, 1f);

    private const int KB_ROWS = 5;
    private const int KB_COLS = 3;

    private ClawModuleController controller;
    private int kbCurrentRow = 3;
    private int kbCurrentCol = 1;
    private int[,] kbMotorIdArray;
    private Renderer[,] kbRendererArray;
    private float kbRotationSpeed = 18f;
    private bool prevUseKeyboardControl;
    private bool hadArrowInputLastFrame;
    private readonly bool[] kbSingleFrozen = new bool[12];
    private int previousSelectedMotorID;
    private int sideLockedMotorID;
    private bool sideLockedUseLeftSide;
    private bool hasPendingArrow;
    private int pendingArrowMotorID;
    private bool pendingArrowUseHorizontal;
    private bool pendingArrowUseLeftSide;
    private float pendingArrowDelta;
    private bool selectedFrozenUsesLightRed;
    public Baseline2PlaneButtonInteraction planeButtonInteraction;
    public TriggerRightWrist triggerRightWrist;

    [Header("=== Baseline2 Operation Log ===")]
    [Tooltip("Write one CSV row per completed operation. Relative folders are created under the Unity project folder.")]
    public bool enableOperationLogging = true;
    [Tooltip("Absolute folder path, or a path relative to the Unity project folder.")]
    public string operationLogFolder = "UserStudyLogs";
    [Tooltip("CSV file name written inside Operation Log Folder.")]
    public string operationLogFileName = "baseline2_operation_log.csv";
    [Tooltip("If true, append a timestamp suffix when a new log session starts to avoid overwriting previous files.")]
    public bool appendTimestampToLogFileName = true;
    [Tooltip("Press this key during Play Mode to discard the current log and start again from operation 0.")]
    public KeyCode restartOperationLogKey = KeyCode.Backspace;
    [Tooltip("Turn this on in the Inspector during Play Mode to discard the current log and start again from operation 0.")]
    public bool restartOperationLogNow;
    [Tooltip("Turn this on in the Inspector during Play Mode to write the current operation log values to CSV.")]
    public bool writeOperationLogNow;
    public int loggedOperationCount;
    public int engagementOnCount;
    public float totalOperationSeconds;
    public float taskCompletionSeconds;
    public string currentOperationLogPath = "";
    public string operationLogStatus = "Log not started";

    private struct OperationLogEntry
    {
        public int index;
        public float startTime;
        public float endTime;
        public string startedAt;
        public string endedAt;
    }

    private readonly List<OperationLogEntry> operationLogEntries = new List<OperationLogEntry>();
    private bool operationLogActive;
    private bool operationSawAdjustmentInput;
    private bool currentOperationCounted;
    private float currentOperationStartTime;
    private string currentOperationStartedAt;
    private float completedOperationSeconds;
    private bool previousAnyAdjustmentPressed;
    private bool previousEngagementActive;
    private bool hasPendingOperationEnd;
    private float pendingOperationEndTime;
    private string pendingOperationEndedAt;
    private bool hasTaskCompletionStart;
    private bool hasTaskCompletionEnd;
    private float taskCompletionStartTime;
    private float taskCompletionEndTime;
    private string taskCompletionStartedAt;
    private string taskCompletionEndedAt;
    private string runtimeOperationLogFileName;
    private bool wasPlaneWPressed;
    private bool wasPlaneAPressed;
    private bool wasPlaneSPressed;
    private bool wasPlaneDPressed;

    public bool IsMoveUpPressed => useKeyboardControl && (Input.GetKey(KeyCode.W) || IsPlaneButtonTouched(KeyCode.W));
    public bool IsMoveLeftPressed => useKeyboardControl && (Input.GetKey(KeyCode.A) || IsPlaneButtonTouched(KeyCode.A));
    public bool IsMoveDownPressed => useKeyboardControl && (Input.GetKey(KeyCode.S) || IsPlaneButtonTouched(KeyCode.S));
    public bool IsMoveRightPressed => useKeyboardControl && (Input.GetKey(KeyCode.D) || IsPlaneButtonTouched(KeyCode.D));
    public bool IsRotateNegativePressed => useKeyboardControl && (Input.GetKey(KeyCode.Q) || IsPlaneButtonTouched(KeyCode.Q));
    public bool IsRotatePositivePressed => useKeyboardControl && (Input.GetKey(KeyCode.E) || IsPlaneButtonTouched(KeyCode.E));
    public bool IsFreezePressed => useKeyboardControl && (Input.GetKey(KeyCode.F) || IsPlaneButtonTouched(KeyCode.F));
    public bool IsResetPressed => useKeyboardControl && (Input.GetKey(KeyCode.Space) || IsPlaneButtonTouched(KeyCode.Space));
    public bool IsIndexMiddleIndividualModeActive => useKeyboardControl && controller != null && controller.useIndexMiddleIndividualMode;
    public bool IsSmallRangeMappingActive => useKeyboardControl && controller != null && !controller.isFullRangeMapping;
    public bool IsSnappingModeActive => useKeyboardControl && controller != null && controller.IsCurrentSnappingEnabled();
    public bool IsCurrentSelectionFrozen => IsSelectionFrozen(GetMotorIDForCell(kbCurrentRow, kbCurrentCol));

    private void Awake()
    {
        controller = GetComponent<ClawModuleController>();

        if (planeButtonInteraction == null)
        {
            planeButtonInteraction = FindObjectOfType<Baseline2PlaneButtonInteraction>();
        }

        if (triggerRightWrist == null)
        {
            triggerRightWrist = FindObjectOfType<TriggerRightWrist>();
        }

        if (useKeyboardControl && controller != null && controller.modeSwitching != null)
        {
            controller.modeSwitching.enabled = false;
        }
    }

    private void Start()
    {
        if (controller == null)
        {
            controller = GetComponent<ClawModuleController>();
            if (controller == null)
            {
                enabled = false;
                return;
            }
        }

        kbMotorIdArray = new int[KB_ROWS, KB_COLS]
        {
            { 1, 5, 9 },
            { 2, 6, 10 },
            { 3, 7, 11 },
            { 4, 8, 12 },
            { 13, 14, 15 }
        };

        kbRendererArray = new Renderer[KB_ROWS, KB_COLS]
        {
            { controller.thumbJoint1Renderer, controller.indexJoint1Renderer, controller.middleJoint1Renderer },
            { controller.thumbJoint2Renderer, controller.indexJoint2Renderer, controller.middleJoint2Renderer },
            { controller.thumbJoint3Renderer, controller.indexJoint3Renderer, controller.middleJoint3Renderer },
            { controller.thumbJoint4Renderer, controller.indexJoint4Renderer, controller.middleJoint4Renderer },
            { GetPaxiniRenderer(13), GetPaxiniRenderer(14), GetPaxiniRenderer(15) }
        };

        prevUseKeyboardControl = useKeyboardControl;
        RestartOperationLog();
        if (useKeyboardControl)
        {
            EnterKeyboardMode();
        }
    }

    private void Update()
    {
        if (controller == null)
        {
            return;
        }

        if (useKeyboardControl != prevUseKeyboardControl)
        {
            prevUseKeyboardControl = useKeyboardControl;
            if (useKeyboardControl)
            {
                EnterKeyboardMode();
            }
            else
            {
                ExitKeyboardMode();
            }
        }

        TrackTaskCompletionTime();

        if (useKeyboardControl)
        {
            HandleOperationLogRestartInput();
            TrackOperationStartBeforeInputHandlers();
            HandleKeyboardControl();
            TrackOperationEndAfterInputHandlers();
        }
    }

    private void HandleOperationLogRestartInput()
    {
        if (!enableOperationLogging)
        {
            restartOperationLogNow = false;
            writeOperationLogNow = false;
            return;
        }

        if (restartOperationLogNow || (restartOperationLogKey != KeyCode.None && Input.GetKeyDown(restartOperationLogKey)))
        {
            restartOperationLogNow = false;
            RestartOperationLog();
        }

        if (writeOperationLogNow)
        {
            writeOperationLogNow = false;
            CompletePendingOperationIfReady();
            WriteOperationLogCsv(true);
        }
    }

    private void TrackOperationStartBeforeInputHandlers()
    {
        if (!enableOperationLogging)
        {
            return;
        }

        if (!hasTaskCompletionStart)
        {
            return;
        }

        bool navigationPressedThisFrame = IsAnyNavigationPressedThisFrame();
        bool adjustmentPressedThisFrame = IsAnyAdjustmentPressed() && !previousAnyAdjustmentPressed;
        if (navigationPressedThisFrame)
        {
            if (operationLogActive)
            {
                if (hasPendingOperationEnd)
                {
                    CompleteCurrentOperation(pendingOperationEndTime, pendingOperationEndedAt);
                }
                else
                {
                    return;
                }
            }

            StartNewOperation();
        }
        else if (!operationLogActive && adjustmentPressedThisFrame)
        {
            StartNewOperation();
        }
    }

    private void StartNewOperation()
    {
        operationLogActive = true;
        operationSawAdjustmentInput = false;
        currentOperationCounted = false;
        hasPendingOperationEnd = false;
        currentOperationStartTime = Time.realtimeSinceStartup;
        currentOperationStartedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        operationLogStatus = "Operation " + (operationLogEntries.Count + 1) + " running";
    }

    private void TrackOperationEndAfterInputHandlers()
    {
        if (!enableOperationLogging)
        {
            previousAnyAdjustmentPressed = IsAnyAdjustmentPressed();
            return;
        }

        if (!hasTaskCompletionStart)
        {
            previousAnyAdjustmentPressed = IsAnyAdjustmentPressed();
            return;
        }

        bool anyAdjustmentPressed = IsAnyAdjustmentPressed();
        if (operationLogActive && anyAdjustmentPressed)
        {
            operationSawAdjustmentInput = true;
            CountCurrentOperationIfNeeded();
        }

        bool adjustmentReleasedThisFrame = previousAnyAdjustmentPressed && !anyAdjustmentPressed;
        if (operationLogActive && operationSawAdjustmentInput && adjustmentReleasedThisFrame)
        {
            hasPendingOperationEnd = true;
            pendingOperationEndTime = Time.realtimeSinceStartup;
            pendingOperationEndedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            UpdateTotalOperationSeconds();
        }

        previousAnyAdjustmentPressed = anyAdjustmentPressed;
    }

    private void TrackTaskCompletionTime()
    {
        if (!enableOperationLogging)
        {
            previousEngagementActive = IsEngagementActive();
            return;
        }

        bool engagementActive = IsEngagementActive();
        if (engagementActive && !previousEngagementActive)
        {
            engagementOnCount += 1;
            if (!hasTaskCompletionStart)
            {
                hasTaskCompletionStart = true;
                hasTaskCompletionEnd = false;
                taskCompletionStartTime = Time.realtimeSinceStartup;
                taskCompletionEndTime = 0f;
                taskCompletionSeconds = 0f;
                taskCompletionStartedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                taskCompletionEndedAt = "";
                operationLogStatus = "Engagement started: operation logging is now active";
            }

            WriteOperationLogCsv();
        }

        if (!engagementActive && previousEngagementActive && hasTaskCompletionStart)
        {
            CompletePendingOperationIfReady();
            hasTaskCompletionEnd = true;
            taskCompletionEndTime = Time.realtimeSinceStartup;
            taskCompletionSeconds = Mathf.Max(0f, taskCompletionEndTime - taskCompletionStartTime);
            taskCompletionEndedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            WriteOperationLogCsv();
        }

        previousEngagementActive = engagementActive;
    }

    private bool IsEngagementActive()
    {
        return triggerRightWrist != null && triggerRightWrist.IsEngaged;
    }

    private bool IsAnyNavigationPressedThisFrame()
    {
        return Input.GetKeyDown(KeyCode.W)
            || Input.GetKeyDown(KeyCode.A)
            || Input.GetKeyDown(KeyCode.S)
            || Input.GetKeyDown(KeyCode.D)
            || ConsumePlaneButtonPress(KeyCode.W, ref wasPlaneWPressed)
            || ConsumePlaneButtonPress(KeyCode.A, ref wasPlaneAPressed)
            || ConsumePlaneButtonPress(KeyCode.S, ref wasPlaneSPressed)
            || ConsumePlaneButtonPress(KeyCode.D, ref wasPlaneDPressed);
    }

    private bool ConsumePlaneButtonPress(KeyCode keyCode, ref bool wasPressedLastFrame)
    {
        bool isPressedThisFrame = IsPlaneButtonTouched(keyCode);
        bool pressedThisFrame = isPressedThisFrame && !wasPressedLastFrame;
        wasPressedLastFrame = isPressedThisFrame;
        return pressedThisFrame;
    }

    private bool IsAnyAdjustmentPressed()
    {
        return IsAdjustmentPressed(KeyCode.Q)
            || IsAdjustmentPressed(KeyCode.E)
            || IsAdjustmentPressed(KeyCode.F);
    }

    private bool IsAdjustmentPressed(KeyCode keyCode)
    {
        return Input.GetKey(keyCode) || IsPlaneButtonTouched(keyCode);
    }

    private void CompletePendingOperationIfReady()
    {
        if (operationLogActive && hasPendingOperationEnd)
        {
            CompleteCurrentOperation(pendingOperationEndTime, pendingOperationEndedAt);
        }
    }

    private void CompleteCurrentOperation(float endTime, string endedAt)
    {
        float duration = Mathf.Max(0f, endTime - currentOperationStartTime);
        operationLogEntries.Add(new OperationLogEntry
        {
            index = operationLogEntries.Count + 1,
            startTime = currentOperationStartTime,
            endTime = endTime,
            startedAt = currentOperationStartedAt,
            endedAt = endedAt
        });

        completedOperationSeconds += duration;
        operationLogActive = false;
        operationSawAdjustmentInput = false;
        currentOperationCounted = false;
        hasPendingOperationEnd = false;
        UpdateLoggedOperationCount();
        UpdateTotalOperationSeconds();
        WriteOperationLogCsv();
    }

    private void CountCurrentOperationIfNeeded()
    {
        if (!currentOperationCounted)
        {
            currentOperationCounted = true;
            UpdateLoggedOperationCount();
        }
    }

    private void UpdateLoggedOperationCount()
    {
        loggedOperationCount = operationLogEntries.Count + (currentOperationCounted ? 1 : 0);
    }

    private void UpdateTotalOperationSeconds()
    {
        totalOperationSeconds = completedOperationSeconds;
        if (operationLogActive && hasPendingOperationEnd)
        {
            totalOperationSeconds += Mathf.Max(0f, pendingOperationEndTime - currentOperationStartTime);
        }
    }

    [ContextMenu("Restart Operation Log")]
    public void RestartOperationLog()
    {
        operationLogEntries.Clear();
        loggedOperationCount = 0;
        engagementOnCount = 0;
        completedOperationSeconds = 0f;
        totalOperationSeconds = 0f;
        taskCompletionSeconds = 0f;
        operationLogActive = false;
        operationSawAdjustmentInput = false;
        currentOperationCounted = false;
        hasPendingOperationEnd = false;
        previousAnyAdjustmentPressed = IsAnyAdjustmentPressed();
        hasTaskCompletionStart = false;
        hasTaskCompletionEnd = false;
        taskCompletionStartTime = 0f;
        taskCompletionEndTime = 0f;
        taskCompletionStartedAt = "";
        taskCompletionEndedAt = "";
        previousEngagementActive = IsEngagementActive();
        if (previousEngagementActive)
        {
            engagementOnCount = 1;
            hasTaskCompletionStart = true;
            taskCompletionStartTime = Time.realtimeSinceStartup;
            taskCompletionStartedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        }

        runtimeOperationLogFileName = BuildRuntimeOperationLogFileName();
        WriteOperationLogCsv();
        if (hasTaskCompletionStart)
        {
            operationLogStatus = "Engagement already ON: operation logging active";
        }
        else
        {
            operationLogStatus = "Waiting for first engagement ON";
        }
    }

    private string BuildRuntimeOperationLogFileName(bool forceTimestamp = false)
    {
        string fileName = string.IsNullOrWhiteSpace(operationLogFileName) ? "baseline2_operation_log.csv" : operationLogFileName.Trim();
        string extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension))
        {
            extension = ".csv";
        }

        string baseName = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "baseline2_operation_log";
        }

        if (!forceTimestamp && !appendTimestampToLogFileName)
        {
            return baseName + extension;
        }

        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
        return baseName + "_" + timestamp + extension;
    }

    private void WriteOperationLogCsv(bool forceNewFileName = false)
    {
        if (!enableOperationLogging)
        {
            operationLogStatus = "Operation logging disabled";
            return;
        }

        string folderPath = ResolveOperationLogFolderPath();
        if (forceNewFileName)
        {
            runtimeOperationLogFileName = BuildRuntimeOperationLogFileName(true);
        }
        else if (string.IsNullOrWhiteSpace(runtimeOperationLogFileName))
        {
            runtimeOperationLogFileName = BuildRuntimeOperationLogFileName();
        }

        try
        {
            Directory.CreateDirectory(folderPath);
            currentOperationLogPath = Path.Combine(folderPath, runtimeOperationLogFileName);
            File.WriteAllText(currentOperationLogPath, BuildOperationLogCsv(), Encoding.UTF8);
            operationLogStatus = "Wrote " + loggedOperationCount + " operations";
        }
        catch (System.Exception exception)
        {
            operationLogStatus = "Log write failed: " + exception.Message;
            Debug.LogError(operationLogStatus);
        }
    }

    private string ResolveOperationLogFolderPath()
    {
        string folder = string.IsNullOrWhiteSpace(operationLogFolder) ? "UserStudyLogs" : operationLogFolder.Trim();
        if (Path.IsPathRooted(folder))
        {
            return folder;
        }

        string projectFolder = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectFolder, folder);
    }

    private string BuildOperationLogCsv()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("RecordType,OperationIndex,StartRealtimeSeconds,EndRealtimeSeconds,DurationSeconds,StartedAt,EndedAt,TotalOperations,TotalOperationSeconds,TaskStartRealtimeSeconds,TaskEndRealtimeSeconds,TaskCompletionSeconds,TaskStartedAt,TaskEndedAt,EngagementOnCount");

        float calculatedTotalOperationSeconds = 0f;
        for (int i = 0; i < operationLogEntries.Count; i++)
        {
            OperationLogEntry entry = operationLogEntries[i];
            float duration = Mathf.Max(0f, entry.endTime - entry.startTime);
            calculatedTotalOperationSeconds += duration;
            builder.Append("Operation,");
            builder.Append(entry.index.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(entry.startTime.ToString("F4", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(entry.endTime.ToString("F4", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(duration.ToString("F4", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(EscapeCsv(entry.startedAt));
            builder.Append(',');
            builder.Append(EscapeCsv(entry.endedAt));
            builder.AppendLine(",,,,,,,,");
        }

        completedOperationSeconds = calculatedTotalOperationSeconds;
        UpdateLoggedOperationCount();
        UpdateTotalOperationSeconds();
        builder.Append("Summary,,,,,,,");
        builder.Append(loggedOperationCount.ToString(CultureInfo.InvariantCulture));
        builder.Append(',');
        builder.Append(totalOperationSeconds.ToString("F4", CultureInfo.InvariantCulture));
        builder.Append(',');
        builder.Append(hasTaskCompletionStart ? taskCompletionStartTime.ToString("F4", CultureInfo.InvariantCulture) : "");
        builder.Append(',');
        builder.Append(hasTaskCompletionEnd ? taskCompletionEndTime.ToString("F4", CultureInfo.InvariantCulture) : "");
        builder.Append(',');
        builder.Append(hasTaskCompletionEnd ? taskCompletionSeconds.ToString("F4", CultureInfo.InvariantCulture) : "");
        builder.Append(',');
        builder.Append(EscapeCsv(taskCompletionStartedAt));
        builder.Append(',');
        builder.Append(EscapeCsv(taskCompletionEndedAt));
        builder.Append(',');
        builder.AppendLine(engagementOnCount.ToString(CultureInfo.InvariantCulture));
        return builder.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }

    private void LateUpdate()
    {
        if (controller == null)
        {
            return;
        }

        if (!useKeyboardControl)
        {
            return;
        }

        if (hasPendingArrow)
        {
            if (pendingArrowUseHorizontal)
            {
                controller.SyncArmUIHorizontalArrowState(pendingArrowMotorID, pendingArrowUseLeftSide, pendingArrowDelta);
            }
            else
            {
                controller.SyncArmUIDirectAngleArrowState(pendingArrowMotorID, pendingArrowDelta);
            }
        }
    }

    private void EnterKeyboardMode()
    {
        controller.ResetFingerRotations();
        controller.KeyboardResetModeSwitchingState();
        SetCollidersEnabled(false);

        if (controller.modeSwitching != null)
        {
            controller.modeSwitching.enabled = false;
        }

        kbCurrentRow = 3;
        kbCurrentCol = 1;
        previousSelectedMotorID = GetMotorIDForCell(kbCurrentRow, kbCurrentCol);
        sideLockedMotorID = 0;
        sideLockedUseLeftSide = false;
        ClearAllSingleFreezeStates();
        KbSetAllColors(controller.KeyboardOriginalColor);
        KbUpdateSelection();
    }

    private void ExitKeyboardMode()
    {
        controller.ResetFingerRotations();
        controller.KeyboardResetModeSwitchingState();
        SetCollidersEnabled(true);

        if (controller.modeSwitching != null)
        {
            controller.modeSwitching.enabled = true;
        }

        controller.KeyboardSetEmbodimentInitialColors();
        controller.ClearArmUIDirectAngleArrowState();
        controller.KeyboardClearSingleMotorFreezeOverrides();
        hadArrowInputLastFrame = false;
        sideLockedMotorID = 0;
        hasPendingArrow = false;
        ClearAllSingleFreezeStates();
    }

    private void SetCollidersEnabled(bool enabledState)
    {
        if (controller.modeSwitching != null && controller.modeSwitching.SelectMotorCollider != null)
        {
            controller.modeSwitching.SelectMotorCollider.enabled = enabledState;
        }

        SetTriggerColliderEnabled(controller.triggerRightIndexTip, enabledState);
        SetTriggerColliderEnabled(controller.triggerRightMiddleTip, enabledState);
        SetTriggerColliderEnabled(controller.triggerRightThumbTip, enabledState);
        SetTriggerColliderEnabled(controller.triggerRightThumbAbduction, enabledState);
    }

    private static void SetTriggerColliderEnabled(MonoBehaviour trigger, bool enabledState)
    {
        if (trigger == null)
        {
            return;
        }

        Collider col = trigger.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = enabledState;
        }
    }

    private void KbSetAllColors(Color color)
    {
        for (int row = 0; row < KB_ROWS; row++)
        {
            for (int col = 0; col < KB_COLS; col++)
            {
                int motorID = GetMotorIDForCell(row, col);
                if (motorID > 0)
                {
                    ApplyMotorVisualState(motorID, color);
                }
            }
        }
    }

    private void KbUpdateSelection()
    {
        int previousMotorID = previousSelectedMotorID;
        int currentMotorID = GetMotorIDForCell(kbCurrentRow, kbCurrentCol);

        if (previousMotorID > 0)
        {
            ApplyMotorVisualState(previousMotorID);
        }

        previousSelectedMotorID = currentMotorID;
        selectedFrozenUsesLightRed = IsMotorFrozen(currentMotorID);
        ApplyMotorVisualState(currentMotorID);
    }

    private void HandleKeyboardControl()
    {
        if (Input.GetKeyDown(KeyCode.W)) MoveSelectionUp();
        else if (Input.GetKeyDown(KeyCode.S)) MoveSelectionDown();
        else if (Input.GetKeyDown(KeyCode.A)) MoveSelectionLeft();
        else if (Input.GetKeyDown(KeyCode.D)) MoveSelectionRight();

        int selectedMotorID = GetMotorIDForCell(kbCurrentRow, kbCurrentCol);

        float rotDelta = kbRotationSpeed * Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleCurrentSelectionFreeze();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ToggleIndexMiddleIndividualMode();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ToggleSmallRangeMapping();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ToggleSnappingMode();
        }

        bool isPaxiniSelection = selectedMotorID >= 13 && selectedMotorID <= 15;

        if (!isPaxiniSelection)
        {
            if (IsRotateNegativePressed) KbApplyRotation(kbCurrentRow, kbCurrentCol, -rotDelta);
            if (IsRotatePositivePressed) KbApplyRotation(kbCurrentRow, kbCurrentCol, rotDelta);
        }

        bool hasArrowInput = !isPaxiniSelection && (IsRotateNegativePressed || IsRotatePositivePressed);
        if (!hasArrowInput && hadArrowInputLastFrame)
        {
            controller.ClearArmUIDirectAngleArrowState();
            hasPendingArrow = false;
        }
        hadArrowInputLastFrame = hasArrowInput;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TriggerReset();
        }
    }

    public void ToggleIndexMiddleIndividualMode()
    {
        if (!useKeyboardControl || controller == null)
        {
            return;
        }

        controller.useIndexMiddleIndividualMode = !controller.useIndexMiddleIndividualMode;
    }

    public void ToggleSmallRangeMapping()
    {
        if (!useKeyboardControl || controller == null)
        {
            return;
        }

        controller.isFullRangeMapping = !controller.isFullRangeMapping;
    }

    public void ToggleSnappingMode()
    {
        if (!useKeyboardControl || controller == null)
        {
            return;
        }

        controller.ToggleCurrentSnapping();
    }

    public void TriggerReset()
    {
        if (!useKeyboardControl)
        {
            return;
        }

        ResetKeyboardOffsets();
    }

    public void MoveSelectionUp()
    {
        if (!useKeyboardControl)
        {
            return;
        }

        kbCurrentRow = (kbCurrentRow + 1) % KB_ROWS;
        HandleSelectionChanged();
    }

    public void MoveSelectionLeft()
    {
        if (!useKeyboardControl)
        {
            return;
        }

        kbCurrentCol = (kbCurrentCol - 1 + KB_COLS) % KB_COLS;
        HandleSelectionChanged();
    }

    public void MoveSelectionDown()
    {
        if (!useKeyboardControl)
        {
            return;
        }

        kbCurrentRow = (kbCurrentRow - 1 + KB_ROWS) % KB_ROWS;
        HandleSelectionChanged();
    }

    public void MoveSelectionRight()
    {
        if (!useKeyboardControl)
        {
            return;
        }

        kbCurrentCol = (kbCurrentCol + 1) % KB_COLS;
        HandleSelectionChanged();
    }

    private void HandleSelectionChanged()
    {
        int previousMotorID = previousSelectedMotorID;
        int selectedMotorID = GetMotorIDForCell(kbCurrentRow, kbCurrentCol);

        if (selectedMotorID != previousMotorID)
        {
            sideLockedMotorID = 0;
            sideLockedUseLeftSide = false;
            hasPendingArrow = false;
            controller.ClearArmUIDirectAngleArrowState();
        }

        KbUpdateSelection();
    }

    private void ResetKeyboardOffsets()
    {
        if (controller == null)
        {
            return;
        }

        // Use the controller's canonical reset path so rotations, freeze states,
        // mode state, and Paxini colors are all restored consistently.
        controller.ResetFingerRotations();

        // Keep BaselineTwo local freeze cache fully cleared after reset.
        controller.KeyboardClearSingleMotorFreezeOverrides();
        ClearAllSingleFreezeStates();

        // Re-apply baseline keyboard visuals: no yellow freeze colors.
        selectedFrozenUsesLightRed = false;
        KbSetAllColors(controller.KeyboardOriginalColor);
        KbUpdateSelection();

        controller.ClearArmUIDirectAngleArrowState();
        hadArrowInputLastFrame = false;
        hasPendingArrow = false;
        sideLockedMotorID = 0;
        sideLockedUseLeftSide = false;
        previousSelectedMotorID = GetMotorIDForCell(kbCurrentRow, kbCurrentCol);
    }

    public void ToggleCurrentSelectionFreeze()
    {
        if (!useKeyboardControl)
        {
            return;
        }

        int motorID = GetMotorIDForCell(kbCurrentRow, kbCurrentCol);
        if (motorID <= 0)
        {
            return;
        }

        if (motorID >= 13 && motorID <= 15)
        {
            TogglePaxiniFreezeForSelection(motorID);
            return;
        }

        if (controller != null && controller.KeyboardIsPaxiniFrozen(motorID))
        {
            if (controller.KeyboardReleasePaxiniFreezeForMotor(motorID))
            {
                int groupStart = GetGroupStartForMotorID(motorID);
                for (int currentMotorID = groupStart; currentMotorID < groupStart + 4; currentMotorID++)
                {
                    int index = currentMotorID - 1;
                    if (index >= 0 && index < kbSingleFrozen.Length)
                    {
                        kbSingleFrozen[index] = currentMotorID != motorID;
                    }
                }

                selectedFrozenUsesLightRed = false;
                RefreshAllMotorVisualStates();
            }
            return;
        }

        bool newFrozenState = !IsSingleFrozen(motorID);
        SetSingleFrozen(motorID, newFrozenState);
    }

    private bool IsSelectionFrozen(int motorID)
    {
        if (motorID >= 13 && motorID <= 15)
        {
            return IsPaxiniSelectionFrozen(motorID);
        }

        return IsSingleFrozen(motorID);
    }

    private bool IsSingleFrozen(int motorID)
    {
        if (motorID < 1 || motorID > kbSingleFrozen.Length)
        {
            return false;
        }

        return kbSingleFrozen[motorID - 1];
    }

    private bool IsPaxiniSelectionFrozen(int motorID)
    {
        if (controller == null)
        {
            return false;
        }

        return controller.KeyboardShouldShowPaxiniYellow(motorID);
    }

    private void TogglePaxiniFreezeForSelection(int motorID)
    {
        if (controller == null)
        {
            return;
        }

        if (!controller.KeyboardTogglePaxiniFreeze(motorID))
        {
            return;
        }

        if (!controller.KeyboardIsPaxiniFrozen(motorID))
        {
            ClearSingleFreezeStateForGroup(GetGroupStartForPaxiniSelection(motorID));
        }

        if (motorID == GetMotorIDForCell(kbCurrentRow, kbCurrentCol))
        {
            selectedFrozenUsesLightRed = false;
        }

        RefreshAllMotorVisualStates();
    }

    private static int GetGroupStartForPaxiniSelection(int motorID)
    {
        if (motorID == 13) return 1;
        if (motorID == 14) return 5;
        if (motorID == 15) return 9;
        return 0;
    }

    private static int GetGroupStartForMotorID(int motorID)
    {
        if (motorID >= 1 && motorID <= 4) return 1;
        if (motorID >= 5 && motorID <= 8) return 5;
        if (motorID >= 9 && motorID <= 12) return 9;
        return 0;
    }

    private void SetSingleFrozen(int motorID, bool frozen)
    {
        if (motorID < 1 || motorID > kbSingleFrozen.Length)
        {
            return;
        }

        kbSingleFrozen[motorID - 1] = frozen;

        if (controller != null && controller.modeSwitching != null && controller.modeSwitching.singleMotorFrozen != null &&
            motorID - 1 < controller.modeSwitching.singleMotorFrozen.Length)
        {
            controller.KeyboardSetSingleMotorFreezeState(motorID, frozen);
        }

        if (motorID == GetMotorIDForCell(kbCurrentRow, kbCurrentCol))
        {
            selectedFrozenUsesLightRed = false;
        }

        if (controller != null)
        {
            controller.RefreshKeyboardPaxiniPreviewForMotor(motorID);
        }

        ApplyMotorVisualState(motorID);
    }

    private void ClearAllSingleFreezeStates()
    {
        for (int i = 0; i < kbSingleFrozen.Length; i++)
        {
            kbSingleFrozen[i] = false;
        }

        if (controller != null && controller.modeSwitching != null && controller.modeSwitching.singleMotorFrozen != null)
        {
            int count = Mathf.Min(kbSingleFrozen.Length, controller.modeSwitching.singleMotorFrozen.Length);
            for (int i = 0; i < count; i++)
            {
                controller.KeyboardSetSingleMotorFreezeState(i + 1, false);
            }

            controller.ClearKeyboardPaxiniPreviewStates();
        }

        RefreshAllMotorVisualStates();
    }

    private void ClearSingleFreezeStateForGroup(int groupStart)
    {
        int groupEnd = groupStart + 3;
        for (int motorID = groupStart; motorID <= groupEnd; motorID++)
        {
            int index = motorID - 1;
            if (index < 0 || index >= kbSingleFrozen.Length)
            {
                continue;
            }

            kbSingleFrozen[index] = false;
        }
    }

    private void RefreshAllMotorVisualStates()
    {
        for (int row = 0; row < KB_ROWS; row++)
        {
            for (int col = 0; col < KB_COLS; col++)
            {
                int motorID = GetMotorIDForCell(row, col);
                if (motorID > 0)
                {
                    ApplyMotorVisualState(motorID);
                }
            }
        }
    }

    private void ApplyMotorVisualState(int motorID, Color fallbackColor)
    {
        Renderer renderer = GetRendererForMotorID(motorID);
        if (renderer == null)
        {
            return;
        }

        renderer.material.color = GetKeyboardVisualColorForMotor(motorID, fallbackColor);
    }

    public Color GetKeyboardVisualColorForMotor(int motorID, Color fallbackColor)
    {
        if (motorID <= 0)
        {
            return fallbackColor;
        }

        bool isSelected = motorID == GetMotorIDForCell(kbCurrentRow, kbCurrentCol);
        bool isPaxiniFrozen = controller != null && controller.KeyboardIsPaxiniFrozen(motorID);
        bool shouldShowPaxiniYellow = controller != null && controller.KeyboardShouldShowPaxiniYellow(motorID);
        bool isSingleFrozen = IsSingleFrozen(motorID);
        bool isFrozen = shouldShowPaxiniYellow || isSingleFrozen;
        Color freezeColor = controller != null ? controller.yellowColor : Color.yellow;

        if (isSelected)
        {
            if (isFrozen && !selectedFrozenUsesLightRed)
            {
                return freezeColor;
            }

            return lightRedColor;
        }

        if (shouldShowPaxiniYellow)
        {
            return freezeColor;
        }

        if (motorID >= 13 && motorID <= 15)
        {
            return fallbackColor;
        }

        if (isSingleFrozen)
        {
            return freezeColor;
        }

        return fallbackColor;
    }

    private bool IsMotorFrozen(int motorID)
    {
        if (motorID <= 0)
        {
            return false;
        }

        if (controller != null && controller.KeyboardIsPaxiniFrozen(motorID))
        {
            return true;
        }

        return IsSingleFrozen(motorID);
    }

    private void ApplyMotorVisualState(int motorID)
    {
        ApplyMotorVisualState(motorID, controller != null ? controller.KeyboardOriginalColor : Color.white);
    }

    private bool IsPlaneButtonTouched(KeyCode keyCode)
    {
        if (planeButtonInteraction == null)
        {
            planeButtonInteraction = FindObjectOfType<Baseline2PlaneButtonInteraction>();
        }

        if (planeButtonInteraction == null)
        {
            return false;
        }

        Baseline2PlaneButtonInteraction.ButtonBinding button = planeButtonInteraction.GetButtonBinding(keyCode);
        return button != null && button.isTouched;
    }

    private Renderer GetRendererForMotorID(int motorID)
    {
        switch (motorID)
        {
            case 1: return controller != null ? controller.thumbJoint1Renderer : null;
            case 2: return controller != null ? controller.thumbJoint2Renderer : null;
            case 3: return controller != null ? controller.thumbJoint3Renderer : null;
            case 4: return controller != null ? controller.thumbJoint4Renderer : null;
            case 5: return controller != null ? controller.indexJoint1Renderer : null;
            case 6: return controller != null ? controller.indexJoint2Renderer : null;
            case 7: return controller != null ? controller.indexJoint3Renderer : null;
            case 8: return controller != null ? controller.indexJoint4Renderer : null;
            case 9: return controller != null ? controller.middleJoint1Renderer : null;
            case 10: return controller != null ? controller.middleJoint2Renderer : null;
            case 11: return controller != null ? controller.middleJoint3Renderer : null;
            case 12: return controller != null ? controller.middleJoint4Renderer : null;
            case 13: return controller != null && controller.triggerRightThumbTip != null ? controller.triggerRightThumbTip.thumbPaxiniRenderer : null;
            case 14: return controller != null && controller.triggerRightIndexTip != null ? controller.triggerRightIndexTip.indexPaxiniRenderer : null;
            case 15: return controller != null && controller.triggerRightMiddleTip != null ? controller.triggerRightMiddleTip.middlePaxiniRenderer : null;
            default: return null;
        }
    }

    private Renderer GetPaxiniRenderer(int motorID)
    {
        if (controller == null)
        {
            return null;
        }

        if (motorID == 13 && controller.triggerRightThumbTip != null)
        {
            return controller.triggerRightThumbTip.thumbPaxiniRenderer;
        }

        if (motorID == 14 && controller.triggerRightIndexTip != null)
        {
            return controller.triggerRightIndexTip.indexPaxiniRenderer;
        }

        if (motorID == 15 && controller.triggerRightMiddleTip != null)
        {
            return controller.triggerRightMiddleTip.middlePaxiniRenderer;
        }

        return null;
    }

    private static int GetMotorIDForCell(int row, int col)
    {
        switch (row)
        {
            case 0:
                switch (col)
                {
                    case 0: return 1;
                    case 1: return 5;
                    case 2: return 9;
                }
                break;
            case 1:
                switch (col)
                {
                    case 0: return 2;
                    case 1: return 6;
                    case 2: return 10;
                }
                break;
            case 2:
                switch (col)
                {
                    case 0: return 3;
                    case 1: return 7;
                    case 2: return 11;
                }
                break;
            case 3:
                switch (col)
                {
                    case 0: return 4;
                    case 1: return 8;
                    case 2: return 12;
                }
                break;
            case 4:
                switch (col)
                {
                    case 0: return 13;
                    case 1: return 14;
                    case 2: return 15;
                }
                break;
        }

        return 0;
    }

    private void SyncKeyboardArrowForCell(int row, int col, float delta)
    {
        if (Mathf.Abs(delta) <= 0.0001f)
        {
            return;
        }

        int motorID = GetMotorIDForCell(row, col);
        if (motorID <= 0)
        {
            return;
        }

        if (row <= 1)
        {
            if (sideLockedMotorID != motorID)
            {
                sideLockedMotorID = motorID;
                sideLockedUseLeftSide = delta < 0f;
            }

            hasPendingArrow = true;
            pendingArrowMotorID = motorID;
            pendingArrowUseHorizontal = true;
            pendingArrowUseLeftSide = sideLockedUseLeftSide;
            pendingArrowDelta = delta;
            return;
        }

        hasPendingArrow = true;
        pendingArrowMotorID = motorID;
        pendingArrowUseHorizontal = false;
        pendingArrowUseLeftSide = false;
        pendingArrowDelta = delta;
    }

    private void EnsureThumbPronationMinBaselineInitialized()
    {
        if (!Mathf.Approximately(controller.currentThumbRotationYMin, 0f))
        {
            return;
        }

        float wrappedY = Mathf.Repeat(controller.thumbGripperJoint1MinRotationVector.y, 360f);
        if (Mathf.Abs(wrappedY - 60f) > 0.5f)
        {
            return;
        }

        controller.currentThumbRotationYMin = 60f;
        Vector3 minVec = controller.thumbGripperJoint1MinRotationVector;
        minVec.y = 60f;
        controller.thumbGripperJoint1MinRotationVector = minVec;
        controller.minThumbYAxisAngle = controller.KeyboardNormalizeAngle(minVec.y);
    }

    private void EnsureIndexPronationMinBaselineInitialized()
    {
        if (!Mathf.Approximately(controller.currentIndexRotationYMin, 0f))
        {
            return;
        }

        float wrappedY = Mathf.Repeat(controller.indexGripperJoint1MinRotationVector.y, 360f);
        if (Mathf.Abs(wrappedY - 60f) > 0.5f)
        {
            return;
        }

        controller.currentIndexRotationYMin = 60f;
        Vector3 minVec = controller.indexGripperJoint1MinRotationVector;
        minVec.y = 60f;
        controller.indexGripperJoint1MinRotationVector = minVec;
        controller.minIndexYAxisAngle = controller.KeyboardNormalizeAngle(minVec.y);
    }

    private void KbApplyRotation(int row, int col, float delta)
    {
        int motorID = GetMotorIDForCell(row, col);
        if (motorID >= 13 && motorID <= 15)
        {
            return;
        }

        if (motorID > 0 && IsSingleFrozen(motorID))
        {
            hasPendingArrow = false;
            controller.ClearArmUIDirectAngleArrowState();
            return;
        }

        float effectiveDelta = delta;
        // Extension rows use reversed keyboard direction (Q/E opposite of previous behavior).
        if (row >= 2)
        {
            effectiveDelta = -delta;
        }

        bool changed = false;

        switch (row)
        {
            case 0:
                switch (col)
                {
                    case 0:
                        if (sideLockedMotorID != 1)
                        {
                            sideLockedMotorID = 1;
                            sideLockedUseLeftSide = effectiveDelta < 0f;
                        }

                        if (sideLockedUseLeftSide)
                        {
                            float prevThumbYMax = controller.currentThumbRotationYMax;
                            controller.currentThumbRotationYMax += effectiveDelta;
                            controller.currentThumbRotationYMax = Mathf.Clamp(controller.currentThumbRotationYMax, -90f, 0f);
                            changed = !Mathf.Approximately(prevThumbYMax, controller.currentThumbRotationYMax);
                            controller.thumbGripperJoint1MaxRotationVector =
                                (controller.KeyboardThumbAngle1InitialRotation * Quaternion.Euler(0f, controller.currentThumbRotationYMax, 0f)).eulerAngles;
                            controller.maxThumbYAxisAngle = controller.KeyboardNormalizeAngle(controller.thumbGripperJoint1MaxRotationVector.y);
                        }
                        else
                        {
                            EnsureThumbPronationMinBaselineInitialized();

                            float prevThumbYMin = controller.currentThumbRotationYMin;
                            controller.currentThumbRotationYMin += effectiveDelta;
                            controller.currentThumbRotationYMin = Mathf.Clamp(controller.currentThumbRotationYMin, 0f, 90f);
                            changed = !Mathf.Approximately(prevThumbYMin, controller.currentThumbRotationYMin);
                            controller.thumbGripperJoint1MinRotationVector =
                                (controller.KeyboardThumbAngle1InitialRotation * Quaternion.Euler(0f, controller.currentThumbRotationYMin, 0f)).eulerAngles;
                            controller.minThumbYAxisAngle = controller.KeyboardNormalizeAngle(controller.thumbGripperJoint1MinRotationVector.y);
                        }
                        break;
                    case 1:
                        if (sideLockedMotorID != 5)
                        {
                            sideLockedMotorID = 5;
                            sideLockedUseLeftSide = effectiveDelta < 0f;
                        }

                        if (sideLockedUseLeftSide)
                        {
                            float prevIndexYMax = controller.currentIndexRotationYMax;
                            controller.currentIndexRotationYMax += effectiveDelta;
                            controller.currentIndexRotationYMax = Mathf.Clamp(controller.currentIndexRotationYMax, -90f, 0f);
                            changed = !Mathf.Approximately(prevIndexYMax, controller.currentIndexRotationYMax);
                            controller.indexGripperJoint1MaxRotationVector = controller.KeyboardGetIndexJoint1MaxRotationVector();
                            controller.maxIndexYAxisAngle = controller.KeyboardNormalizeAngle(controller.indexGripperJoint1MaxRotationVector.y);
                        }
                        else
                        {
                            EnsureIndexPronationMinBaselineInitialized();

                            float prevIndexYMin = controller.currentIndexRotationYMin;
                            controller.currentIndexRotationYMin += effectiveDelta;
                            controller.currentIndexRotationYMin = Mathf.Clamp(controller.currentIndexRotationYMin, 0f, 90f);
                            changed = !Mathf.Approximately(prevIndexYMin, controller.currentIndexRotationYMin);
                            controller.indexGripperJoint1MinRotationVector =
                                (controller.KeyboardIndexAngle1InitialRotation * Quaternion.Euler(0f, controller.currentIndexRotationYMin, 0f)).eulerAngles;
                            controller.minIndexYAxisAngle = controller.KeyboardNormalizeAngle(controller.indexGripperJoint1MinRotationVector.y);
                        }
                        break;
                    case 2:
                        if (sideLockedMotorID != 9)
                        {
                            sideLockedMotorID = 9;
                            sideLockedUseLeftSide = effectiveDelta < 0f;
                        }

                        if (sideLockedUseLeftSide)
                        {
                            float prevMiddleYMax = controller.currentMiddleRotationYMax;
                            controller.currentMiddleRotationYMax += effectiveDelta;
                            controller.currentMiddleRotationYMax = Mathf.Clamp(controller.currentMiddleRotationYMax, -90f, 0f);
                            changed = !Mathf.Approximately(prevMiddleYMax, controller.currentMiddleRotationYMax);
                            controller.middleGripperJoint1MaxRotationVector = controller.KeyboardGetMiddleJoint1MaxRotationVector();
                            controller.maxMiddleYAxisAngle = controller.KeyboardNormalizeMiddleJoint1MaxAngle(controller.middleGripperJoint1MaxRotationVector.y);
                            controller.KeyboardRefreshMiddleJoint1YDebug("KbApplyRotation:max");
                        }
                        else
                        {
                            float prevMiddleYMin = controller.currentMiddleRotationYMin;
                            controller.currentMiddleRotationYMin += effectiveDelta;
                            controller.currentMiddleRotationYMin = Mathf.Clamp(controller.currentMiddleRotationYMin, 0f, 90f);
                            changed = !Mathf.Approximately(prevMiddleYMin, controller.currentMiddleRotationYMin);
                            controller.middleGripperJoint1MinRotationVector = controller.KeyboardGetMiddleJoint1MinRotationVector();
                            controller.minMiddleYAxisAngle = controller.KeyboardNormalizeAngle(controller.middleGripperJoint1MinRotationVector.y);
                            controller.KeyboardRefreshMiddleJoint1YDebug("KbApplyRotation:min");
                        }
                        break;
                }
                break;

            case 1:
                switch (col)
                {
                    case 0:
                        if (sideLockedMotorID != 2)
                        {
                            sideLockedMotorID = 2;
                            sideLockedUseLeftSide = effectiveDelta < 0f;
                        }

                        if (sideLockedUseLeftSide)
                        {
                            float prevThumbZMax = controller.currentThumbRotationZMax;
                            controller.currentThumbRotationZMax += effectiveDelta;
                            controller.currentThumbRotationZMax = Mathf.Clamp(controller.currentThumbRotationZMax, -90f, 0f);
                            changed = !Mathf.Approximately(prevThumbZMax, controller.currentThumbRotationZMax);
                            controller.thumbGripperJoint2MaxRotationVector =
                                (controller.KeyboardThumbAngle2InitialRotation * Quaternion.Euler(0f, 0f, controller.currentThumbRotationZMax)).eulerAngles;
                            if (controller.thumbGripperJoint2MaxRotationVector.z < 1f) controller.thumbGripperJoint2MaxRotationVector.z = 360f;
                            controller.maxThumbZAxisAngle = controller.thumbGripperJoint2MaxRotationVector.z;
                        }
                        else
                        {
                            float prevThumbZMin = controller.currentThumbRotationZMin;
                            controller.currentThumbRotationZMin += effectiveDelta;
                            controller.currentThumbRotationZMin = Mathf.Clamp(controller.currentThumbRotationZMin, 0f, 90f);
                            changed = !Mathf.Approximately(prevThumbZMin, controller.currentThumbRotationZMin);
                            controller.thumbGripperJoint2MinRotationVector =
                                (controller.KeyboardThumbAngle2InitialRotation * Quaternion.Euler(0f, 0f, controller.currentThumbRotationZMin)).eulerAngles;
                            controller.minThumbZAxisAngle = controller.thumbGripperJoint2MinRotationVector.z;
                        }

                        controller.hasThumbAbductionAdjustment = true;
                        break;
                    case 1:
                        if (sideLockedMotorID != 6)
                        {
                            sideLockedMotorID = 6;
                            sideLockedUseLeftSide = effectiveDelta < 0f;
                        }

                        if (sideLockedUseLeftSide)
                        {
                            float prevIndexZMax = controller.currentIndexRotationZMax;
                            controller.currentIndexRotationZMax += effectiveDelta;
                            controller.currentIndexRotationZMax = Mathf.Clamp(controller.currentIndexRotationZMax, -90f, 0f);
                            changed = !Mathf.Approximately(prevIndexZMax, controller.currentIndexRotationZMax);
                            controller.indexGripperJoint2MaxRotationVector =
                                (controller.KeyboardIndexAngle2InitialRotation * Quaternion.Euler(0f, 0f, controller.currentIndexRotationZMax)).eulerAngles;
                            controller.maxIndexZAxisAngle = controller.indexGripperJoint2MaxRotationVector.z;
                        }
                        else
                        {
                            float prevIndexZMin = controller.currentIndexRotationZMin;
                            controller.currentIndexRotationZMin += effectiveDelta;
                            controller.currentIndexRotationZMin = Mathf.Clamp(controller.currentIndexRotationZMin, 0f, 90f);
                            changed = !Mathf.Approximately(prevIndexZMin, controller.currentIndexRotationZMin);
                            controller.indexGripperJoint2MinRotationVector =
                                (controller.KeyboardIndexAngle2InitialRotation * Quaternion.Euler(0f, 0f, controller.currentIndexRotationZMin)).eulerAngles;
                            controller.minIndexZAxisAngle = controller.indexGripperJoint2MinRotationVector.z;
                        }
                        break;
                    case 2:
                        if (sideLockedMotorID != 10)
                        {
                            sideLockedMotorID = 10;
                            sideLockedUseLeftSide = effectiveDelta < 0f;
                        }

                        if (sideLockedUseLeftSide)
                        {
                            float prevMiddleZMax = controller.currentMiddleRotationZMax;
                            controller.currentMiddleRotationZMax += effectiveDelta;
                            controller.currentMiddleRotationZMax = Mathf.Clamp(controller.currentMiddleRotationZMax, -90f, 0f);
                            changed = !Mathf.Approximately(prevMiddleZMax, controller.currentMiddleRotationZMax);
                            controller.middleGripperJoint2MaxRotationVector = controller.KeyboardGetMiddleJoint2MaxRotationVector();
                            controller.maxMiddleZAxisAngle = controller.middleGripperJoint2MaxRotationVector.z;
                        }
                        else
                        {
                            float prevMiddleZMin = controller.currentMiddleRotationZMin;
                            controller.currentMiddleRotationZMin += effectiveDelta;
                            controller.currentMiddleRotationZMin = Mathf.Clamp(controller.currentMiddleRotationZMin, 0f, 90f);
                            changed = !Mathf.Approximately(prevMiddleZMin, controller.currentMiddleRotationZMin);
                            controller.middleGripperJoint2MinRotationVector =
                                (controller.KeyboardMiddleAngle2InitialRotation * Quaternion.Euler(0f, 0f, controller.currentMiddleRotationZMin)).eulerAngles;
                            controller.minMiddleZAxisAngle = controller.middleGripperJoint2MinRotationVector.z;
                        }
                        break;
                }
                break;

            case 2:
                switch (col)
                {
                    case 0:
                        float prevThumbInner = controller.currentThumbInnerExtensionRotationZ;
                        controller.currentThumbInnerExtensionRotationZ += effectiveDelta;
                        controller.currentThumbInnerExtensionRotationZ = Mathf.Clamp(controller.currentThumbInnerExtensionRotationZ, controller.KeyboardExtensionClampMin, controller.KeyboardExtensionClampMax);
                        changed = !Mathf.Approximately(prevThumbInner, controller.currentThumbInnerExtensionRotationZ);
                        break;
                    case 1:
                        float prevIndexInner = controller.currentIndexInnerExtensionRotationZ;
                        controller.currentIndexInnerExtensionRotationZ += effectiveDelta;
                        controller.currentIndexInnerExtensionRotationZ = Mathf.Clamp(controller.currentIndexInnerExtensionRotationZ, controller.KeyboardExtensionClampMin, controller.KeyboardExtensionClampMax);
                        changed = !Mathf.Approximately(prevIndexInner, controller.currentIndexInnerExtensionRotationZ);
                        break;
                    case 2:
                        float prevMiddleInner = controller.currentMiddleInnerExtensionRotationZ;
                        controller.currentMiddleInnerExtensionRotationZ += effectiveDelta;
                        controller.currentMiddleInnerExtensionRotationZ = Mathf.Clamp(controller.currentMiddleInnerExtensionRotationZ, controller.KeyboardExtensionClampMin, controller.KeyboardExtensionClampMax);
                        changed = !Mathf.Approximately(prevMiddleInner, controller.currentMiddleInnerExtensionRotationZ);
                        break;
                }
                break;

            case 3:
                switch (col)
                {
                    case 0:
                        float prevThumbTip = controller.currentThumbTipRotationZ;
                        controller.currentThumbTipRotationZ += effectiveDelta;
                        controller.currentThumbTipRotationZ = Mathf.Clamp(controller.currentThumbTipRotationZ, controller.KeyboardExtensionClampMin, controller.KeyboardExtensionClampMax);
                        changed = !Mathf.Approximately(prevThumbTip, controller.currentThumbTipRotationZ);
                        break;
                    case 1:
                        float prevIndexTip = controller.currentIndexTipRotationZ;
                        controller.currentIndexTipRotationZ += effectiveDelta;
                        controller.currentIndexTipRotationZ = Mathf.Clamp(controller.currentIndexTipRotationZ, controller.KeyboardExtensionClampMin, controller.KeyboardExtensionClampMax);
                        changed = !Mathf.Approximately(prevIndexTip, controller.currentIndexTipRotationZ);
                        break;
                    case 2:
                        float prevMiddleTip = controller.currentMiddleTipRotationZ;
                        controller.currentMiddleTipRotationZ += effectiveDelta;
                        controller.currentMiddleTipRotationZ = Mathf.Clamp(controller.currentMiddleTipRotationZ, controller.KeyboardExtensionClampMin, controller.KeyboardExtensionClampMax);
                        changed = !Mathf.Approximately(prevMiddleTip, controller.currentMiddleTipRotationZ);
                        break;
                }
                break;
        }

        if (changed)
        {
            SyncKeyboardArrowForCell(row, col, effectiveDelta);
        }
    }
}

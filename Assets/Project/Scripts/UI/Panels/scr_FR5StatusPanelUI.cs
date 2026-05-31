using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// FR5 Status Panel UI
///
/// 역할:
/// 1. 오른쪽 Status 패널의 상태값을 갱신한다.
/// 2. Robot TCP / FK / Python 비교 값을 표시한다.
/// 3. TopBar / BottomBar와 동일한 Runtime Summary 값을 표시한다.
/// 4. Unity Replay Source가 선택된 경우 System Status 영역에 replay 상태를 우선 표시한다.
/// </summary>
public class scr_FR5StatusPanelUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private scr_FR5RobotManualController robotController;
    [SerializeField] private scr_FR5RuntimeSyncManager runtimeSyncManagerBehaviour;
    [SerializeField] private scr_FR5CSharpBridgeClient cSharpBridgeClient;
    [SerializeField] private scr_FR5UnityReplayJointStateSource unityReplaySource;

    [Header("Runtime Summary UI")]
    [SerializeField] private TMP_Text modeSummaryText;
    [SerializeField] private TMP_Text sourceSummaryText;
    [SerializeField] private TMP_Text robotStateSummaryText;
    [SerializeField] private TMP_Text validSummaryText;

    [Header("Status UI")]
    [SerializeField] private TMP_Text modeText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text alarmText;

    [Header("Relative Position UI")]
    [SerializeField] private TMP_Text deltaXText;
    [SerializeField] private TMP_Text deltaYText;
    [SerializeField] private TMP_Text deltaZText;

    [Header("TCP Local Position UI")]
    [SerializeField] private TMP_Text basePosXText;
    [SerializeField] private TMP_Text basePosYText;
    [SerializeField] private TMP_Text basePosZText;

    [Header("TCP Local Rotation UI")]
    [SerializeField] private TMP_Text tcpRotXText;
    [SerializeField] private TMP_Text tcpRotYText;
    [SerializeField] private TMP_Text tcpRotZText;

    [Header("FK Position UI")]
    [SerializeField] private TMP_Text fkXText;
    [SerializeField] private TMP_Text fkYText;
    [SerializeField] private TMP_Text fkZText;

    [Header("Python Position UI")]
    [SerializeField] private TMP_Text pyXText;
    [SerializeField] private TMP_Text pyYText;
    [SerializeField] private TMP_Text pyZText;

    [Header("Error UI - Unity vs FK")]
    [SerializeField] private TMP_Text errorXText;
    [SerializeField] private TMP_Text errorYText;
    [SerializeField] private TMP_Text errorZText;

    [Header("Error UI - Unity vs Python")]
    [SerializeField] private TMP_Text errorPyXText;
    [SerializeField] private TMP_Text errorPyYText;
    [SerializeField] private TMP_Text errorPyZText;

    [Header("Options")]
    [SerializeField] private bool autoRefreshInUpdate = true;
    [SerializeField] private float refreshIntervalSeconds = 0.05f;

    private float nextRefreshTime = 0f;

    private void Awake()
    {
        ResolveOptionalReferences();
    }

    private void Start()
    {
        RefreshUI();
    }

    private void Update()
    {
        if (!autoRefreshInUpdate)
        {
            return;
        }

        if (Time.time < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.time + refreshIntervalSeconds;
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (robotController == null)
        {
            return;
        }

        ResolveOptionalReferences();

        Vector3 unityPos = robotController.GetCurrentUnityTCPLocalPosition();
        Vector3 unityRot = robotController.GetCurrentUnityTCPLocalRotation();
        Vector3 fkPos = robotController.GetCurrentFKTCPLocalPosition();
        Vector3 pyPos = robotController.GetCurrentPythonTCPLocalPosition();
        Vector3 errFK = robotController.GetCurrentTCPErrorUnityVsFKLocal();
        Vector3 errPY = robotController.GetCurrentTCPErrorUnityVsPythonLocal();
        Vector3 relative = robotController.GetCurrentRelativeDeltaLocal();

        RefreshRuntimeSummary();
        RefreshSystemStatus();

        if (deltaXText != null) deltaXText.text = FormatMeters("DX", relative.x);
        if (deltaYText != null) deltaYText.text = FormatMeters("DY", relative.y);
        if (deltaZText != null) deltaZText.text = FormatMeters("DZ", relative.z);

        if (basePosXText != null) basePosXText.text = FormatMeters("X", unityPos.x);
        if (basePosYText != null) basePosYText.text = FormatMeters("Y", unityPos.y);
        if (basePosZText != null) basePosZText.text = FormatMeters("Z", unityPos.z);

        if (tcpRotXText != null) tcpRotXText.text = FormatDegrees("RX", unityRot.x);
        if (tcpRotYText != null) tcpRotYText.text = FormatDegrees("RY", unityRot.y);
        if (tcpRotZText != null) tcpRotZText.text = FormatDegrees("RZ", unityRot.z);

        if (fkXText != null) fkXText.text = FormatMeters("FK X", fkPos.x);
        if (fkYText != null) fkYText.text = FormatMeters("FK Y", fkPos.y);
        if (fkZText != null) fkZText.text = FormatMeters("FK Z", fkPos.z);

        if (robotController.IsPythonTCPLoaded())
        {
            if (pyXText != null) pyXText.text = FormatMeters("PY X", pyPos.x);
            if (pyYText != null) pyYText.text = FormatMeters("PY Y", pyPos.y);
            if (pyZText != null) pyZText.text = FormatMeters("PY Z", pyPos.z);

            if (errorPyXText != null) errorPyXText.text = FormatError("EPX", errPY.x);
            if (errorPyYText != null) errorPyYText.text = FormatError("EPY", errPY.y);
            if (errorPyZText != null) errorPyZText.text = FormatError("EPZ", errPY.z);
        }
        else
        {
            if (pyXText != null) pyXText.text = "PY X : LOAD FAIL";
            if (pyYText != null) pyYText.text = "PY Y : LOAD FAIL";
            if (pyZText != null) pyZText.text = "PY Z : LOAD FAIL";

            if (errorPyXText != null) errorPyXText.text = "EPX : ---";
            if (errorPyYText != null) errorPyYText.text = "EPY : ---";
            if (errorPyZText != null) errorPyZText.text = "EPZ : ---";
        }

        if (errorXText != null) errorXText.text = FormatError("EFX", errFK.x);
        if (errorYText != null) errorYText.text = FormatError("EFY", errFK.y);
        if (errorZText != null) errorZText.text = FormatError("EFZ", errFK.z);
    }

    private void ResolveOptionalReferences()
    {
        if (unityReplaySource == null)
        {
            unityReplaySource = FindObjectOfType<scr_FR5UnityReplayJointStateSource>();
        }
    }

    private void RefreshRuntimeSummary()
    {
        string modeLabel = runtimeSyncManagerBehaviour != null
            ? runtimeSyncManagerBehaviour.GetFormattedModeLabel()
            : "N/A";

        string sourceLabel = runtimeSyncManagerBehaviour != null
            ? runtimeSyncManagerBehaviour.GetFormattedSourceLabel()
            : "N/A";

        string robotStateLabel = runtimeSyncManagerBehaviour != null
            ? runtimeSyncManagerBehaviour.GetSelectedSourceRobotStateLabel()
            : (cSharpBridgeClient != null ? cSharpBridgeClient.GetDisplayRobotStateLabel() : "Unknown");

        string validLabel = runtimeSyncManagerBehaviour != null
            ? runtimeSyncManagerBehaviour.GetFormattedValidLabel()
            : "FALSE";

        if (modeSummaryText != null)
        {
            modeSummaryText.text = $"MODE : {modeLabel}";
        }

        if (sourceSummaryText != null)
        {
            sourceSummaryText.text = $"SOURCE : {sourceLabel}";
        }

        if (robotStateSummaryText != null)
        {
            robotStateSummaryText.text = $"ROBOT : {robotStateLabel.ToUpperInvariant()}";
        }

        if (validSummaryText != null)
        {
            validSummaryText.text = $"VALID : {validLabel}";
        }
    }

    private void RefreshSystemStatus()
    {
        if (IsUnityReplayRuntimeActive() && unityReplaySource != null)
        {
            if (modeText != null)
            {
                modeText.text = "MODE : LIVE REPLAY";
            }

            if (speedText != null)
            {
                speedText.text = $"SPEED : {unityReplaySource.PlaybackSpeed:0.##}x";
            }

            if (stateText != null)
            {
                stateText.text = $"STATE : {unityReplaySource.GetReplayStateLabel()} | {unityReplaySource.GetReplayTimeLabel()}";
            }

            if (alarmText != null)
            {
                alarmText.text = unityReplaySource.LastSampleValid ? "ALARM : NONE" : "ALARM : REPLAY SAMPLE INVALID";
            }

            return;
        }

        if (modeText != null) modeText.text = $"MODE : {robotController.GetCurrentModeLabel().ToUpperInvariant()}";
        if (speedText != null) speedText.text = $"SPEED : {robotController.GetCurrentSpeedLabel().ToUpperInvariant()}";
        if (stateText != null) stateText.text = $"STATE : {GetStatusStateLabel()}";
        if (alarmText != null) alarmText.text = $"ALARM : {GetAlarmLabel()}";
    }

    private bool IsUnityReplayRuntimeActive()
    {
        return runtimeSyncManagerBehaviour != null &&
               runtimeSyncManagerBehaviour.SelectedRuntimeSource == scr_FR5RuntimeSyncManager.RuntimeSourceType.UnityReplay;
    }

    private string GetStatusStateLabel()
    {
        string controllerState = robotController != null ? robotController.GetCurrentStateLabel() : "Ready";

        if (runtimeSyncManagerBehaviour == null)
        {
            return controllerState.ToUpperInvariant();
        }

        return runtimeSyncManagerBehaviour.GetEffectiveSystemStateLabel(controllerState);
    }

    private string GetAlarmLabel()
    {
        string controllerAlarm = robotController != null ? robotController.GetCurrentAlarmLabel() : "No Alarm";

        if (runtimeSyncManagerBehaviour == null ||
            runtimeSyncManagerBehaviour.SelectedRuntimeSource != scr_FR5RuntimeSyncManager.RuntimeSourceType.CSharpBridge ||
            cSharpBridgeClient == null)
        {
            return controllerAlarm.ToUpperInvariant();
        }

        string bridgeState = cSharpBridgeClient.GetDisplayRobotStateLabel();
        if (bridgeState == "Disconnected" || bridgeState == "Poll Failed" || bridgeState == "Bridge File Missing")
        {
            return bridgeState.ToUpperInvariant();
        }

        return controllerAlarm.ToUpperInvariant();
    }

    private string FormatMeters(string label, float value)
    {
        return $"{label} : {value:+0.000;-0.000;0.000}";
    }

    private string FormatDegrees(string label, float value)
    {
        return $"{label} : {value:+0.0;-0.0;0.0}°";
    }

    private string FormatError(string label, float value)
    {
        return $"{label} : {value:+0.0000;-0.0000;0.0000}";
    }
}

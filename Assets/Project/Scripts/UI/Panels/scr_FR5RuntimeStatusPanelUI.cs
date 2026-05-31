using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FR5   Ÿ         г  UI
///
///     :
/// 1. TopBar   Runtime    ¸  ǥ   Ѵ .
/// 2. RightDock Summary   MODE / SOURCE / ROBOT / VALID    ¸  ǥ   Ѵ .
/// 3. RightDock TCP / DELTA / ROT      ǥ   Ѵ .
/// 4. RightDock Runtime       ¸  ǥ   Ѵ .
/// 5. RightDock Compare      ǥ   Ѵ .
/// 6. RightDock Alarm    ¸  ǥ   Ѵ .
///
///   Ģ:
/// - RectTransform, Anchor,   ġ, ũ                   ʴ´ .
/// -      ũ  Ʈ   Text, Image     ,                Ѵ .
/// </summary>
public class scr_FR5RuntimeStatusPanelUI : MonoBehaviour
{
    [Header("    ")]
    [SerializeField] private scr_FR5RuntimeSyncManager runtimeSyncManager;
    [SerializeField] private scr_FR5CSharpBridgeClient cSharpBridgeClient;
    [SerializeField] private scr_FR5CSharpSdkClient cSharpSdkClient;
    [SerializeField] private scr_FR5RobotManualController robotController;
    [SerializeField] private scr_FR5UnityReplayJointStateSource unityReplaySource;

    [Header("TopBar     ؽ Ʈ")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text modeText;
    [SerializeField] private TMP_Text sourceText;
    [SerializeField] private TMP_Text robotStateText;
    [SerializeField] private TMP_Text lastPollText;
    [SerializeField] private TMP_Text lastMessageText;

    [Header("TopBar     ؽ Ʈ")]
    [SerializeField] private TMP_Text modeLabelText;
    [SerializeField] private TMP_Text sourceLabelText;
    [SerializeField] private TMP_Text robotLabelText;
    [SerializeField] private TMP_Text lastPollLabelText;
    [SerializeField] private TMP_Text lastMessageLabelText;
    [SerializeField] private TMP_Text connectionLabelText;
    [SerializeField] private TMP_Text validLabelText;

    [Header("TopBar        Ʈ")]
    [SerializeField] private GameObject lastPollBlockRoot;
    [SerializeField] private GameObject lastMessageBlockRoot;

    [Header("TopBar       ̹   ")]
    [SerializeField] private Image modeBlockGraphic;
    [SerializeField] private Image sourceBlockGraphic;
    [SerializeField] private Image robotStateBlockGraphic;
    [SerializeField] private Image connectionBlockGraphic;
    [SerializeField] private Image validBlockGraphic;

    [Header("     /       ̹   ")]
    [SerializeField] private Image connectionLamp;
    [SerializeField] private Image sampleValidLamp;

    [Header("RightDock Summary  ؽ Ʈ")]
    [SerializeField] private TMP_Text rightModeSummaryText;
    [SerializeField] private TMP_Text rightSourceSummaryText;
    [SerializeField] private TMP_Text rightRobotStateSummaryText;
    [SerializeField] private TMP_Text rightValidSummaryText;

    [Header("RightDock TCP / Delta  ؽ Ʈ")]
    [SerializeField] private TMP_Text rightRelativeXText;
    [SerializeField] private TMP_Text rightRelativeYText;
    [SerializeField] private TMP_Text rightRelativeZText;

    [SerializeField] private TMP_Text rightTcpPosXText;
    [SerializeField] private TMP_Text rightTcpPosYText;
    [SerializeField] private TMP_Text rightTcpPosZText;

    [SerializeField] private TMP_Text rightTcpRotXText;
    [SerializeField] private TMP_Text rightTcpRotYText;
    [SerializeField] private TMP_Text rightTcpRotZText;

    [Header("RightDock Runtime  ؽ Ʈ")]
    [SerializeField] private TMP_Text rightLastPollText;
    [SerializeField] private TMP_Text rightRuntimeModeText;
    [SerializeField] private TMP_Text rightRuntimeSpeedText;
    [SerializeField] private TMP_Text rightRuntimeStateText;

    [Header("RightDock Compare - FK   ġ")]
    [SerializeField] private TMP_Text rightFkXText;
    [SerializeField] private TMP_Text rightFkYText;
    [SerializeField] private TMP_Text rightFkZText;

    [Header("RightDock Compare - Python   ġ")]
    [SerializeField] private TMP_Text rightPythonXText;
    [SerializeField] private TMP_Text rightPythonYText;
    [SerializeField] private TMP_Text rightPythonZText;

    [Header("RightDock Compare - Unity    FK     ")]
    [SerializeField] private TMP_Text rightErrorFkXText;
    [SerializeField] private TMP_Text rightErrorFkYText;
    [SerializeField] private TMP_Text rightErrorFkZText;

    [Header("RightDock Compare - Unity    Python     ")]
    [SerializeField] private TMP_Text rightErrorPythonXText;
    [SerializeField] private TMP_Text rightErrorPythonYText;
    [SerializeField] private TMP_Text rightErrorPythonZText;

    [Header("RightDock Alarm  ؽ Ʈ")]
    [SerializeField] private TMP_Text rightAlarmText;

    [Header("ǥ       ")]
    [SerializeField] private string panelTitle = "FR5 Digital Twin Runtime";
    [SerializeField] private string defaultRobotModeLabel = "MANUAL";
    [SerializeField] private string defaultSpeedLabel = "100%";
    [SerializeField] private string noAlarmLabel = "NO ALARM";
    [SerializeField] private string pythonPausedLabel = "PAUSED";
    [SerializeField] private string emptyValueLabel = "---";

    [Header("ǥ    ɼ ")]
    [SerializeField] private bool showRuntimeMessageAsAlarm = false;
    [SerializeField] private bool showPythonPausedWhenMissing = true;

    [Header("         ")]
    [SerializeField] private Color connectedColor = new Color(0.0f, 0.85f, 0.55f, 1.0f);
    [SerializeField] private Color disconnectedColor = new Color(0.75f, 0.2f, 0.2f, 1.0f);
    [SerializeField] private Color validColor = new Color(0.1f, 0.9f, 0.6f, 1.0f);
    [SerializeField] private Color invalidColor = new Color(0.95f, 0.55f, 0.15f, 1.0f);

    [Header("TopBar          ")]
    [SerializeField] private Color liveModeBlockColor = new Color(0.10f, 0.55f, 0.90f, 0.95f);
    [SerializeField] private Color simModeBlockColor = new Color(0.22f, 0.32f, 0.45f, 0.95f);
    [SerializeField] private Color sourceBlockColor = new Color(0.18f, 0.30f, 0.48f, 0.95f);
    [SerializeField] private Color robotReadyBlockColor = new Color(0.12f, 0.56f, 0.36f, 0.95f);
    [SerializeField] private Color robotDisconnectedBlockColor = new Color(0.56f, 0.18f, 0.18f, 0.95f);
    [SerializeField] private Color robotInvalidBlockColor = new Color(0.62f, 0.38f, 0.10f, 0.95f);
    [SerializeField] private Color robotNeutralBlockColor = new Color(0.22f, 0.30f, 0.40f, 0.95f);
    [SerializeField] private Color connectionBlockStaticColor = new Color(0.18f, 0.30f, 0.48f, 0.95f);
    [SerializeField] private Color validBlockStaticColor = new Color(0.18f, 0.30f, 0.48f, 0.95f);

    [Header(" ؽ Ʈ     ")]
    [SerializeField] private Color labelColor = new Color(0.76f, 0.84f, 0.90f, 0.92f);
    [SerializeField] private Color valueColor = Color.white;

    [Header("      ɼ ")]
    [SerializeField] private bool autoRefreshInUpdate = true;
    [SerializeField] private float refreshIntervalSeconds = 0.1f;

    private float nextRefreshTime = 0f;

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

    public void SetDetailBlocksVisible(bool showLastPoll, bool showLastMessage)
    {
        if (lastPollBlockRoot != null)
        {
            lastPollBlockRoot.SetActive(showLastPoll);
        }

        if (lastMessageBlockRoot != null)
        {
            lastMessageBlockRoot.SetActive(showLastMessage);
        }
    }

    public void RefreshUI()
    {
        ApplyStaticLabels();

        SetValue(titleText, panelTitle);

        if (runtimeSyncManager == null)
        {
            ApplyNoReferenceState();
            return;
        }

        string formattedMode = runtimeSyncManager.GetFormattedModeLabel();
        string formattedSource = runtimeSyncManager.GetFormattedSourceLabel();
        string formattedRobotState = runtimeSyncManager.GetSelectedSourceRobotStateLabel();
        string formattedLastPoll = runtimeSyncManager.GetEffectiveLastPollLabel();
        string formattedMessage = runtimeSyncManager.GetEffectiveMessageLabel();

        bool connected = runtimeSyncManager.ActiveSourceConnected;
        bool sampleValid = runtimeSyncManager.LastSampleValid;

        if (runtimeSyncManager.SelectedRuntimeSource == scr_FR5RuntimeSyncManager.RuntimeSourceType.CSharpBridge &&
            cSharpBridgeClient != null)
        {
            connected = cSharpBridgeClient.IsConnected;
            sampleValid = cSharpBridgeClient.LastSampleValid;
        }

        ApplyTopBarValues(
            formattedMode,
            formattedSource,
            formattedRobotState,
            formattedLastPoll,
            formattedMessage
        );

        ApplyRightSummaryValues(
            formattedMode,
            formattedSource,
            formattedRobotState,
            sampleValid
        );

        ApplyTcpAndCompareValues();
        ApplyRightRuntimeValues(formattedLastPoll, sampleValid);
        ApplyRightAlarmValues(formattedMessage);

        SetLamp(connectionLamp, connected, connectedColor, disconnectedColor);
        SetLamp(sampleValidLamp, sampleValid, validColor, invalidColor);

        ApplyBlockColors(formattedMode, formattedRobotState);
    }

    private void ApplyTopBarValues(
        string formattedMode,
        string formattedSource,
        string formattedRobotState,
        string formattedLastPoll,
        string formattedMessage)
    {
        SetValue(modeText, formattedMode);
        SetValue(sourceText, formattedSource);
        SetValue(robotStateText, formattedRobotState);
        SetValue(lastPollText, formattedLastPoll);
        SetValue(lastMessageText, formattedMessage);
    }

    private void ApplyRightSummaryValues(
        string formattedMode,
        string formattedSource,
        string formattedRobotState,
        bool sampleValid)
    {
        SetValue(rightModeSummaryText, $"MODE : {formattedMode}");
        SetValue(rightSourceSummaryText, $"SOURCE : {formattedSource}");
        SetValue(rightRobotStateSummaryText, $"ROBOT : {formattedRobotState}");
        SetValue(rightValidSummaryText, $"VALID : {sampleValid.ToString().ToUpperInvariant()}");
    }

    private void ApplyTcpAndCompareValues()
    {
        if (robotController == null)
        {
            ApplyNoRobotControllerTcpState();
            return;
        }

        robotController.ForceRefreshStatus();

        Vector3 tcpPos = robotController.GetCurrentUnityTCPLocalPosition();
        Vector3 tcpRot = robotController.GetCurrentUnityTCPLocalRotation();
        Vector3 delta = robotController.GetCurrentRelativeDeltaLocal();

        Vector3 fkPos = robotController.GetCurrentFKTCPLocalPosition();
        Vector3 pyPos = robotController.GetCurrentPythonTCPLocalPosition();

        Vector3 errorFk = robotController.GetCurrentTCPErrorUnityVsFKLocal();
        Vector3 errorPy = robotController.GetCurrentTCPErrorUnityVsPythonLocal();

        bool pythonLoaded = robotController.IsPythonTCPLoaded();

        SetValue(rightTcpPosXText, $"X : {FormatSigned3(tcpPos.x)}");
        SetValue(rightTcpPosYText, $"Y : {FormatSigned3(tcpPos.y)}");
        SetValue(rightTcpPosZText, $"Z : {FormatSigned3(tcpPos.z)}");

        SetValue(rightTcpRotXText, $"RX : {FormatSigned1(tcpRot.x)}");
        SetValue(rightTcpRotYText, $"RY : {FormatSigned1(tcpRot.y)}");
        SetValue(rightTcpRotZText, $"RZ : {FormatSigned1(tcpRot.z)}");

        SetValue(rightRelativeXText, $"DX : {FormatSigned3(delta.x)}");
        SetValue(rightRelativeYText, $"DY : {FormatSigned3(delta.y)}");
        SetValue(rightRelativeZText, $"DZ : {FormatSigned3(delta.z)}");

        SetValue(rightFkXText, $"FK X : {FormatSigned3(fkPos.x)}");
        SetValue(rightFkYText, $"FK Y : {FormatSigned3(fkPos.y)}");
        SetValue(rightFkZText, $"FK Z : {FormatSigned3(fkPos.z)}");

        if (pythonLoaded)
        {
            SetValue(rightPythonXText, $"PY X : {FormatSigned3(pyPos.x)}");
            SetValue(rightPythonYText, $"PY Y : {FormatSigned3(pyPos.y)}");
            SetValue(rightPythonZText, $"PY Z : {FormatSigned3(pyPos.z)}");

            SetValue(rightErrorPythonXText, $"EPX : {FormatSigned4(errorPy.x)}");
            SetValue(rightErrorPythonYText, $"EPY : {FormatSigned4(errorPy.y)}");
            SetValue(rightErrorPythonZText, $"EPZ : {FormatSigned4(errorPy.z)}");
        }
        else
        {
            string pyLabel = showPythonPausedWhenMissing ? pythonPausedLabel : emptyValueLabel;

            SetValue(rightPythonXText, $"PY X : {pyLabel}");
            SetValue(rightPythonYText, $"PY Y : {pyLabel}");
            SetValue(rightPythonZText, $"PY Z : {pyLabel}");

            SetValue(rightErrorPythonXText, $"EPX : {emptyValueLabel}");
            SetValue(rightErrorPythonYText, $"EPY : {emptyValueLabel}");
            SetValue(rightErrorPythonZText, $"EPZ : {emptyValueLabel}");
        }

        SetValue(rightErrorFkXText, $"EFX : {FormatSigned4(errorFk.x)}");
        SetValue(rightErrorFkYText, $"EFY : {FormatSigned4(errorFk.y)}");
        SetValue(rightErrorFkZText, $"EFZ : {FormatSigned4(errorFk.z)}");
    }

    private void ApplyRightRuntimeValues(string formattedLastPoll, bool sampleValid)
    {
        if (TryApplyUnityReplayRuntimeValues())
        {
            return;
        }

        SetValue(rightLastPollText, formattedLastPoll);
        SetValue(rightRuntimeModeText, $"MODE : {defaultRobotModeLabel}");

        string speedLabel = defaultSpeedLabel;

        if (cSharpSdkClient != null)
        {
            speedLabel = cSharpSdkClient.GetFormattedCommandSpeedLabel();
        }
        else if (robotController != null)
        {
            speedLabel = robotController.GetCurrentSpeedLabel();
        }

        SetValue(rightRuntimeSpeedText, $"SPEED : {speedLabel}");

        string stateLabel = sampleValid ? "READY" : "POLL FAILED";

        if (runtimeSyncManager != null &&
            runtimeSyncManager.InputMode == scr_FR5RuntimeSyncManager.RuntimeInputMode.SimManual)
        {
            stateLabel = "READY";
        }

        SetValue(rightRuntimeStateText, $"STATE : {stateLabel}");
    }

    private bool TryApplyUnityReplayRuntimeValues()
    {
        if (runtimeSyncManager == null ||
            runtimeSyncManager.SelectedRuntimeSource != scr_FR5RuntimeSyncManager.RuntimeSourceType.UnityReplay)
        {
            return false;
        }

        SetValue(rightRuntimeModeText, "MODE : LIVE REPLAY");

        if (unityReplaySource == null)
        {
            SetValue(rightLastPollText, "REPLAY : SOURCE MISSING");
            SetValue(rightRuntimeSpeedText, "SPEED : -");
            SetValue(rightRuntimeStateText, "STATE : SOURCE MISSING");
            return true;
        }

        SetValue(rightLastPollText, $"REPLAY : {unityReplaySource.GetReplayProgressLabel()}");
        SetValue(rightRuntimeSpeedText, $"SPEED : {unityReplaySource.PlaybackSpeed:0.##}x");
        SetValue(
            rightRuntimeStateText,
            $"STATE : {unityReplaySource.GetReplayStateLabel()} | {unityReplaySource.GetReplayTimeLabel()}"
        );

        return true;
    }

    private void ApplyRightAlarmValues(string formattedMessage)
    {
        if (rightAlarmText == null)
        {
            return;
        }

        if (showRuntimeMessageAsAlarm && !string.IsNullOrWhiteSpace(formattedMessage))
        {
            SetValue(rightAlarmText, formattedMessage);
            return;
        }

        if (cSharpSdkClient != null)
        {
            string lastCommandSummary = cSharpSdkClient.GetLastCommandSummary();
            string lastCommandTime = cSharpSdkClient.GetLastCommandTimeLabel();

            if (!string.IsNullOrWhiteSpace(lastCommandSummary) &&
                !string.Equals(lastCommandSummary, "#0 NONE | 100%", System.StringComparison.OrdinalIgnoreCase))
            {
                SetValue(rightAlarmText, $"{lastCommandTime} | {lastCommandSummary}");
                return;
            }
        }

        if (robotController != null)
        {
            string alarmLabel = robotController.GetCurrentAlarmLabel();

            if (!string.IsNullOrWhiteSpace(alarmLabel) &&
                !string.Equals(alarmLabel, "No Alarm", System.StringComparison.OrdinalIgnoreCase))
            {
                SetValue(rightAlarmText, alarmLabel.ToUpperInvariant());
                return;
            }
        }

        SetValue(rightAlarmText, noAlarmLabel);
    }

    private void ApplyStaticLabels()
    {
        SetLabel(modeLabelText, "MODE");
        SetLabel(sourceLabelText, "SOURCE");
        SetLabel(robotLabelText, "ROBOT");
        SetLabel(lastPollLabelText, "LAST POLL");
        SetLabel(lastMessageLabelText, "MESSAGE");
        SetLabel(connectionLabelText, "LINK");
        SetLabel(validLabelText, "VALID");
    }

    private void ApplyNoReferenceState()
    {
        SetValue(modeText, "N/A");
        SetValue(sourceText, "N/A");
        SetValue(robotStateText, "N/A");
        SetValue(lastPollText, "-");
        SetValue(lastMessageText, "RuntimeSyncManager not assigned");

        SetValue(rightModeSummaryText, "MODE : N/A");
        SetValue(rightSourceSummaryText, "SOURCE : N/A");
        SetValue(rightRobotStateSummaryText, "ROBOT : N/A");
        SetValue(rightValidSummaryText, "VALID : FALSE");

        SetValue(rightLastPollText, "-");
        SetValue(rightRuntimeModeText, $"MODE : {defaultRobotModeLabel}");
        SetValue(rightRuntimeSpeedText, $"SPEED : {defaultSpeedLabel}");
        SetValue(rightRuntimeStateText, "STATE : NO RUNTIME SOURCE");
        SetValue(rightAlarmText, "RuntimeSyncManager not assigned");

        ApplyNoRobotControllerTcpState();

        SetLamp(connectionLamp, false, connectedColor, disconnectedColor);
        SetLamp(sampleValidLamp, false, validColor, invalidColor);

        SetImageColor(modeBlockGraphic, simModeBlockColor);
        SetImageColor(sourceBlockGraphic, sourceBlockColor);
        SetImageColor(robotStateBlockGraphic, robotNeutralBlockColor);
        SetImageColor(connectionBlockGraphic, connectionBlockStaticColor);
        SetImageColor(validBlockGraphic, validBlockStaticColor);
    }

    private void ApplyNoRobotControllerTcpState()
    {
        SetValue(rightTcpPosXText, "X : 0.000");
        SetValue(rightTcpPosYText, "Y : 0.000");
        SetValue(rightTcpPosZText, "Z : 0.000");

        SetValue(rightTcpRotXText, "RX : 0.0");
        SetValue(rightTcpRotYText, "RY : 0.0");
        SetValue(rightTcpRotZText, "RZ : 0.0");

        SetValue(rightRelativeXText, "DX : 0.000");
        SetValue(rightRelativeYText, "DY : 0.000");
        SetValue(rightRelativeZText, "DZ : 0.000");

        SetValue(rightFkXText, "FK X : 0.000");
        SetValue(rightFkYText, "FK Y : 0.000");
        SetValue(rightFkZText, "FK Z : 0.000");

        SetValue(rightPythonXText, $"PY X : {pythonPausedLabel}");
        SetValue(rightPythonYText, $"PY Y : {pythonPausedLabel}");
        SetValue(rightPythonZText, $"PY Z : {pythonPausedLabel}");

        SetValue(rightErrorFkXText, "EFX : 0.0000");
        SetValue(rightErrorFkYText, "EFY : 0.0000");
        SetValue(rightErrorFkZText, "EFZ : 0.0000");

        SetValue(rightErrorPythonXText, $"EPX : {emptyValueLabel}");
        SetValue(rightErrorPythonYText, $"EPY : {emptyValueLabel}");
        SetValue(rightErrorPythonZText, $"EPZ : {emptyValueLabel}");
    }

    private void ApplyBlockColors(string formattedMode, string robotStateLabel)
    {
        SetImageColor(
            modeBlockGraphic,
            formattedMode == "LIVE" ? liveModeBlockColor : simModeBlockColor
        );

        SetImageColor(sourceBlockGraphic, sourceBlockColor);
        SetImageColor(robotStateBlockGraphic, GetRobotStateColor(robotStateLabel));
        SetImageColor(connectionBlockGraphic, connectionBlockStaticColor);
        SetImageColor(validBlockGraphic, validBlockStaticColor);
    }

    private Color GetRobotStateColor(string robotStateLabel)
    {
        string normalized = string.IsNullOrWhiteSpace(robotStateLabel)
            ? string.Empty
            : robotStateLabel.ToUpperInvariant();

        if (normalized.Contains("READY"))
        {
            return robotReadyBlockColor;
        }

        if (normalized.Contains("DISCONNECTED"))
        {
            return robotDisconnectedBlockColor;
        }

        if (normalized.Contains("INVALID") || normalized.Contains("POLL"))
        {
            return robotInvalidBlockColor;
        }

        return robotNeutralBlockColor;
    }

    private string FormatSigned1(float value)
    {
        return value.ToString("+0.0;-0.0;0.0");
    }

    private string FormatSigned3(float value)
    {
        return value.ToString("+0.000;-0.000;0.000");
    }

    private string FormatSigned4(float value)
    {
        return value.ToString("+0.0000;-0.0000;0.0000");
    }

    private void SetLamp(Image image, bool state, Color onColor, Color offColor)
    {
        if (image == null)
        {
            return;
        }

        image.color = state ? onColor : offColor;
    }

    private void SetImageColor(Image image, Color color)
    {
        if (image == null)
        {
            return;
        }

        image.color = color;
    }

    private void SetLabel(TMP_Text labelText, string content)
    {
        if (labelText == null)
        {
            return;
        }

        labelText.text = content;
        labelText.color = labelColor;
    }

    private void SetValue(TMP_Text valueText, string content)
    {
        if (valueText == null)
        {
            return;
        }

        valueText.text = content;
        valueText.color = valueColor;
    }
}
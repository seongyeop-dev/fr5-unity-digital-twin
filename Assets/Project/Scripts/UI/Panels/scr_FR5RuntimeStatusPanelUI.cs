using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FR5 Runtime 상태 표시 UI를 관리하는 스크립트입니다.
///
/// 주요 역할:
/// 1. TopBar에 Runtime 모드 / Source / Robot 상태 / 연결 상태를 표시합니다.
/// 2. RightDock Summary에 MODE / SOURCE / ROBOT / VALID 상태를 표시합니다.
/// 3. RightDock TCP / DELTA / ROT 값을 표시합니다.
/// 4. RightDock Runtime 상태를 표시합니다.
/// 5. RightDock Compare 값을 표시합니다.
/// 6. RightDock Alarm 값을 표시합니다.
/// 7. COMMAND / QUEUE / MOTION 상태를 표시합니다.
///
/// 주의:
/// - RectTransform, Anchor, 위치, 크기 값은 이 스크립트에서 변경하지 않습니다.
/// - 이 스크립트는 Text, Image 값만 갱신합니다.
/// </summary>
public class scr_FR5RuntimeStatusPanelUI : MonoBehaviour
{
    [Header("참조 설정")]
    [SerializeField] private scr_FR5RuntimeSyncManager runtimeSyncManager;
    [SerializeField] private scr_FR5CSharpBridgeClient cSharpBridgeClient;
    [SerializeField] private scr_FR5CSharpSdkClient cSharpSdkClient;
    [SerializeField] private scr_FR5RobotManualController robotController;
    [SerializeField] private scr_FR5UnityReplayJointStateSource unityReplaySource;
    [SerializeField] private scr_FR5Ros2JointStateClient ros2JointStateClient;

    [Header("TopBar 값 텍스트")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text modeText;
    [SerializeField] private TMP_Text sourceText;
    [SerializeField] private TMP_Text robotStateText;
    [SerializeField] private TMP_Text lastPollText;
    [SerializeField] private TMP_Text lastMessageText;

    [Header("TopBar 라벨 텍스트")]
    [SerializeField] private TMP_Text modeLabelText;
    [SerializeField] private TMP_Text sourceLabelText;
    [SerializeField] private TMP_Text robotLabelText;
    [SerializeField] private TMP_Text lastPollLabelText;
    [SerializeField] private TMP_Text lastMessageLabelText;
    [SerializeField] private TMP_Text connectionLabelText;
    [SerializeField] private TMP_Text validLabelText;

    [Header("TopBar 상세 블록")]
    [SerializeField] private GameObject lastPollBlockRoot;
    [SerializeField] private GameObject lastMessageBlockRoot;

    [Header("TopBar 블록 이미지")]
    [SerializeField] private Image modeBlockGraphic;
    [SerializeField] private Image sourceBlockGraphic;
    [SerializeField] private Image robotStateBlockGraphic;
    [SerializeField] private Image connectionBlockGraphic;
    [SerializeField] private Image validBlockGraphic;

    [Header("연결 / 유효성 램프")]
    [SerializeField] private Image connectionLamp;
    [SerializeField] private Image sampleValidLamp;

    [Header("RightDock Summary 텍스트")]
    [SerializeField] private TMP_Text rightModeSummaryText;
    [SerializeField] private TMP_Text rightSourceSummaryText;
    [SerializeField] private TMP_Text rightRobotStateSummaryText;
    [SerializeField] private TMP_Text rightValidSummaryText;

    [Header("RightDock TCP / Delta 텍스트")]
    [SerializeField] private TMP_Text rightRelativeXText;
    [SerializeField] private TMP_Text rightRelativeYText;
    [SerializeField] private TMP_Text rightRelativeZText;

    [SerializeField] private TMP_Text rightTcpPosXText;
    [SerializeField] private TMP_Text rightTcpPosYText;
    [SerializeField] private TMP_Text rightTcpPosZText;

    [SerializeField] private TMP_Text rightTcpRotXText;
    [SerializeField] private TMP_Text rightTcpRotYText;
    [SerializeField] private TMP_Text rightTcpRotZText;

    [Header("RightDock Runtime 텍스트")]
    [SerializeField] private TMP_Text rightLastPollText;
    [SerializeField] private TMP_Text rightRuntimeModeText;
    [SerializeField] private TMP_Text rightRuntimeSpeedText;
    [SerializeField] private TMP_Text rightRuntimeStateText;

    [Header("RightDock Command Status 텍스트")]
    [SerializeField] private TMP_Text rightCommandStatusText;
    [SerializeField] private TMP_Text rightQueueStatusText;
    [SerializeField] private TMP_Text rightMotionStatusText;

    [Header("RightDock Compare - FK 위치")]
    [SerializeField] private TMP_Text rightFkXText;
    [SerializeField] private TMP_Text rightFkYText;
    [SerializeField] private TMP_Text rightFkZText;

    [Header("RightDock Compare - Python 위치")]
    [SerializeField] private TMP_Text rightPythonXText;
    [SerializeField] private TMP_Text rightPythonYText;
    [SerializeField] private TMP_Text rightPythonZText;

    [Header("RightDock Compare - Unity vs FK 오차")]
    [SerializeField] private TMP_Text rightErrorFkXText;
    [SerializeField] private TMP_Text rightErrorFkYText;
    [SerializeField] private TMP_Text rightErrorFkZText;

    [Header("RightDock Compare - Unity vs Python 오차")]
    [SerializeField] private TMP_Text rightErrorPythonXText;
    [SerializeField] private TMP_Text rightErrorPythonYText;
    [SerializeField] private TMP_Text rightErrorPythonZText;

    [Header("RightDock Alarm 텍스트")]
    [SerializeField] private TMP_Text rightAlarmText;

    [Header("표시 문자열")]
    [SerializeField] private string panelTitle = "FR5 Digital Twin Runtime";
    [SerializeField] private string defaultRobotModeLabel = "MANUAL";
    [SerializeField] private string defaultSpeedLabel = "100%";
    [SerializeField] private string noAlarmLabel = "NO ALARM";
    [SerializeField] private string pythonPausedLabel = "PAUSED";
    [SerializeField] private string emptyValueLabel = "---";

    [Header("Command Status Runtime")]
    [SerializeField] private string lastCommandStatus = "READY";
    [SerializeField] private string lastQueueStatus = "0";
    [SerializeField] private string lastMotionStatus = "READY";

    [Header("Command Status Auto Clear")]
    [SerializeField] private bool autoClearCommandStatus = true;
    [SerializeField] private float commandStatusAutoClearSeconds = 3.0f;

    private bool commandStatusAutoClearActive = false;
    private float commandStatusClearTime = -1f;

    [Header("표시 옵션")]
    [SerializeField] private bool showRuntimeMessageAsAlarm = false;
    [SerializeField] private bool showPythonPausedWhenMissing = true;

    [Header("상태 색상")]
    [SerializeField] private Color connectedColor = new Color(0.0f, 0.85f, 0.55f, 1.0f);
    [SerializeField] private Color disconnectedColor = new Color(0.75f, 0.2f, 0.2f, 1.0f);
    [SerializeField] private Color validColor = new Color(0.1f, 0.9f, 0.6f, 1.0f);
    [SerializeField] private Color invalidColor = new Color(0.95f, 0.55f, 0.15f, 1.0f);

    [Header("TopBar 블록 색상")]
    [SerializeField] private Color liveModeBlockColor = new Color(0.10f, 0.55f, 0.90f, 0.95f);
    [SerializeField] private Color simModeBlockColor = new Color(0.22f, 0.32f, 0.45f, 0.95f);
    [SerializeField] private Color sourceBlockColor = new Color(0.18f, 0.30f, 0.48f, 0.95f);
    [SerializeField] private Color robotReadyBlockColor = new Color(0.12f, 0.56f, 0.36f, 0.95f);
    [SerializeField] private Color robotDisconnectedBlockColor = new Color(0.56f, 0.18f, 0.18f, 0.95f);
    [SerializeField] private Color robotInvalidBlockColor = new Color(0.62f, 0.38f, 0.10f, 0.95f);
    [SerializeField] private Color robotNeutralBlockColor = new Color(0.22f, 0.30f, 0.40f, 0.95f);
    [SerializeField] private Color connectionBlockStaticColor = new Color(0.18f, 0.30f, 0.48f, 0.95f);
    [SerializeField] private Color validBlockStaticColor = new Color(0.18f, 0.30f, 0.48f, 0.95f);

    [Header("텍스트 색상")]
    [SerializeField] private Color labelColor = new Color(0.76f, 0.84f, 0.90f, 0.92f);
    [SerializeField] private Color valueColor = Color.white;

    [Header("자동 갱신 옵션")]
    [SerializeField] private bool autoRefreshInUpdate = true;
    [SerializeField] private float refreshIntervalSeconds = 0.2f;
    [SerializeField] private float uiRefreshInterval = 0.2f;
    [SerializeField] private float runtimeStatusRefreshInterval = 0.2f;
    [SerializeField] private float numericRefreshInterval = 0.2f;
    [SerializeField] private float messageRefreshInterval = 0.4f;
    [SerializeField] private float staleTimeoutSeconds = 0.8f;
    [SerializeField] private float disconnectTimeoutSeconds = 2.0f;
    [SerializeField] private bool suppressUnchangedText = true;
    [SerializeField] private bool keepLastValidValues = true;
    [SerializeField] private float tcpPositionDisplayDeadbandMeters = 0.0005f;
    [SerializeField] private float tcpRotationDisplayDeadbandDegrees = 0.05f;
    [SerializeField] private float deltaDisplayDeadbandMeters = 0.0005f;

    private float nextRefreshTime = 0f;
    private float nextMessageRefreshTime = 0f;
    private bool allowMessageRefreshThisPass = true;
    private float lastRos2ValidSampleTime = -1f;
    private string lastRos2StableStateLabel = "DISCONNECTED";
    private string lastRos2StableValidLabel = "DISCONNECTED";
    private bool hasDisplayedTcpPos = false;
    private bool hasDisplayedTcpRot = false;
    private bool hasDisplayedDelta = false;
    private bool hasDisplayedFkPos = false;
    private bool hasDisplayedErrorFk = false;
    private bool hasDisplayedPythonPos = false;
    private bool hasDisplayedErrorPython = false;
    private Vector3 displayedTcpPos = Vector3.zero;
    private Vector3 displayedTcpRot = Vector3.zero;
    private Vector3 displayedDelta = Vector3.zero;
    private Vector3 displayedFkPos = Vector3.zero;
    private Vector3 displayedErrorFk = Vector3.zero;
    private Vector3 displayedPythonPos = Vector3.zero;
    private Vector3 displayedErrorPython = Vector3.zero;

    private void Start()
    {
        RefreshUI();
    }

    private void Update()
    {
        CheckCommandStatusAutoClear();

        if (!autoRefreshInUpdate)
        {
            return;
        }

        if (Time.time < nextRefreshTime)
        {
            return;
        }

        float interval = Mathf.Max(0.05f, Mathf.Max(uiRefreshInterval > 0f ? uiRefreshInterval : refreshIntervalSeconds, Mathf.Min(Mathf.Max(0.05f, runtimeStatusRefreshInterval), Mathf.Max(0.05f, numericRefreshInterval))));
        nextRefreshTime = Time.time + interval;
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

    public void SetCommandMotionStatus(
        string commandStatus,
        string motionStatus,
        int queueCount = 0,
        bool autoClear = true)
    {
        lastCommandStatus = string.IsNullOrWhiteSpace(commandStatus)
            ? "READY"
            : commandStatus.ToUpperInvariant();

        lastMotionStatus = string.IsNullOrWhiteSpace(motionStatus)
            ? "READY"
            : motionStatus.ToUpperInvariant();

        lastQueueStatus = Mathf.Max(0, queueCount).ToString();

        bool shouldAutoClear =
            autoClearCommandStatus &&
            autoClear &&
            !string.Equals(lastCommandStatus, "READY", System.StringComparison.OrdinalIgnoreCase);

        commandStatusAutoClearActive = shouldAutoClear;
        commandStatusClearTime = shouldAutoClear
            ? Time.time + Mathf.Max(0.2f, commandStatusAutoClearSeconds)
            : -1f;

        ApplyCommandMotionStatusValues();
    }

    public void ResetCommandMotionStatus()
    {
        SetCommandMotionStatus("READY", "READY", 0);
    }

    private void CheckCommandStatusAutoClear()
    {
        if (!commandStatusAutoClearActive)
        {
            return;
        }

        if (Time.time < commandStatusClearTime)
        {
            return;
        }

        commandStatusAutoClearActive = false;
        commandStatusClearTime = -1f;

        ResetCommandMotionStatus();
    }

    private void ApplyCommandMotionStatusValues()
    {
        SetValue(rightCommandStatusText, $"COMMAND : {lastCommandStatus}");
        SetValue(rightQueueStatusText, $"QUEUE : {lastQueueStatus}");
        SetValue(rightMotionStatusText, $"MOTION : {lastMotionStatus}");
    }

    public void RefreshUI()
    {
        ApplyStaticLabels();

        allowMessageRefreshThisPass = Time.time >= nextMessageRefreshTime;
        if (allowMessageRefreshThisPass)
        {
            nextMessageRefreshTime = Time.time + Mathf.Clamp(messageRefreshInterval, 0.3f, 0.5f);
        }

        SetValue(titleText, panelTitle);

        if (runtimeSyncManager == null)
        {
            ApplyNoReferenceState();
            ApplyCommandMotionStatusValues();
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
        else if (runtimeSyncManager.SelectedRuntimeSource == scr_FR5RuntimeSyncManager.RuntimeSourceType.Ros2JointState)
        {
            scr_FR5Ros2JointStateClient ros2Client = ResolveRos2JointStateClient();

            formattedSource = "ROS2 Joint State";

            if (ros2Client != null)
            {
                UpdateRos2StableUiState(ros2Client);
                connected = !string.Equals(lastRos2StableValidLabel, "DISCONNECTED", System.StringComparison.OrdinalIgnoreCase);
                sampleValid = string.Equals(lastRos2StableValidLabel, "VALID", System.StringComparison.OrdinalIgnoreCase);
                formattedRobotState = lastRos2StableStateLabel;
                formattedLastPoll = GetRos2LastPollLabel(ros2Client, formattedLastPoll);
                formattedMessage = GetRos2MessageLabel(ros2Client, formattedMessage);
            }
            else
            {
                connected = false;
                sampleValid = false;
                lastRos2StableStateLabel = "ROS2 WAITING";
                lastRos2StableValidLabel = "DISCONNECTED";
                formattedRobotState = lastRos2StableStateLabel;
                formattedMessage = "Waiting for ros_tcp_endpoint and /fr5/joint_states.";
            }
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
            GetStableValidLabel(sampleValid)
        );

        ApplyTcpAndCompareValues();
        ApplyRightRuntimeValues(formattedLastPoll, sampleValid);
        ApplyRightAlarmValues(formattedMessage);

        SetLamp(connectionLamp, connected, connectedColor, disconnectedColor);
        SetLamp(sampleValidLamp, sampleValid, validColor, invalidColor);
        ApplyConnectionStatusLabels(connected, sampleValid);

        ApplyBlockColors(formattedMode, formattedRobotState);
        ApplyCommandMotionStatusValues();
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
        if (allowMessageRefreshThisPass)
        {
            SetValue(lastMessageText, formattedMessage);
        }
    }

    private void ApplyRightSummaryValues(
        string formattedMode,
        string formattedSource,
        string formattedRobotState,
        string validLabel)
    {
        SetValue(rightModeSummaryText, $"MODE : {SafeField(formattedMode)}");
        SetValue(rightSourceSummaryText, $"SOURCE : {SafeField(formattedSource)}");
        SetValue(rightRobotStateSummaryText, $"ROBOT : {SafeField(formattedRobotState)}");
        SetValue(rightValidSummaryText, $"VALID : {SafeField(validLabel)}");
    }

    private void ApplyTcpAndCompareValues()
    {
        if (robotController == null)
        {
            ApplyNoRobotControllerTcpState();
            return;
        }

        robotController.ForceRefreshStatus();

        Vector3 tcpPos = StabilizeDisplayedVector(robotController.GetCurrentUnityTCPLocalPosition(), ref displayedTcpPos, ref hasDisplayedTcpPos, tcpPositionDisplayDeadbandMeters);
        Vector3 tcpRot = StabilizeDisplayedVector(robotController.GetCurrentUnityTCPLocalRotation(), ref displayedTcpRot, ref hasDisplayedTcpRot, tcpRotationDisplayDeadbandDegrees);
        Vector3 delta = StabilizeDisplayedVector(robotController.GetCurrentRelativeDeltaLocal(), ref displayedDelta, ref hasDisplayedDelta, deltaDisplayDeadbandMeters);

        Vector3 fkPos = StabilizeDisplayedVector(robotController.GetCurrentFKTCPLocalPosition(), ref displayedFkPos, ref hasDisplayedFkPos, tcpPositionDisplayDeadbandMeters);
        Vector3 pyPos = StabilizeDisplayedVector(robotController.GetCurrentPythonTCPLocalPosition(), ref displayedPythonPos, ref hasDisplayedPythonPos, tcpPositionDisplayDeadbandMeters);

        Vector3 errorFk = StabilizeDisplayedVector(robotController.GetCurrentTCPErrorUnityVsFKLocal(), ref displayedErrorFk, ref hasDisplayedErrorFk, deltaDisplayDeadbandMeters);
        Vector3 errorPy = StabilizeDisplayedVector(robotController.GetCurrentTCPErrorUnityVsPythonLocal(), ref displayedErrorPython, ref hasDisplayedErrorPython, deltaDisplayDeadbandMeters);

        bool pythonLoaded = robotController.IsPythonTCPLoaded();

        SetValue(rightTcpPosXText, $"X : {FormatPosition(tcpPos.x)}");
        SetValue(rightTcpPosYText, $"Y : {FormatPosition(tcpPos.y)}");
        SetValue(rightTcpPosZText, $"Z : {FormatPosition(tcpPos.z)}");

        SetValue(rightTcpRotXText, $"RX : {FormatRotation(tcpRot.x)}");
        SetValue(rightTcpRotYText, $"RY : {FormatRotation(tcpRot.y)}");
        SetValue(rightTcpRotZText, $"RZ : {FormatRotation(tcpRot.z)}");

        SetValue(rightRelativeXText, $"DX : {FormatDelta(delta.x)}");
        SetValue(rightRelativeYText, $"DY : {FormatDelta(delta.y)}");
        SetValue(rightRelativeZText, $"DZ : {FormatDelta(delta.z)}");

        SetValue(rightFkXText, $"FK X : {FormatPosition(fkPos.x)}");
        SetValue(rightFkYText, $"FK Y : {FormatPosition(fkPos.y)}");
        SetValue(rightFkZText, $"FK Z : {FormatPosition(fkPos.z)}");

        if (pythonLoaded)
        {
            SetValue(rightPythonXText, $"PY X : {FormatPosition(pyPos.x)}");
            SetValue(rightPythonYText, $"PY Y : {FormatPosition(pyPos.y)}");
            SetValue(rightPythonZText, $"PY Z : {FormatPosition(pyPos.z)}");

            SetValue(rightErrorPythonXText, $"EPX : {FormatDelta(errorPy.x)}");
            SetValue(rightErrorPythonYText, $"EPY : {FormatDelta(errorPy.y)}");
            SetValue(rightErrorPythonZText, $"EPZ : {FormatDelta(errorPy.z)}");
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

        SetValue(rightErrorFkXText, $"EFX : {FormatDelta(errorFk.x)}");
        SetValue(rightErrorFkYText, $"EFY : {FormatDelta(errorFk.y)}");
        SetValue(rightErrorFkZText, $"EFZ : {FormatDelta(errorFk.z)}");
    }

    private void ApplyRightRuntimeValues(string formattedLastPoll, bool sampleValid)
    {
        if (TryApplyUnityReplayRuntimeValues())
        {
            return;
        }

        if (TryApplyRos2RuntimeValues(formattedLastPoll))
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
            SetValue(rightRuntimeSpeedText, $"SPEED : {defaultSpeedLabel}");
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

    private bool TryApplyRos2RuntimeValues(string formattedLastPoll)
    {
        if (runtimeSyncManager == null ||
            runtimeSyncManager.SelectedRuntimeSource != scr_FR5RuntimeSyncManager.RuntimeSourceType.Ros2JointState)
        {
            return false;
        }

        SetValue(rightRuntimeModeText, "MODE : ROS2 JOINT STATE");

        scr_FR5Ros2JointStateClient ros2Client = ResolveRos2JointStateClient();

        if (ros2Client == null)
        {
            SetValue(rightLastPollText, "LAST : WAITING");
            SetValue(rightRuntimeSpeedText, "TOPIC : SOURCE MISSING");
            SetValue(rightRuntimeStateText, "STATE : ROS2 WAITING");
            return true;
        }

        string topicLabel = string.IsNullOrWhiteSpace(ros2Client.TopicName)
            ? "-"
            : ros2Client.TopicName;

        string subscribedLabel = ros2Client.HasSubscribed ? "YES" : "NO";
        string lastPollLabel = GetRos2LastPollLabel(ros2Client, formattedLastPoll);
        string stateLabel = GetRos2RuntimeStateLabel(ros2Client);
        string jointSummary = GetRos2JointSummaryLabel(ros2Client);

        SetValue(rightLastPollText, $"LAST : {lastPollLabel}");
        SetValue(rightRuntimeSpeedText, $"TOPIC : {topicLabel} | SUB : {subscribedLabel}");

        if (!string.IsNullOrWhiteSpace(jointSummary))
        {
            SetValue(rightRuntimeStateText, $"STATE : {stateLabel} | J {jointSummary}");
        }
        else
        {
            SetValue(rightRuntimeStateText, $"STATE : {stateLabel}");
        }

        return true;
    }

    private void ApplyRightAlarmValues(string formattedMessage)
    {
        if (rightAlarmText == null || !allowMessageRefreshThisPass)
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

    private scr_FR5Ros2JointStateClient ResolveRos2JointStateClient()
    {
        if (ros2JointStateClient != null)
        {
            return ros2JointStateClient;
        }

        ros2JointStateClient = FindSceneObject<scr_FR5Ros2JointStateClient>();
        return ros2JointStateClient;
    }

    private T FindSceneObject<T>() where T : UnityEngine.Object
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<T>();
#else
        return FindObjectOfType<T>();
#endif
    }

    private string GetRos2RuntimeStateLabel(scr_FR5Ros2JointStateClient ros2Client)
    {
        if (ros2Client == null)
        {
            return "ROS2 WAITING";
        }

        string robotState = string.IsNullOrWhiteSpace(ros2Client.LastRobotState)
            ? string.Empty
            : ros2Client.LastRobotState.ToUpperInvariant();

        string errorMessage = string.IsNullOrWhiteSpace(ros2Client.LastErrorMessage)
            ? string.Empty
            : ros2Client.LastErrorMessage.ToUpperInvariant();

        if (robotState.Contains("TIMEOUT") || errorMessage.Contains("TIMEOUT"))
        {
            return "ROS2 TIMEOUT";
        }

        if (!ros2Client.IsConnected || !ros2Client.LatestMessageReceived)
        {
            return "ROS2 WAITING";
        }

        if (ros2Client.LastPollSucceeded && ros2Client.LastSampleValid)
        {
            return "ROS2 LIVE";
        }

        return "ROS2 WAITING";
    }

    private string GetRos2LastPollLabel(scr_FR5Ros2JointStateClient ros2Client, string fallbackLabel)
    {
        if (ros2Client == null)
        {
            return string.IsNullOrWhiteSpace(fallbackLabel) ? "WAITING" : fallbackLabel;
        }

        if (!string.IsNullOrWhiteSpace(ros2Client.LastPollTime) && ros2Client.LastPollTime != "-")
        {
            return ros2Client.LastPollTime;
        }

        return string.IsNullOrWhiteSpace(fallbackLabel) ? "WAITING" : fallbackLabel;
    }

    private string GetRos2MessageLabel(scr_FR5Ros2JointStateClient ros2Client, string fallbackMessage)
    {
        if (ros2Client == null || !ros2Client.IsConnected || !ros2Client.LatestMessageReceived)
        {
            return "Waiting for ros_tcp_endpoint and /fr5/joint_states.";
        }

        string message = ros2Client.GetDisplayMessageLabel(fallbackMessage);
        string topicLabel = string.IsNullOrWhiteSpace(ros2Client.TopicName)
            ? "-"
            : ros2Client.TopicName;
        string jointSummary = GetRos2JointSummaryLabel(ros2Client);

        if (!string.IsNullOrWhiteSpace(jointSummary))
        {
            return $"{message} | Topic: {topicLabel} | Joints: {jointSummary}";
        }

        return $"{message} | Topic: {topicLabel}";
    }

    private string GetRos2JointSummaryLabel(scr_FR5Ros2JointStateClient ros2Client)
    {
        if (ros2Client != null &&
            !string.IsNullOrWhiteSpace(ros2Client.LastJointSummary) &&
            ros2Client.LastJointSummary != "-")
        {
            return ros2Client.LastJointSummary;
        }

        if (runtimeSyncManager != null &&
            !string.IsNullOrWhiteSpace(runtimeSyncManager.LastAppliedJointSummary) &&
            runtimeSyncManager.LastAppliedJointSummary != "-")
        {
            return runtimeSyncManager.LastAppliedJointSummary;
        }

        return string.Empty;
    }

    private void UpdateRos2StableUiState(scr_FR5Ros2JointStateClient ros2Client)
    {
        if (ros2Client == null)
        {
            lastRos2StableStateLabel = "ROS2 WAITING";
            lastRos2StableValidLabel = "DISCONNECTED";
            return;
        }

        if (ros2Client.LastPollSucceeded && ros2Client.LastSampleValid && ros2Client.LatestMessageReceived)
        {
            lastRos2ValidSampleTime = Time.time;
        }

        if (lastRos2ValidSampleTime < 0f)
        {
            lastRos2StableStateLabel = "ROS2 WAITING";
            lastRos2StableValidLabel = "DISCONNECTED";
            return;
        }

        float age = Time.time - lastRos2ValidSampleTime;

        if (age < Mathf.Max(0.1f, staleTimeoutSeconds))
        {
            lastRos2StableStateLabel = "ROS2 LIVE";
            lastRos2StableValidLabel = "VALID";
            return;
        }

        if (age < Mathf.Max(staleTimeoutSeconds + 0.1f, disconnectTimeoutSeconds))
        {
            lastRos2StableStateLabel = "ROS2 STALE";
            lastRos2StableValidLabel = "STALE";
            return;
        }

        lastRos2StableStateLabel = "ROS2 WAITING";
        lastRos2StableValidLabel = "DISCONNECTED";
    }

    private string GetStableValidLabel(bool sampleValid)
    {
        if (runtimeSyncManager != null &&
            runtimeSyncManager.SelectedRuntimeSource == scr_FR5RuntimeSyncManager.RuntimeSourceType.Ros2JointState)
        {
            return string.IsNullOrWhiteSpace(lastRos2StableValidLabel)
                ? "DISCONNECTED"
                : lastRos2StableValidLabel;
        }

        return sampleValid ? "TRUE" : "FALSE";
    }

    private string SafeField(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? emptyValueLabel : value;
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

    private void ApplyConnectionStatusLabels(bool connected, bool sampleValid)
    {
        string validLabel = GetStableValidLabel(sampleValid);
        SetLabel(connectionLabelText, connected ? "LINK CONNECTED" : "LINK DISCONNECTED");
        SetLabel(validLabelText, $"VALID {validLabel}");
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
        SetValue(rightTcpPosXText, "X : 0.000000");
        SetValue(rightTcpPosYText, "Y : 0.000000");
        SetValue(rightTcpPosZText, "Z : 0.000000");

        SetValue(rightTcpRotXText, "RX : 0.000000");
        SetValue(rightTcpRotYText, "RY : 0.000000");
        SetValue(rightTcpRotZText, "RZ : 0.000000");

        SetValue(rightRelativeXText, "DX : 0.000000");
        SetValue(rightRelativeYText, "DY : 0.000000");
        SetValue(rightRelativeZText, "DZ : 0.000000");

        SetValue(rightFkXText, "FK X : 0.000000");
        SetValue(rightFkYText, "FK Y : 0.000000");
        SetValue(rightFkZText, "FK Z : 0.000000");

        SetValue(rightPythonXText, $"PY X : {pythonPausedLabel}");
        SetValue(rightPythonYText, $"PY Y : {pythonPausedLabel}");
        SetValue(rightPythonZText, $"PY Z : {pythonPausedLabel}");

        SetValue(rightErrorFkXText, "EFX : 0.000000");
        SetValue(rightErrorFkYText, "EFY : 0.000000");
        SetValue(rightErrorFkZText, "EFZ : 0.000000");

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

    private Vector3 StabilizeDisplayedVector(Vector3 current, ref Vector3 displayed, ref bool hasDisplayed, float deadband)
    {
        if (!hasDisplayed)
        {
            displayed = current;
            hasDisplayed = true;
            return displayed;
        }

        float threshold = Mathf.Max(0f, deadband);
        if (Mathf.Abs(current.x - displayed.x) >= threshold ||
            Mathf.Abs(current.y - displayed.y) >= threshold ||
            Mathf.Abs(current.z - displayed.z) >= threshold)
        {
            displayed = current;
        }

        return displayed;
    }

    private string FormatPosition(float value)
    {
        return value.ToString("+0.000;-0.000;0.000", System.Globalization.CultureInfo.InvariantCulture);
    }

    private string FormatRotation(float value)
    {
        return value.ToString("+0.00;-0.00;0.00", System.Globalization.CultureInfo.InvariantCulture);
    }

    private string FormatDelta(float value)
    {
        return value.ToString("+0.0000;-0.0000;0.0000", System.Globalization.CultureInfo.InvariantCulture);
    }

    private string FormatSigned1(float value)
    {
        return value.ToString("+0.000;-0.000;0.000", System.Globalization.CultureInfo.InvariantCulture);
    }

    private string FormatSigned3(float value)
    {
        return value.ToString("+0.000;-0.000;0.000", System.Globalization.CultureInfo.InvariantCulture);
    }

    private string FormatSigned4(float value)
    {
        return value.ToString("+0.000;-0.000;0.000", System.Globalization.CultureInfo.InvariantCulture);
    }

    private void SetLamp(Image image, bool state, Color onColor, Color offColor)
    {
        if (image == null)
        {
            return;
        }

        SetImageColor(image, state ? onColor : offColor);
    }

    private void SetImageColor(Image image, Color color)
    {
        if (image == null)
        {
            return;
        }

        if (image.color == color)
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

        string safeContent = string.IsNullOrWhiteSpace(content) ? emptyValueLabel : content;

        if (suppressUnchangedText && labelText.text == safeContent)
        {
            if (labelText.color != labelColor)
            {
                labelText.color = labelColor;
            }

            return;
        }

        labelText.text = safeContent;

        if (labelText.color != labelColor)
        {
            labelText.color = labelColor;
        }
    }

    private void SetValue(TMP_Text valueText, string content)
    {
        if (valueText == null)
        {
            return;
        }

        string safeContent = string.IsNullOrWhiteSpace(content) ? emptyValueLabel : content;

        if (keepLastValidValues && IsEmptyRuntimeValue(safeContent) && !string.IsNullOrWhiteSpace(valueText.text))
        {
            return;
        }

        if (suppressUnchangedText && valueText.text == safeContent)
        {
            if (valueText.color != valueColor)
            {
                valueText.color = valueColor;
            }

            return;
        }

        valueText.text = safeContent;

        if (valueText.color != valueColor)
        {
            valueText.color = valueColor;
        }
    }

    private bool IsEmptyRuntimeValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        string trimmed = value.Trim();
        return trimmed == "-" || trimmed == "---" || trimmed.EndsWith(" : -") || trimmed.EndsWith(" : ---");
    }
}
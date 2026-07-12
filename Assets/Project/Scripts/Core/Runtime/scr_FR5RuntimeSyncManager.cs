using System;
using UnityEngine;

public class scr_FR5RuntimeSyncManager : MonoBehaviour
{
    public enum RuntimeInputMode
    {
        SimManual,
        LiveRuntimeSource
    }

    public enum RuntimeSourceType
    {
        MockSdk,
        CSharpBridge,
        PythonBridge,
        CSharpSdk,
        UnityReplay,
        Ros2JointState
    }

    [Header("Mode")]
    [SerializeField] private RuntimeInputMode inputMode = RuntimeInputMode.SimManual;
    [SerializeField] private RuntimeSourceType selectedRuntimeSource = RuntimeSourceType.CSharpBridge;
    [SerializeField] private bool autoSyncInUpdate = true;
    [SerializeField] private float liveSyncIntervalSeconds = 0.05f;

    [Header("References")]
    [SerializeField] private scr_VirtualJointController virtualRobotController;
    [SerializeField] private scr_FR5ValidationManager validationManager;

    [Header("Runtime Sources")]
    [SerializeField] private scr_FR5SdkClient mockSdkClient;
    [SerializeField] private scr_FR5CSharpBridgeClient cSharpBridgeClient;
    [SerializeField] private scr_FR5PythonBridgeClient pythonBridgeClient;
    [SerializeField] private scr_FR5CSharpSdkClient cSharpSdkClient;
    [SerializeField] private scr_FR5UnityReplayJointStateSource unityReplaySource;
    [SerializeField] private scr_FR5Ros2JointStateClient ros2JointStateClient;

    [Header("Options")]
    [SerializeField] private bool autoConnectSelectedSource = true;
    [SerializeField] private bool disconnectInactiveSourcesOnSwitch = true;
    [SerializeField] private bool applyJointAnglesFromRuntimeSource = true;
    [SerializeField] private bool runPythonValidationAfterLiveSync = false;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;
    [SerializeField] private bool logEveryAppliedSample = false;

    [Header("Runtime Status")]
    [SerializeField] private string activeSourceName = "None";
    [SerializeField] private bool activeSourceResolved = false;
    [SerializeField] private bool activeSourceConnected = false;
    [SerializeField] private bool lastSyncSucceeded = false;
    [SerializeField] private bool lastSampleValid = false;
    [SerializeField] private string lastSampleSource = "None";
    [SerializeField] private string lastSyncMessage = "Idle";
    [SerializeField] private string lastSyncTime = "-";
    [SerializeField] private string lastAppliedJointSummary = "-";

    private IFR5RuntimePoseSource runtimePoseSource;
    private float nextLiveSyncTime = 0f;

    private string previousStatusKey = string.Empty;
    private string previousAppliedJointSummary = string.Empty;

    public RuntimeInputMode InputMode => inputMode;
    public RuntimeSourceType SelectedRuntimeSource => selectedRuntimeSource;

    public string ActiveSourceName => activeSourceName;
    public bool ActiveSourceResolved => activeSourceResolved;
    public bool ActiveSourceConnected => activeSourceConnected;
    public bool LastSyncSucceeded => lastSyncSucceeded;
    public bool LastSampleValid => lastSampleValid;
    public string LastSampleSource => lastSampleSource;
    public string LastSyncMessage => lastSyncMessage;
    public string LastSyncTime => lastSyncTime;
    public string LastAppliedJointSummary => lastAppliedJointSummary;
    public bool RuntimeApplyEnabled => applyJointAnglesFromRuntimeSource;

    public void SetRuntimeApplyEnabled(bool enabled)
    {
        if (applyJointAnglesFromRuntimeSource == enabled)
        {
            return;
        }

        applyJointAnglesFromRuntimeSource = enabled;

        if (verboseLog)
        {
            Debug.Log($"[FR5RuntimeSyncManager] Runtime apply enabled changed: {applyJointAnglesFromRuntimeSource}");
        }
    }

    private void Awake()
    {
        ResolveCurrentSource();
    }

    private void Start()
    {
        TryAutoConnectSelectedSource();
    }

    private void Update()
    {
        if (!autoSyncInUpdate)
        {
            return;
        }

        if (inputMode != RuntimeInputMode.LiveRuntimeSource)
        {
            return;
        }

        if (Time.time < nextLiveSyncTime)
        {
            return;
        }

        nextLiveSyncTime = Time.time + liveSyncIntervalSeconds;
        SyncLiveOnce();
    }

    [ContextMenu("Set SIM Mode")]
    public void SetSimModeFromInspector()
    {
        SetInputMode(RuntimeInputMode.SimManual);
    }

    [ContextMenu("Set LIVE Mode")]
    public void SetLiveModeFromInspector()
    {
        SetInputMode(RuntimeInputMode.LiveRuntimeSource);
    }

    [ContextMenu("Use Mock Source")]
    public void UseMockSourceFromInspector()
    {
        SetRuntimeSource(RuntimeSourceType.MockSdk);
    }

    [ContextMenu("Use CSharpBridge Source")]
    public void UseCSharpBridgeSourceFromInspector()
    {
        SetRuntimeSource(RuntimeSourceType.CSharpBridge);
    }

    [ContextMenu("Use PythonBridge Source")]
    public void UsePythonBridgeSourceFromInspector()
    {
        SetRuntimeSource(RuntimeSourceType.PythonBridge);
    }

    [ContextMenu("Use CSharpSdk Source")]
    public void UseCSharpSdkSourceFromInspector()
    {
        SetRuntimeSource(RuntimeSourceType.CSharpSdk);
    }

    [ContextMenu("Use Unity Replay Source")]
    public void UseUnityReplaySourceFromInspector()
    {
        SetRuntimeSource(RuntimeSourceType.UnityReplay);
    }

    [ContextMenu("Use ROS2 JointState Source")]
    public void UseRos2JointStateSourceFromInspector()
    {
        SetRuntimeSource(RuntimeSourceType.Ros2JointState);
    }

    [ContextMenu("Sync LIVE Once")]
    public void SyncLiveOnceFromInspector()
    {
        SyncLiveOnce();
    }

    [ContextMenu("Connect Selected Source")]
    public void ConnectSelectedSourceFromInspector()
    {
        ResolveCurrentSource();

        if (runtimePoseSource == null)
        {
            Debug.LogWarning("[FR5RuntimeSyncManager] No runtime pose source to connect.");
            return;
        }

        bool connected = runtimePoseSource.Connect();
        activeSourceConnected = runtimePoseSource.IsConnected;

        if (verboseLog)
        {
            Debug.Log($"[FR5RuntimeSyncManager] Connect selected source result: {connected}");
        }
    }

    public void SetInputMode(RuntimeInputMode mode)
    {
        inputMode = mode;

        SetRuntimeStatus(
            false,
            false,
            lastSampleSource,
            $"Mode changed: {GetFormattedModeLabel()}",
            lastAppliedJointSummary
        );

        LogStatusIfChanged();

        if (verboseLog)
        {
            Debug.Log($"[FR5RuntimeSyncManager] Mode changed: {inputMode}");
        }
    }

    public void SetRuntimeSource(RuntimeSourceType sourceType)
    {
        selectedRuntimeSource = sourceType;
        ResolveCurrentSource();

        if (disconnectInactiveSourcesOnSwitch)
        {
            DisconnectInactiveSources();
        }

        TryAutoConnectSelectedSource();

        SetRuntimeStatus(
            false,
            false,
            activeSourceName,
            $"Runtime source changed: {GetFormattedSourceLabel()}",
            "-"
        );

        LogStatusIfChanged();

        if (verboseLog)
        {
            Debug.Log($"[FR5RuntimeSyncManager] Runtime source changed: {selectedRuntimeSource}");
        }
    }

    public bool SyncLiveOnce()
    {
        if (inputMode != RuntimeInputMode.LiveRuntimeSource)
        {
            SetRuntimeStatus(false, false, activeSourceName, "Sync skipped. Current mode is not LIVE.", "-");
            LogStatusIfChanged();
            return false;
        }

        if (!ValidateReferences())
        {
            SetRuntimeStatus(false, false, activeSourceName, "Reference validation failed.", "-");
            LogStatusIfChanged();
            return false;
        }

        if (!activeSourceConnected && autoConnectSelectedSource)
        {
            TryAutoConnectSelectedSource();
        }

        if (runtimePoseSource == null || !runtimePoseSource.IsConnected)
        {
            activeSourceConnected = false;
            SetRuntimeStatus(false, false, activeSourceName, "Selected runtime source is not connected.", "-");
            LogStatusIfChanged();
            return false;
        }

        bool polled = runtimePoseSource.PollLatestState();
        if (!polled)
        {
            SetRuntimeStatus(false, false, activeSourceName, "Runtime pose source poll failed.", "-");
            LogStatusIfChanged();
            return false;
        }

        if (!runtimePoseSource.TryGetLatestSample(out FR5SdkPoseSample sample))
        {
            SetRuntimeStatus(false, false, activeSourceName, "Failed to get latest sample.", "-");
            LogStatusIfChanged();
            return false;
        }

        if (sample == null)
        {
            SetRuntimeStatus(false, false, activeSourceName, "Latest sample is null.", "-");
            LogStatusIfChanged();
            return false;
        }

        if (!sample.isValid)
        {
            SetRuntimeStatus(
                false,
                false,
                sample.source,
                $"Latest sample is invalid. RobotState={sample.robotState}",
                FormatJointSummary(sample.jointDegrees)
            );

            LogStatusIfChanged();
            return false;
        }

        ApplySample(sample);

        SetRuntimeStatus(
            true,
            true,
            sample.source,
            "LIVE sample applied successfully.",
            FormatJointSummary(sample.jointDegrees)
        );

        LogStatusIfChanged();
        return true;
    }

    /// <summary>
    /// UI    Ͽ  ª         
    /// </summary>
    public string GetFormattedModeLabel()
    {
        return inputMode == RuntimeInputMode.SimManual ? "SIM" : "LIVE";
    }

    /// <summary>
    /// UI    Ͽ       Source   
    /// </summary>
    public string GetFormattedSourceLabel()
    {
        switch (selectedRuntimeSource)
        {
            case RuntimeSourceType.MockSdk:
                return "MOCK SDK";
            case RuntimeSourceType.CSharpBridge:
                return "C SHARP BRIDGE";
            case RuntimeSourceType.PythonBridge:
                return "PYTHON BRIDGE";
            case RuntimeSourceType.CSharpSdk:
                return "C SHARP SDK";
            case RuntimeSourceType.UnityReplay:
                return "UNITY REPLAY";
            case RuntimeSourceType.Ros2JointState:
                return "ROS2 JOINT STATE";
            default:
                return "UNKNOWN";
        }
    }

    /// <summary>
    /// TopBar / Runtime summary                κ         
    /// </summary>
    public string GetSelectedSourceRobotStateLabel()
    {
        if (selectedRuntimeSource == RuntimeSourceType.CSharpBridge && cSharpBridgeClient != null)
        {
            return cSharpBridgeClient.GetDisplayRobotStateLabel();
        }

        if (selectedRuntimeSource == RuntimeSourceType.UnityReplay && unityReplaySource != null)
        {
            return unityReplaySource.GetDisplayRobotStateLabel();
        }

        if (selectedRuntimeSource == RuntimeSourceType.Ros2JointState && ros2JointStateClient != null)
        {
            return ros2JointStateClient.GetDisplayRobotStateLabel();
        }

        if (!activeSourceResolved)
        {
            return "Source Missing";
        }

        if (!activeSourceConnected)
        {
            return "Disconnected";
        }

        if (!lastSyncSucceeded && inputMode == RuntimeInputMode.LiveRuntimeSource)
        {
            return "Poll Failed";
        }

        if (!lastSampleValid && inputMode == RuntimeInputMode.LiveRuntimeSource)
        {
            return "Sample Invalid";
        }

        return inputMode == RuntimeInputMode.SimManual ? "Sim Manual" : "Ready";
    }

    /// <summary>
    /// BottomBar / Status block         ý           
    /// </summary>
    public string GetEffectiveSystemStateLabel(string simFallback)
    {
        if (inputMode == RuntimeInputMode.SimManual)
        {
            return string.IsNullOrWhiteSpace(simFallback) ? "READY" : simFallback.ToUpperInvariant();
        }

        if (!activeSourceResolved)
        {
            return "SOURCE MISSING";
        }

        if (!activeSourceConnected)
        {
            return selectedRuntimeSource == RuntimeSourceType.Ros2JointState
                ? "ROS2 DISCONNECTED"
                : "SOURCE DISCONNECTED";
        }

        if (!lastSyncSucceeded)
        {
            if (selectedRuntimeSource == RuntimeSourceType.Ros2JointState && ros2JointStateClient != null)
            {
                return ros2JointStateClient.GetDisplayRobotStateLabel().ToUpperInvariant();
            }

            return "POLL FAILED";
        }

        if (!lastSampleValid)
        {
            return "SAMPLE INVALID";
        }

        return "LIVE READY";
    }

    public string GetFormattedValidLabel()
    {
        return lastSampleValid ? "TRUE" : "FALSE";
    }

    public string GetEffectiveLastPollLabel()
    {
        if (selectedRuntimeSource == RuntimeSourceType.CSharpBridge && cSharpBridgeClient != null)
        {
            return string.IsNullOrWhiteSpace(cSharpBridgeClient.LastPollTime)
                ? lastSyncTime
                : cSharpBridgeClient.LastPollTime;
        }

        if (selectedRuntimeSource == RuntimeSourceType.UnityReplay && unityReplaySource != null)
        {
            return string.IsNullOrWhiteSpace(unityReplaySource.LastPollTime)
                ? lastSyncTime
                : unityReplaySource.LastPollTime;
        }

        if (selectedRuntimeSource == RuntimeSourceType.Ros2JointState && ros2JointStateClient != null)
        {
            return string.IsNullOrWhiteSpace(ros2JointStateClient.LastPollTime)
                ? lastSyncTime
                : ros2JointStateClient.LastPollTime;
        }

        return string.IsNullOrWhiteSpace(lastSyncTime) ? "-" : lastSyncTime;
    }

    public string GetEffectiveMessageLabel()
    {
        if (selectedRuntimeSource == RuntimeSourceType.CSharpBridge && cSharpBridgeClient != null)
        {
            return cSharpBridgeClient.GetDisplayMessageLabel(lastSyncMessage);
        }

        if (selectedRuntimeSource == RuntimeSourceType.UnityReplay && unityReplaySource != null)
        {
            return unityReplaySource.GetDisplayMessageLabel(lastSyncMessage);
        }

        if (selectedRuntimeSource == RuntimeSourceType.Ros2JointState && ros2JointStateClient != null)
        {
            return ros2JointStateClient.GetDisplayMessageLabel(lastSyncMessage);
        }

        return string.IsNullOrWhiteSpace(lastSyncMessage) ? "None" : lastSyncMessage;
    }

    private void ApplySample(FR5SdkPoseSample sample)
    {
        if (sample == null || !sample.isValid)
        {
            return;
        }

        if (applyJointAnglesFromRuntimeSource)
        {
            float[] joints = sample.jointDegrees;

            if (joints == null || joints.Length != 6)
            {
                Debug.LogError("[FR5RuntimeSyncManager] Joint array is invalid.");
                return;
            }

            if (virtualRobotController == null)
            {
                Debug.LogError("[FR5RuntimeSyncManager] Virtual Robot Controller is not assigned.");
                return;
            }

            //         ӿ  6                                Pose     
            for (int i = 0; i < 6; i++)
            {
                virtualRobotController.SetJointAngleByIndex(i, joints[i], false);
            }

            virtualRobotController.ApplyPose();
        }

        //       ܰ迡     Python            ̻  
        if (runPythonValidationAfterLiveSync && validationManager != null)
        {
            if (verboseLog)
            {
                Debug.LogWarning("[FR5RuntimeSyncManager] Python validation is reserved for later phase.");
            }
        }

        if (verboseLog && logEveryAppliedSample)
        {
            Debug.Log(
                "[FR5RuntimeSyncManager] LIVE sample applied | " +
                $"Source={sample.source} | " +
                $"Joints={FormatJointSummary(sample.jointDegrees)}"
            );
        }
    }

    private bool ValidateReferences()
    {
        if (virtualRobotController == null)
        {
            Debug.LogError("[FR5RuntimeSyncManager] Virtual Robot Controller is not assigned.");
            return false;
        }

        ResolveCurrentSource();

        if (runtimePoseSource == null)
        {
            Debug.LogError("[FR5RuntimeSyncManager] Failed to resolve runtime pose source.");
            return false;
        }

        activeSourceConnected = runtimePoseSource.IsConnected;
        return true;
    }

    private void ResolveCurrentSource()
    {
        runtimePoseSource = null;
        activeSourceResolved = false;

        switch (selectedRuntimeSource)
        {
            case RuntimeSourceType.MockSdk:
                runtimePoseSource = mockSdkClient;
                activeSourceName = mockSdkClient != null ? mockSdkClient.name : "MockSdk (Missing)";
                break;

            case RuntimeSourceType.CSharpBridge:
                runtimePoseSource = cSharpBridgeClient;
                activeSourceName = cSharpBridgeClient != null ? cSharpBridgeClient.name : "CSharpBridge (Missing)";
                break;

            case RuntimeSourceType.PythonBridge:
                runtimePoseSource = pythonBridgeClient;
                activeSourceName = pythonBridgeClient != null ? pythonBridgeClient.name : "PythonBridge (Missing)";
                break;

            case RuntimeSourceType.CSharpSdk:
                runtimePoseSource = cSharpSdkClient;
                activeSourceName = cSharpSdkClient != null ? cSharpSdkClient.name : "CSharpSdk (Missing)";
                break;

            case RuntimeSourceType.UnityReplay:
                runtimePoseSource = unityReplaySource;
                activeSourceName = unityReplaySource != null ? unityReplaySource.name : "UnityReplay (Missing)";
                break;

            case RuntimeSourceType.Ros2JointState:
                runtimePoseSource = ros2JointStateClient;
                activeSourceName = ros2JointStateClient != null ? ros2JointStateClient.name : "Ros2JointState (Missing)";
                break;
        }

        activeSourceResolved = runtimePoseSource != null;
        activeSourceConnected = runtimePoseSource != null && runtimePoseSource.IsConnected;
    }

    private void DisconnectInactiveSources()
    {
        DisconnectSourceIfInactive(mockSdkClient);
        DisconnectSourceIfInactive(cSharpBridgeClient);
        DisconnectSourceIfInactive(pythonBridgeClient);
        DisconnectSourceIfInactive(cSharpSdkClient);
        DisconnectSourceIfInactive(unityReplaySource);
        DisconnectSourceIfInactive(ros2JointStateClient);
    }

    private void DisconnectSourceIfInactive(IFR5RuntimePoseSource source)
    {
        if (source == null)
        {
            return;
        }

        if (ReferenceEquals(source, runtimePoseSource))
        {
            return;
        }

        if (source.IsConnected)
        {
            source.Disconnect();
        }
    }

    private void TryAutoConnectSelectedSource()
    {
        ResolveCurrentSource();

        if (!autoConnectSelectedSource)
        {
            return;
        }

        if (runtimePoseSource == null)
        {
            return;
        }

        if (!runtimePoseSource.IsConnected)
        {
            runtimePoseSource.Connect();
        }

        activeSourceConnected = runtimePoseSource.IsConnected;
    }

    private void SetRuntimeStatus(
        bool syncSucceeded,
        bool sampleValid,
        string sampleSource,
        string message,
        string jointSummary)
    {
        lastSyncSucceeded = syncSucceeded;
        lastSampleValid = sampleValid;
        lastSampleSource = string.IsNullOrWhiteSpace(sampleSource) ? "None" : sampleSource;
        lastSyncMessage = string.IsNullOrWhiteSpace(message) ? "None" : message;
        lastSyncTime = DateTime.Now.ToString("HH:mm:ss");
        lastAppliedJointSummary = string.IsNullOrWhiteSpace(jointSummary) ? "-" : jointSummary;
    }

    private string FormatJointSummary(float[] joints)
    {
        if (joints == null || joints.Length < 6)
        {
            return "-";
        }

        return
            $"[{joints[0]:F3}, {joints[1]:F3}, {joints[2]:F3}, " +
            $"{joints[3]:F3}, {joints[4]:F3}, {joints[5]:F3}]";
    }

    private void LogStatusIfChanged()
    {
        if (!verboseLog)
        {
            return;
        }

        string statusKey =
            $"{activeSourceName}|{lastSyncSucceeded}|{lastSampleValid}|{lastSampleSource}|{lastSyncMessage}|{lastAppliedJointSummary}";

        bool changed = statusKey != previousStatusKey;
        bool jointChanged = lastAppliedJointSummary != previousAppliedJointSummary;

        if (changed || jointChanged)
        {
            Debug.Log(
                "[FR5RuntimeSyncManager] " +
                $"ActiveSource={activeSourceName} | " +
                $"Sync={lastSyncSucceeded} | " +
                $"Valid={lastSampleValid} | " +
                $"Source={lastSampleSource} | " +
                $"Message={lastSyncMessage} | " +
                $"J | " +
                $"Joints={lastAppliedJointSummary}"
            );

            previousStatusKey = statusKey;
            previousAppliedJointSummary = lastAppliedJointSummary;
        }
    }
}
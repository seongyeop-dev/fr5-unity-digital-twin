using TMPro;
using UnityEngine;

/// <summary>
/// FR5 UI 명령 라우터
///
/// 역할:
/// 1. UI 버튼 클릭 이벤트를 한 곳에서 받는다.
/// 2. 각 버튼에 맞는 컨트롤러를 호출하고, 컨트롤러가 필요한 Manager에 명령을 전달한다.
/// 3. 현재 FR5 / SDK 제어 흐름은 BottomBar 로그 중심으로 Mock 명령을 기록한다.
/// 4. 상태 변경 및 결과를 UI에 반영한다.
/// 5. RectTransform, Anchor, 위치, 크기는 코드에서 변경하지 않는다.
/// </summary>
public class scr_FR5UICommandRouter : MonoBehaviour
{
    [Header("참조 설정")]
    [SerializeField] private scr_FR5RobotManualController robotController;
    [SerializeField] private scr_FR5RuntimeSyncManager runtimeSyncManager;
    [SerializeField] private scr_FR5JointPanelUI jointPanelUI;
    [SerializeField] private scr_FR5CSharpSdkClient cSharpSdkClient;
    [SerializeField] private scr_FR5CameraTopBarUI cameraTopBarUI;
    [SerializeField] private scr_FR5RuntimeStatusPanelUI runtimeStatusPanelUI;
    [SerializeField] private scr_FR5CenterOverlayUI centerOverlayUI;
    [SerializeField] private scr_FR5BottomBarUI bottomBarUI;
    [SerializeField] private scr_FR5UILayoutModeController layoutModeController;
    [SerializeField] private scr_FR5UnityReplayJointStateSource unityReplaySource;
    [SerializeField] private scr_FR5Ros2CommandPublisher ros2CommandPublisher;

    [Header("Speed UI")]
    [SerializeField] private TMP_Text commandSpeedText;

    [Header("동작 옵션")]
    [SerializeField] private bool refreshLinkedUIAfterCommand = true;

    private void Start()
    {
        RefreshLinkedUI();
    }

    // ------------------------------------------------------------
    // LEFT / OPERATE 명령
    // ------------------------------------------------------------

    public void OnClickHome()
    {
        // In ROS2 live mode, do not directly modify the Unity robot pose.
        // Unity visual pose must be driven by /joint_states only.
        if (IsRos2JointStateSourceActive())
        {
            float[] homeTarget = GetHomeCommandJointTarget();
            bool published = TryPublishRos2Home(homeTarget);

            if (published)
            {
                SetRuntimeCommandStatus(
                    GetCommandStatusAfterPublish("HOME", published),
                    GetMotionStatusAfterCommand(published),
                    0,
                    true
                );
            }

            WriteLog(published ? "ROS2 HOME command published." : "ROS2 HOME publish failed.");
            RefreshLinkedUI();
            return;
        }

        // Local/simulation mode only.
        if (robotController != null)
        {
            robotController.MoveToHome();
        }

        WriteLog("Moved to local zero/home reference pose.");
        RefreshLinkedUI();
    }

    public void OnClickReset()
    {
        if (TryResetUnityReplay())
        {
            return;
        }

        // In ROS2 live mode, do not directly reset Unity joints.
        // Unity visual pose must continue following /joint_states.
        if (IsRos2JointStateSourceActive())
        {
            float[] resetTarget = GetResetCommandJointTarget();
            bool published = TryPublishRos2Reset(resetTarget);

            if (published)
            {
                SetRuntimeCommandStatus(
                    GetCommandStatusAfterPublish("RESET", published),
                    GetMotionStatusAfterCommand(published),
                    0,
                    true
                );
            }

            WriteLog(published ? "ROS2 RESET command published." : "ROS2 RESET publish failed.");
            RefreshLinkedUI();
            return;
        }

        // Local/simulation mode only.
        if (robotController != null)
        {
            robotController.ResetAllJoints();
        }

        WriteLog("Local joints reset to RobotZeroPoseDeg.");
        RefreshLinkedUI();
    }

    // ------------------------------------------------------------
    // LEFT / OPERATE - Jog Mode 변경
    // ------------------------------------------------------------

    public void OnClickJogJointMode()
    {
        WriteLog("Jog mode selected: JOINT.");
        RefreshLinkedUI();
    }

    public void OnClickJogBaseMode()
    {
        WriteLog("Jog mode selected: BASE.");
        RefreshLinkedUI();
    }

    public void OnClickJogToolMode()
    {
        WriteLog("Jog mode selected: TOOL.");
        RefreshLinkedUI();
    }

    public void OnClickJogWorkMode()
    {
        WriteLog("Jog mode selected: WORK.");
        RefreshLinkedUI();
    }

    // ------------------------------------------------------------
    // LEFT / OPERATE - Motion 명령
    // ------------------------------------------------------------

    public void OnClickMoveJ()
    {
        float[] jointTarget = GetCommandJointTarget();

        // When ROS2 Joint State source is active, send only a dry-run ROS2 command.
        // Do not call the C# SDK client because the hardware adapter may not be assigned.
        if (IsRos2JointStateSourceActive())
        {
            bool published = TryPublishRos2MoveJ(jointTarget);

            if (published)
            {
                SetRuntimeCommandStatus(
                    GetCommandStatusAfterPublish("MOVE_J", published),
                    GetMotionStatusAfterCommand(published),
                    0,
                    true
                );
            }

            WriteLog(published ? "ROS2 MOVE_J command published." : "ROS2 MOVE_J publish failed.");
            RefreshLinkedUI();
            return;
        }

        if (cSharpSdkClient == null)
        {
            WriteLog("C# SDK Client is not assigned.");
            RefreshLinkedUI();
            return;
        }

        cSharpSdkClient.SendMoveJ(jointTarget);
        WriteLog(cSharpSdkClient.GetLastCommandMessage());
        RefreshLinkedUI();
    }

    public void OnClickMoveL()
    {
        if (cSharpSdkClient == null)
        {
            WriteLog("C# SDK Client is not assigned.");
            RefreshLinkedUI();
            return;
        }

        cSharpSdkClient.SendMoveL(GetCommandJointTarget());
        WriteLog(cSharpSdkClient.GetLastCommandMessage());
        RefreshLinkedUI();
    }

    public void OnClickMoveC()
    {
        if (cSharpSdkClient == null)
        {
            WriteLog("C# SDK Client is not assigned.");
            RefreshLinkedUI();
            return;
        }

        cSharpSdkClient.SendMoveC(GetCommandJointTarget());
        WriteLog(cSharpSdkClient.GetLastCommandMessage());
        RefreshLinkedUI();
    }

    public void OnClickStopMotion()
    {
        if (TryStopUnityReplay())
        {
            return;
        }

        // When ROS2 Joint State source is active, send only a dry-run ROS2 STOP command.
        // Do not call the C# SDK client because the hardware adapter may not be assigned.
        if (IsRos2JointStateSourceActive())
        {
            bool published = TryPublishRos2Stop();

            if (published)
            {
                SetRuntimeCommandStatus(
                    GetCommandStatusAfterPublish("STOP", published),
                    GetMotionStatusAfterCommand(published, true),
                    0,
                    false
                );
            }

            WriteLog(published ? "ROS2 STOP command published." : "ROS2 STOP publish failed.");
            RefreshLinkedUI();
            return;
        }

        if (cSharpSdkClient == null)
        {
            WriteLog("C# SDK Client is not assigned.");
            RefreshLinkedUI();
            return;
        }

        cSharpSdkClient.StopMotion();
        WriteLog(cSharpSdkClient.GetLastCommandMessage());
        RefreshLinkedUI();
    }

    public void OnClickPauseMotion()
    {
        if (TryPauseUnityReplay())
        {
            return;
        }

        if (cSharpSdkClient == null)
        {
            WriteLog("C# SDK Client is not assigned.");
            RefreshLinkedUI();
            return;
        }

        cSharpSdkClient.PauseMotion();
        WriteLog(cSharpSdkClient.GetLastCommandMessage());
        RefreshLinkedUI();
    }

    public void OnClickResumeMotion()
    {
        if (TryResumeUnityReplay())
        {
            return;
        }

        if (cSharpSdkClient == null)
        {
            WriteLog("C# SDK Client is not assigned.");
            RefreshLinkedUI();
            return;
        }

        cSharpSdkClient.ResumeMotion();
        WriteLog(cSharpSdkClient.GetLastCommandMessage());
        RefreshLinkedUI();
    }

    // ------------------------------------------------------------
    // LEFT / OPERATE - Tool / User 좌표 변경
    // ------------------------------------------------------------

    public void OnClickSetTool0()
    {
        WriteLog("Tool coordinate selected: TOOL 0.");
        RefreshLinkedUI();
    }

    public void OnClickSetUser0()
    {
        WriteLog("User coordinate selected: USER 0.");
        RefreshLinkedUI();
    }

    public void OnClickResetToolOffset()
    {
        WriteLog("Tool offset reset requested.");
        RefreshLinkedUI();
    }

    public void OnClickCommandSpeedDown()
    {
        if (TryAdjustUnityReplaySpeed(-0.25f))
        {
            return;
        }

        if (cSharpSdkClient == null)
        {
            WriteLog("C# SDK Client is not assigned.");
            RefreshLinkedUI();
            return;
        }

        cSharpSdkClient.AdjustCommandSpeedPercent(-10);
        WriteLog("Command speed changed: " + cSharpSdkClient.GetFormattedCommandSpeedLabel());
        RefreshLinkedUI();
    }

    public void OnClickCommandSpeedUp()
    {
        if (TryAdjustUnityReplaySpeed(0.25f))
        {
            return;
        }

        if (cSharpSdkClient == null)
        {
            WriteLog("C# SDK Client is not assigned.");
            RefreshLinkedUI();
            return;
        }

        cSharpSdkClient.AdjustCommandSpeedPercent(10);
        WriteLog("Command speed changed: " + cSharpSdkClient.GetFormattedCommandSpeedLabel());
        RefreshLinkedUI();
    }



    // ------------------------------------------------------------
    // TOP / CAMERA 화면 전환 명령
    // ------------------------------------------------------------

    public void OnClickCamPerspective()
    {
        if (cameraTopBarUI != null)
        {
            cameraTopBarUI.SetPerspectiveView();
        }

        if (centerOverlayUI != null)
        {
            centerOverlayUI.SetCurrentCameraName("PERSPECTIVE");
            centerOverlayUI.SetTcpDisplayName("TCP SHADOW");
        }

        WriteLog("Camera changed: PERSPECTIVE.");
        RefreshLinkedUI();
    }

    public void OnClickCamFront()
    {
        if (cameraTopBarUI != null)
        {
            cameraTopBarUI.SetFrontView();
        }

        if (centerOverlayUI != null)
        {
            centerOverlayUI.SetCurrentCameraName("FRONT");
            centerOverlayUI.SetTcpDisplayName("TCP SHADOW");
        }

        WriteLog("Camera changed: FRONT.");
        RefreshLinkedUI();
    }

    public void OnClickCamTcp()
    {
        if (cameraTopBarUI != null)
        {
            cameraTopBarUI.SetTcpView();
        }

        if (centerOverlayUI != null)
        {
            centerOverlayUI.SetCurrentCameraName("TCP VIEW");
            centerOverlayUI.SetTcpDisplayName("TCP SHADOW");
        }

        WriteLog("Camera changed: TCP.");
        RefreshLinkedUI();
    }

    public void OnClickCamEe()
    {
        if (cameraTopBarUI != null)
        {
            cameraTopBarUI.SetEndEffectorView();
        }

        if (centerOverlayUI != null)
        {
            centerOverlayUI.SetCurrentCameraName("EE VIEW");
            centerOverlayUI.SetTcpDisplayName("END EFFECTOR");
        }

        WriteLog("Camera changed: EE.");
        RefreshLinkedUI();
    }

    public void OnClickCamRaw()
    {
        // RAW / FOCUS 같은 카메라 관련 명령은 Camera UI 상태를 확인 후 처리한다.
        WriteLog("RAW / Focus toggle is reserved.");
        RefreshLinkedUI();
    }

    // ------------------------------------------------------------
    // LEFT HEADER / 페이지 전환
    // ------------------------------------------------------------

    public void OnClickTabOperate()
    {
        if (layoutModeController != null)
        {
            layoutModeController.SetOperateMode();
        }

        WriteLog("UI page changed: OPERATE.");
        RefreshLinkedUI();
    }

    public void OnClickTabRuntime()
    {
        if (layoutModeController != null)
        {
            layoutModeController.SetRuntimeMode();
        }

        WriteLog("UI page changed: RUNTIME.");
        RefreshLinkedUI();
    }

    public void OnClickTabValidation()
    {
        if (layoutModeController != null)
        {
            layoutModeController.SetValidationMode();
        }

        WriteLog("UI page changed: VALIDATION.");
        RefreshLinkedUI();
    }

    public void OnClickTabIO()
    {
        if (layoutModeController != null)
        {
            layoutModeController.SetIOMode();
        }

        WriteLog("UI page changed: IO.");
        RefreshLinkedUI();
    }

    public void OnClickTabCamera()
    {
        if (layoutModeController != null)
        {
            // 실제 카메라 모드가 아니라 LeftDock의 CameraPage를 표시한다.
            layoutModeController.SetCameraMode();
        }

        WriteLog("UI page changed: CAMERA PAGE.");
        RefreshLinkedUI();
    }

    // ------------------------------------------------------------
    // LEFT / RUNTIME 입력 모드 전환
    // ------------------------------------------------------------

    public void OnClickSetSimMode()
    {
        if (runtimeSyncManager != null)
        {
            runtimeSyncManager.SetInputMode(scr_FR5RuntimeSyncManager.RuntimeInputMode.SimManual);
        }

        WriteLog("Input mode changed: SIM MANUAL.");
        RefreshLinkedUI();
    }

    public void OnClickSetLiveMode()
    {
        if (runtimeSyncManager != null)
        {
            runtimeSyncManager.SetInputMode(scr_FR5RuntimeSyncManager.RuntimeInputMode.LiveRuntimeSource);
        }

        WriteLog("Input mode changed: LIVE.");
        RefreshLinkedUI();
    }

    // ------------------------------------------------------------
    // LEFT / RUNTIME Source 전환
    // ------------------------------------------------------------

    public void OnClickUseMockSource()
    {
        SetRuntimeSource(
            scr_FR5RuntimeSyncManager.RuntimeSourceType.MockSdk,
            "Runtime source changed: MOCK SDK."
        );
    }

    public void OnClickUseCSharpBridgeSource()
    {
        SetRuntimeSource(
            scr_FR5RuntimeSyncManager.RuntimeSourceType.CSharpBridge,
            "Runtime source changed: C SHARP BRIDGE."
        );
    }

    public void OnClickUsePythonBridgeSource()
    {
        SetRuntimeSource(
            scr_FR5RuntimeSyncManager.RuntimeSourceType.PythonBridge,
            "Runtime source changed: PYTHON BRIDGE."
        );
    }

    public void OnClickUseCSharpSdkSource()
    {
        SetRuntimeSource(
            scr_FR5RuntimeSyncManager.RuntimeSourceType.CSharpSdk,
            "Runtime source changed: C SHARP SDK."
        );
    }

    public void OnClickUseUnityReplaySource()
    {
        SetRuntimeSource(
            scr_FR5RuntimeSyncManager.RuntimeSourceType.UnityReplay,
            "Runtime source changed: UNITY REPLAY."
        );
    }

    public void OnClickUseReplaySource()
    {
        OnClickUseUnityReplaySource();
    }

    public void OnClickUseRos2JointStateSource()
    {
        SetRuntimeSource(
            scr_FR5RuntimeSyncManager.RuntimeSourceType.Ros2JointState,
            "Runtime source changed: ROS2 JOINT STATE."
        );
    }

    public void OnClickSyncLiveOnce()
    {
        if (runtimeSyncManager == null)
        {
            WriteLog("RuntimeSyncManager is not assigned.");
            RefreshLinkedUI();
            return;
        }

        bool succeeded = runtimeSyncManager.SyncLiveOnce();

        if (succeeded)
        {
            WriteLog("LIVE sync executed.");
        }
        else
        {
            WriteLog(runtimeSyncManager.GetEffectiveMessageLabel());
        }

        RefreshLinkedUI();
    }

    // ------------------------------------------------------------
    // LEFT / VALIDATION 명령
    // ------------------------------------------------------------

    public void OnClickRunCurrentValidation()
    {
        if (robotController != null)
        {
            robotController.ForceRefreshStatus();
        }

        WriteLog("Validation requested: CURRENT CASE.");
        RefreshLinkedUI();
    }

    public void OnClickRunAllValidation()
    {
        WriteLog("Validation requested: ALL CASES.");
        RefreshLinkedUI();
    }

    public void OnClickLoadPythonResult()
    {
        WriteLog("Load Python result is reserved.");
        RefreshLinkedUI();
    }

    public void OnClickTogglePythonMarker()
    {
        WriteLog("Python marker toggle is reserved.");
        RefreshLinkedUI();
    }

    public void OnClickToggleCSharpMarker()
    {
        WriteLog("C# marker toggle is reserved.");
        RefreshLinkedUI();
    }

    public void OnClickToggleErrorLine()
    {
        WriteLog("Error line toggle is reserved.");
        RefreshLinkedUI();
    }

    public void OnClickResetValidationView()
    {
        if (robotController != null)
        {
            robotController.ClearPythonTCP();
            robotController.ForceRefreshStatus();
        }

        WriteLog("Validation view reset.");
        RefreshLinkedUI();
    }

    // ------------------------------------------------------------
    // LEFT / IO 명령
    // ------------------------------------------------------------

    public void OnClickDO0On()
    {
        WriteLog("IO command requested: DO0 ON.");
        RefreshLinkedUI();
    }

    public void OnClickDO0Off()
    {
        WriteLog("IO command requested: DO0 OFF.");
        RefreshLinkedUI();
    }

    public void OnClickDO1On()
    {
        WriteLog("IO command requested: DO1 ON.");
        RefreshLinkedUI();
    }

    public void OnClickDO1Off()
    {
        WriteLog("IO command requested: DO1 OFF.");
        RefreshLinkedUI();
    }

    public void OnClickToolDO0On()
    {
        WriteLog("Tool IO command requested: TOOL DO0 ON.");
        RefreshLinkedUI();
    }

    public void OnClickToolDO0Off()
    {
        WriteLog("Tool IO command requested: TOOL DO0 OFF.");
        RefreshLinkedUI();
    }

    public void OnClickGripperOpen()
    {
        WriteLog("Gripper command requested: OPEN.");
        RefreshLinkedUI();
    }

    public void OnClickGripperClose()
    {
        WriteLog("Gripper command requested: CLOSE.");
        RefreshLinkedUI();
    }

    public void OnClickRefreshIO()
    {
        WriteLog("IO status refresh requested.");
        RefreshLinkedUI();
    }

    // ------------------------------------------------------------
    // LEFT / CAMERA PAGE 명령
    // ------------------------------------------------------------

    public void OnClickCameraWorkView()
    {
        OnClickCamPerspective();
    }

    public void OnClickCameraFrontView()
    {
        OnClickCamFront();
    }

    public void OnClickCameraTcpFocus()
    {
        OnClickCamTcp();
    }

    public void OnClickCameraEeFocus()
    {
        OnClickCamEe();
    }

    public void OnClickCameraRawFocus()
    {
        OnClickCamRaw();
    }

    public void OnClickEnterCameraFullscreenMode()
    {
        if (layoutModeController != null)
        {
            layoutModeController.EnterCameraFullscreenMode();
        }

        WriteLog("Camera fullscreen mode opened.");
        RefreshLinkedUI();
    }

    public void OnClickExitCameraMode()
    {
        if (layoutModeController != null)
        {
            layoutModeController.ExitCameraMode();
        }

        WriteLog("Camera mode closed.");
        RefreshLinkedUI();
    }

    // ------------------------------------------------------------
    // UI 연결
    // ------------------------------------------------------------

    public void RefreshLinkedUI()
    {
        if (!refreshLinkedUIAfterCommand)
        {
            return;
        }

        if (robotController != null)
        {
            robotController.ForceRefreshStatus();
        }

        if (commandSpeedText != null)
        {
            if (cSharpSdkClient != null)
            {
                commandSpeedText.text = cSharpSdkClient.GetFormattedCommandSpeedLabel();
            }
            else
            {
                commandSpeedText.text = "100%";
            }
        }

        if (layoutModeController != null)
        {
            layoutModeController.RefreshLayout();
        }

        if (cameraTopBarUI != null)
        {
            cameraTopBarUI.RefreshUI();
        }

        if (runtimeStatusPanelUI != null)
        {
            runtimeStatusPanelUI.RefreshUI();
        }

        if (centerOverlayUI != null)
        {
            centerOverlayUI.RefreshUI();
        }

        if (bottomBarUI != null)
        {
            bottomBarUI.RefreshUI();
        }
    }

    // ------------------------------------------------------------
    // 공통 명령 처리
    // ------------------------------------------------------------

    private bool IsUnityReplaySourceActive()
    {
        return runtimeSyncManager != null &&
               runtimeSyncManager.SelectedRuntimeSource == scr_FR5RuntimeSyncManager.RuntimeSourceType.UnityReplay;
    }

    private bool IsRos2JointStateSourceActive()
    {
        return runtimeSyncManager != null &&
               runtimeSyncManager.SelectedRuntimeSource == scr_FR5RuntimeSyncManager.RuntimeSourceType.Ros2JointState;
    }


    private scr_FR5UnityReplayJointStateSource ResolveUnityReplaySource()
    {
        if (unityReplaySource != null)
        {
            return unityReplaySource;
        }

        scr_FR5UnityReplayJointStateSource[] sources = Resources.FindObjectsOfTypeAll<scr_FR5UnityReplayJointStateSource>();

        if (sources != null && sources.Length > 0)
        {
            unityReplaySource = sources[0];
        }

        return unityReplaySource;
    }

    private bool TryResetUnityReplay()
    {
        if (!IsUnityReplaySourceActive())
        {
            return false;
        }

        scr_FR5UnityReplayJointStateSource replaySource = ResolveUnityReplaySource();

        if (replaySource == null)
        {
            WriteLog("Unity Replay Source is not assigned.");
            RefreshLinkedUI();
            return true;
        }

        replaySource.ResetReplayToStart();
        WriteLog("Unity replay reset to first sample.");
        RefreshLinkedUI();
        return true;
    }

    private bool TryStopUnityReplay()
    {
        if (!IsUnityReplaySourceActive())
        {
            return false;
        }

        scr_FR5UnityReplayJointStateSource replaySource = ResolveUnityReplaySource();

        if (replaySource == null)
        {
            WriteLog("Unity Replay Source is not assigned.");
            RefreshLinkedUI();
            return true;
        }

        replaySource.StopReplay();
        WriteLog("Unity replay stopped.");
        RefreshLinkedUI();
        return true;
    }

    private bool TryPauseUnityReplay()
    {
        if (!IsUnityReplaySourceActive())
        {
            return false;
        }

        scr_FR5UnityReplayJointStateSource replaySource = ResolveUnityReplaySource();

        if (replaySource == null)
        {
            WriteLog("Unity Replay Source is not assigned.");
            RefreshLinkedUI();
            return true;
        }

        replaySource.PauseReplay();
        WriteLog("Unity replay paused.");
        RefreshLinkedUI();
        return true;
    }

    private bool TryResumeUnityReplay()
    {
        if (!IsUnityReplaySourceActive())
        {
            return false;
        }

        scr_FR5UnityReplayJointStateSource replaySource = ResolveUnityReplaySource();

        if (replaySource == null)
        {
            WriteLog("Unity Replay Source is not assigned.");
            RefreshLinkedUI();
            return true;
        }

        replaySource.ResumeReplay();
        WriteLog("Unity replay resumed.");
        RefreshLinkedUI();
        return true;
    }

    private bool TryAdjustUnityReplaySpeed(float delta)
    {
        if (!IsUnityReplaySourceActive())
        {
            return false;
        }

        scr_FR5UnityReplayJointStateSource replaySource = ResolveUnityReplaySource();

        if (replaySource == null)
        {
            WriteLog("Unity Replay Source is not assigned.");
            RefreshLinkedUI();
            return true;
        }

        replaySource.AdjustPlaybackSpeed(delta);
        WriteLog("Unity replay speed changed: " + replaySource.GetPlaybackSpeedLabel());
        RefreshLinkedUI();
        return true;
    }

    private void SetRuntimeSource(scr_FR5RuntimeSyncManager.RuntimeSourceType sourceType, string logMessage)
    {
        if (runtimeSyncManager != null)
        {
            runtimeSyncManager.SetRuntimeSource(sourceType);
        }

        WriteLog(logMessage);
        RefreshLinkedUI();
    }

    private float[] GetCommandJointTarget()
    {
        if (jointPanelUI != null)
        {
            float[] panelTarget = jointPanelUI.GetTargetAnglesCopy();
            Debug.Log($"[FR5UICommandRouter] MOVE_J target from JointPanelUI: [{string.Join(", ", panelTarget)}]");
            return panelTarget;
        }

        if (robotController != null)
        {
            float[] controllerTarget = robotController.GetCurrentJointArray();
            Debug.LogWarning($"[FR5UICommandRouter] JointPanelUI is not assigned. MOVE_J target from RobotController: [{string.Join(", ", controllerTarget)}]");
            return controllerTarget;
        }

        Debug.LogWarning("[FR5UICommandRouter] JointPanelUI and RobotController are not assigned. MOVE_J target fallback is zero.");
        return new float[] { 0f, 0f, 0f, 0f, 0f, 0f };
    }

    private float[] GetHomeCommandJointTarget()
    {
        if (jointPanelUI != null)
        {
            float[] homeTarget = jointPanelUI.ApplyHomePresetForCommand();
            Debug.Log($"[FR5UICommandRouter] HOME target from JointPanelUI: [{string.Join(", ", homeTarget)}]");
            return homeTarget;
        }

        Debug.LogWarning("[FR5UICommandRouter] JointPanelUI is not assigned. HOME target fallback is zero.");
        return new float[] { 0f, 0f, 0f, 0f, 0f, 0f };
    }

    private float[] GetResetCommandJointTarget()
    {
        if (jointPanelUI != null)
        {
            float[] resetTarget = jointPanelUI.ApplyResetZeroPresetForCommand();
            Debug.Log($"[FR5UICommandRouter] RESET target from JointPanelUI: [{string.Join(", ", resetTarget)}]");
            return resetTarget;
        }

        Debug.LogWarning("[FR5UICommandRouter] JointPanelUI is not assigned. RESET target fallback is zero.");
        return new float[] { 0f, 0f, 0f, 0f, 0f, 0f };
    }

    private int GetCommandSpeedPercent()
    {
        return cSharpSdkClient != null ? cSharpSdkClient.GetCommandSpeedPercent() : 100;
    }

    private scr_FR5Ros2CommandPublisher ResolveRos2CommandPublisher()
    {
        if (ros2CommandPublisher != null)
        {
            return ros2CommandPublisher;
        }

        ros2CommandPublisher = FindObjectOfType<scr_FR5Ros2CommandPublisher>();

        if (ros2CommandPublisher == null)
        {
            GameObject publisherObject = new GameObject("FR5Ros2CommandPublisher_Runtime");
            ros2CommandPublisher = publisherObject.AddComponent<scr_FR5Ros2CommandPublisher>();
        }

        return ros2CommandPublisher;
    }

    private bool TryPublishRos2MoveJ(float[] jointTarget)
    {
        scr_FR5Ros2CommandPublisher publisher = ResolveRos2CommandPublisher();

        if (publisher == null)
        {
            return false;
        }

        return publisher.PublishMoveJ(jointTarget, GetCommandSpeedPercent());
    }

    private bool TryPublishRos2Stop()
    {
        scr_FR5Ros2CommandPublisher publisher = ResolveRos2CommandPublisher();

        if (publisher == null)
        {
            return false;
        }

        return publisher.PublishStop();
    }

    private bool TryPublishRos2Home(float[] homeTarget)
    {
        scr_FR5Ros2CommandPublisher publisher = ResolveRos2CommandPublisher();

        if (publisher == null)
        {
            return false;
        }

        return publisher.PublishHome(homeTarget, GetCommandSpeedPercent());
    }

    private bool TryPublishRos2Reset(float[] resetTarget)
    {
        scr_FR5Ros2CommandPublisher publisher = ResolveRos2CommandPublisher();

        if (publisher == null)
        {
            return false;
        }

        return publisher.PublishReset(resetTarget, GetCommandSpeedPercent());
    }

    private void SetRuntimeCommandStatus(
        string commandStatus,
        string motionStatus,
        int queueCount = 0,
        bool autoClear = true)
    {
        if (runtimeStatusPanelUI != null)
        {
            runtimeStatusPanelUI.SetCommandMotionStatus(commandStatus, motionStatus, queueCount, autoClear);
        }
    }

    private void WriteLog(string message)
    {
        if (bottomBarUI != null)
        {
            bottomBarUI.SetLogMessage(message);
        }
    }

    private bool IsRos2FeedbackReady()
    {
        // ROS2 JointState source가 아니면 ROS2 feedback ready로 보지 않습니다.
        if (!IsRos2JointStateSourceActive())
        {
            return false;
        }

        // RuntimeSyncManager가 없으면 정확한 상태를 판단할 수 없습니다.
        if (runtimeSyncManager == null)
        {
            return false;
        }

        // /joint_states sample이 유효해야 실제 ROS2 feedback이 살아있다고 판단합니다.
        return runtimeSyncManager.LastSampleValid;
    }

    private string GetMotionStatusAfterCommand(bool published, bool holdWhenReady = false)
    {
        if (!published)
        {
            return "READY";
        }

        if (!IsRos2FeedbackReady())
        {
            return "WAITING ROS2";
        }

        return holdWhenReady ? "HOLD" : "MOVING";
    }

    private string GetCommandStatusAfterPublish(string commandName, bool published)
    {
        string normalizedCommand = string.IsNullOrWhiteSpace(commandName)
            ? "COMMAND"
            : commandName.ToUpperInvariant();

        return published
            ? $"{normalizedCommand} SENT"
            : $"{normalizedCommand} FAILED";
    }
}
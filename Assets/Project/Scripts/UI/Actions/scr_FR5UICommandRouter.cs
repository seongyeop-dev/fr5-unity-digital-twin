using TMPro;
using UnityEngine;

/// <summary>
/// FR5 UI 명령 라우터
///
/// 역할:
/// 1. UI 버튼 클릭 이벤트를 한 곳에서 받는다.
/// 2. 각 버튼에 맞는 컨트롤러를 호출하고, 필요한 Manager에 명령을 전달한다.
/// 3. FR5 / SDK / ROS2 명령 흐름을 BottomBar 로그와 Runtime Status에 반영한다.
/// 4. ROS2 Live Source 상태에서는 Unity visual pose를 직접 바꾸지 않고 /joint_states feedback만 따른다.
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
    [SerializeField] private scr_PythonAutoRunner pythonAutoRunner;

    [Header("Speed UI")]
    [SerializeField] private TMP_Text commandSpeedText;

    [Header("동작 옵션")]
    [SerializeField] private bool refreshLinkedUIAfterCommand = true;

    [Header("ROS2 Gazebo Validation Pose")]
    [SerializeField] private bool useRos2GazeboSafePoseForHomeAndReset = false;
    [SerializeField] private bool useRos2GazeboSafePoseWhenMoveJTargetIsZero = false;
    [SerializeField] private float[] ros2GazeboSafePoseDeg = new float[] { 0f, -60f, 90f, -90f, -90f, 0f };

    private void Start()
    {
        RefreshLinkedUI();
    }

    // ------------------------------------------------------------
    // LEFT / OPERATE 명령
    // ------------------------------------------------------------

    public void OnClickHome()
    {
        // ROS2 Live mode에서는 Unity robot pose를 직접 변경하지 않는다.
        // Unity visual pose는 /joint_states feedback만 따라간다.
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

        // Local / Simulation mode에서만 Unity pose를 직접 변경한다.
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

        // ROS2 Live mode에서는 Unity joint를 직접 reset하지 않는다.
        // Unity visual pose는 계속 /joint_states feedback을 따라간다.
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

        // Local / Simulation mode에서만 Unity pose를 직접 변경한다.
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

        // ROS2 Joint State source가 활성화되어 있으면 ROS2 command만 publish한다.
        // hardware adapter가 연결되어 있지 않을 수 있으므로 C# SDK client는 호출하지 않는다.
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
        // RAW / FOCUS는 카메라 UI 상태만 확인하는 예약 기능으로 처리한다.
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
            // 전체 카메라 모드가 아니라 LeftDock의 CameraPage를 표시한다.
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
        if (IsRos2JointStateSourceActive())
        {
            bool published = TryPublishRos2GripperOpen();
            SetRuntimeCommandStatus(
                GetCommandStatusAfterPublish("GRIPPER_OPEN", published),
                GetMotionStatusAfterCommand(published),
                0,
                true
            );
            WriteLog(published ? "ROS2 GRIPPER_OPEN command published." : "ROS2 GRIPPER_OPEN publish failed.");
            RefreshLinkedUI();
            return;
        }

        WriteLog("Gripper command requested: OPEN.");
        RefreshLinkedUI();
    }

    public void OnClickGripperClose()
    {
        OnClickGripperNormalClose();
    }

    public void OnClickGripperSmallClose()
    {
        if (IsRos2JointStateSourceActive())
        {
            bool published = TryPublishRos2GripperSmallClose();
            SetRuntimeCommandStatus(
                GetCommandStatusAfterPublish("GRIPPER_SMALL_CLOSE", published),
                GetMotionStatusAfterCommand(published),
                0,
                true
            );
            WriteLog(published ? "ROS2 GRIPPER_SMALL_CLOSE command published." : "ROS2 GRIPPER_SMALL_CLOSE publish failed.");
            RefreshLinkedUI();
            return;
        }

        WriteLog("Gripper command requested: SMALL CLOSE.");
        RefreshLinkedUI();
    }

    public void OnClickGripperNormalClose()
    {
        if (IsRos2JointStateSourceActive())
        {
            bool published = TryPublishRos2GripperNormalClose();
            SetRuntimeCommandStatus(
                GetCommandStatusAfterPublish("GRIPPER_NORMAL_CLOSE", published),
                GetMotionStatusAfterCommand(published),
                0,
                true
            );
            WriteLog(published ? "ROS2 GRIPPER_NORMAL_CLOSE command published." : "ROS2 GRIPPER_NORMAL_CLOSE publish failed.");
            RefreshLinkedUI();
            return;
        }

        WriteLog("Gripper command requested: NORMAL CLOSE.");
        RefreshLinkedUI();
    }

    public void OnClickGripperReturnOpen()
    {
        if (IsRos2JointStateSourceActive())
        {
            bool published = TryPublishRos2GripperReturnOpen();
            SetRuntimeCommandStatus(
                GetCommandStatusAfterPublish("GRIPPER_RETURN_OPEN", published),
                GetMotionStatusAfterCommand(published),
                0,
                true
            );
            WriteLog(published ? "ROS2 GRIPPER_RETURN_OPEN command published." : "ROS2 GRIPPER_RETURN_OPEN publish failed.");
            RefreshLinkedUI();
            return;
        }

        WriteLog("Gripper command requested: RETURN OPEN.");
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
    // Button wiring safe wrapper methods
    // ------------------------------------------------------------

    public void OnClickStop()
    {
        OnClickStopMotion();
    }

    public void OnClickGripperSmall()
    {
        OnClickGripperSmallClose();
    }

    public void OnClickGripperNormal()
    {
        OnClickGripperNormalClose();
    }

    public void OnClickGripperReturn()
    {
        OnClickGripperReturnOpen();
    }

    public void OnClickJogJ1Plus()
    {
        ApplyJogStepToJointInput(0, 10f);
    }

    public void OnClickJogJ1Minus()
    {
        ApplyJogStepToJointInput(0, -10f);
    }

    public void OnClickJogJ2Plus()
    {
        ApplyJogStepToJointInput(1, 10f);
    }

    public void OnClickJogJ2Minus()
    {
        ApplyJogStepToJointInput(1, -10f);
    }

    public void OnClickJogJ3Plus()
    {
        ApplyJogStepToJointInput(2, 10f);
    }

    public void OnClickJogJ3Minus()
    {
        ApplyJogStepToJointInput(2, -10f);
    }

    public void OnClickJogJ4Plus()
    {
        ApplyJogStepToJointInput(3, 10f);
    }

    public void OnClickJogJ4Minus()
    {
        ApplyJogStepToJointInput(3, -10f);
    }

    public void OnClickJogJ5Plus()
    {
        ApplyJogStepToJointInput(4, 10f);
    }

    public void OnClickJogJ5Minus()
    {
        ApplyJogStepToJointInput(4, -10f);
    }

    public void OnClickJogJ6Plus()
    {
        ApplyJogStepToJointInput(5, 10f);
    }

    public void OnClickJogJ6Minus()
    {
        ApplyJogStepToJointInput(5, -10f);
    }
    public void OnClickExportCurrentJointToPython()
    {
        if (pythonAutoRunner == null)
        {
            WriteLog("PythonAutoRunner is not assigned.");
            RefreshLinkedUI();
            return;
        }

        bool exported = pythonAutoRunner.ExportCurrentJointToPythonInput();
        WriteLog(exported ? "Current joint exported to Python input." : "Current joint export to Python input failed.");
        RefreshLinkedUI();
    }

    public void OnClickRunPythonGroundTruth()
    {
        if (pythonAutoRunner == null)
        {
            WriteLog("PythonAutoRunner is not assigned.");
            RefreshLinkedUI();
            return;
        }

        bool succeeded = pythonAutoRunner.RunLiveValidation();
        WriteLog(succeeded ? "Python ground truth validation completed." : "Python ground truth validation failed.");
        RefreshLinkedUI();
    }

    public void OnClickResetOffset()
    {
        OnClickResetToolOffset();
    }

    public void OnClickPoseReset()
    {
        ApplyPosePresetToJointInput("Pose RESET", new float[] { 0f, 0f, 0f, 0f, 0f, 0f });
    }

    public void OnClickPoseRos2Demo()
    {
        ApplyPosePresetToJointInput("Pose ROS2 DEMO", new float[] { 0f, -60f, 90f, -90f, -90f, 0f });
    }

    public void OnClickPoseSmallSafe()
    {
        ApplyPosePresetToJointInput("Pose SMALL SAFE", new float[] { 10f, -45f, 75f, -30f, -60f, 15f });
    }
    // ------------------------------------------------------------
    // UI 갱신
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
    // 내부 처리
    // ------------------------------------------------------------

    private void ApplyJogStepToJointInput(int jointIndex, float deltaDegrees)
    {
        if (jointPanelUI == null)
        {
            WriteLog($"J{jointIndex + 1} input step failed. JointPanelUI is not assigned.");
            RefreshLinkedUI();
            return;
        }

        jointPanelUI.StepJointInputForCommand(jointIndex, deltaDegrees);
        WriteLog($"J{jointIndex + 1} input {(deltaDegrees >= 0f ? "+" : "")}{deltaDegrees:0} deg loaded. Press MOVE J to execute.");
        RefreshLinkedUI();
    }
    private void ApplyPosePresetToJointInput(string label, float[] preset)
    {
        if (jointPanelUI == null)
        {
            WriteLog($"{label} failed. JointPanelUI is not assigned.");
            RefreshLinkedUI();
            return;
        }

        jointPanelUI.ApplyPosePresetForCommand(preset);
        WriteLog($"{label} loaded to joint input. Press MOVE J to execute.");
        RefreshLinkedUI();
    }
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

        Debug.LogWarning("[FR5UICommandRouter] JointPanelUI and RobotController are not assigned. MOVE_J target fallback is RobotZeroPoseDeg.");
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

        Debug.LogWarning("[FR5UICommandRouter] JointPanelUI is not assigned. HOME target fallback is RobotZeroPoseDeg.");
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

        Debug.LogWarning("[FR5UICommandRouter] JointPanelUI is not assigned. RESET target fallback is RobotZeroPoseDeg.");
        return new float[] { 0f, 0f, 0f, 0f, 0f, 0f };
    }

    private bool ShouldReplaceZeroMoveJWithRos2SafePose(float[] target)
    {
        return IsRos2JointStateSourceActive() &&
               useRos2GazeboSafePoseWhenMoveJTargetIsZero &&
               IsApproximatelyZeroPose(target);
    }

    private bool IsApproximatelyZeroPose(float[] target)
    {
        if (target == null || target.Length < 6)
        {
            return true;
        }

        for (int i = 0; i < 6; i++)
        {
            if (Mathf.Abs(target[i]) > 0.001f)
            {
                return false;
            }
        }

        return true;
    }

    private float[] GetRos2GazeboSafePoseCopy()
    {
        float[] result = new float[6];

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = ros2GazeboSafePoseDeg != null && i < ros2GazeboSafePoseDeg.Length
                ? ros2GazeboSafePoseDeg[i]
                : 0f;
        }

        return result;
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

    private bool TryPublishRos2GripperOpen()
    {
        scr_FR5Ros2CommandPublisher publisher = ResolveRos2CommandPublisher();

        if (publisher == null)
        {
            return false;
        }

        return publisher.PublishGripperOpen(GetCommandSpeedPercent());
    }

    private bool TryPublishRos2GripperSmallClose()
    {
        scr_FR5Ros2CommandPublisher publisher = ResolveRos2CommandPublisher();

        if (publisher == null)
        {
            return false;
        }

        return publisher.PublishGripperSmallClose(GetCommandSpeedPercent());
    }

    private bool TryPublishRos2GripperNormalClose()
    {
        scr_FR5Ros2CommandPublisher publisher = ResolveRos2CommandPublisher();

        if (publisher == null)
        {
            return false;
        }

        return publisher.PublishGripperNormalClose(GetCommandSpeedPercent());
    }

    private bool TryPublishRos2GripperReturnOpen()
    {
        scr_FR5Ros2CommandPublisher publisher = ResolveRos2CommandPublisher();

        if (publisher == null)
        {
            return false;
        }

        return publisher.PublishGripperReturnOpen(GetCommandSpeedPercent());
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
        // ROS2 JointState source가 아니면 ROS2 feedback ready 상태로 판단하지 않는다.
        if (!IsRos2JointStateSourceActive())
        {
            return false;
        }

        // RuntimeSyncManager 상태를 기준으로 feedback 유효 여부를 판단한다.
        if (runtimeSyncManager == null)
        {
            return false;
        }

        // /joint_states sample이 유효해야 ROS2 feedback이 들어온 것으로 판단한다.
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
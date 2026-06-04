using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FR5 Joint Panel UI
///
/// 역할:
/// 1. Joint Panel 하나를 LIVE / COMMAND 2모드로 관리
/// 2. LIVE 모드에서는 실제 로봇에서 들어온 현재값(actual) 표시
/// 3. COMMAND 모드에서는 사용자가 편집하는 목표값(target) 표시
/// 4. 슬라이더 / 입력 / -+ 는 target만 수정
/// 5. target은 가상 로봇에 미리보기로 적용하고, 실제 전송은 다음 단계에서 연결
/// </summary>
public class scr_FR5JointPanelUI : MonoBehaviour
{
    public enum JointPanelMode
    {
        LiveMonitor,
        CommandEdit
    }

    [System.Serializable]
    public class JointRowUIBinding
    {
        public string displayName = "J";
        public TMP_Text labelText;
        public Button minusStepButton;
        public Slider slider;
        public Button plusStepButton;
        public TMP_InputField inputField;
        public TMP_Text valueText;
    }

    [Header("References")]
    [SerializeField] private scr_VirtualJointController robotController;
    [SerializeField] private scr_FR5RuntimeSyncManager runtimeSyncManager;

    [Header("Joint UI Rows")]
    [SerializeField] private JointRowUIBinding[] jointRows;

    [Header("Buttons")]
    [SerializeField] private Button btnHome;
    [SerializeField] private Button btnReset;

    [Header("Modes")]
    [SerializeField] private JointPanelMode currentMode = JointPanelMode.LiveMonitor;
    [SerializeField] private bool syncFromControllerEveryFrame = true;
    [SerializeField] private bool autoEnterCommandModeOnEdit = true;
    [SerializeField] private bool previewTargetPoseOnVirtualRobot = true;

    [Header("Command Edit Options")]
    [SerializeField] private float stepDegrees = 10f;
    [SerializeField] private string inputFormat = "0.0";

    // Command UI home preset only.
    // This is not confirmed as the actual FAIRINO SDK home pose.
    // See Docs/ProjectMap/README_Pose_Convention.md.
    [SerializeField] private float[] homePresetJointAngles = new float[6] { 0f, -90f, 90f, -90f, -90f, 0f };

    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = false;

    private readonly float[] actualJointAngles = new float[6];
    private readonly float[] targetJointAngles = new float[6];

    private bool isInternalSync = false;

    public bool IsCommandEditMode => currentMode == JointPanelMode.CommandEdit;
    public bool IsLiveMonitorMode => currentMode == JointPanelMode.LiveMonitor;

    public string GetCurrentModeLabel()
    {
        return currentMode == JointPanelMode.LiveMonitor ? "LIVE MONITOR" : "COMMAND EDIT";
    }

    private void Start()
    {
        BindButtons();
        InitializeJointRows();

        CaptureActualFromRobotController();
        CopyActualToTarget();

        ResumeLiveMonitorMode(syncImmediately: false);
    }

    private void Update()
    {
        if (!syncFromControllerEveryFrame || robotController == null)
        {
            return;
        }

        if (currentMode == JointPanelMode.LiveMonitor)
        {
            CaptureActualFromRobotController();
            SyncAllFromActual();
        }
        else
        {
            SyncAllFromTarget();
        }
    }

    private void BindButtons()
    {
        if (btnHome != null)
        {
            btnHome.onClick.RemoveAllListeners();
            btnHome.onClick.AddListener(OnClickHome);
        }

        if (btnReset != null)
        {
            btnReset.onClick.RemoveAllListeners();
            btnReset.onClick.AddListener(OnClickReset);
        }
    }

    private void InitializeJointRows()
    {
        if (robotController == null || jointRows == null)
        {
            return;
        }

        for (int i = 0; i < jointRows.Length; i++)
        {
            int capturedIndex = i;
            JointRowUIBinding row = jointRows[i];

            if (row == null)
            {
                continue;
            }

            if (row.labelText != null)
            {
                string controllerName = robotController.GetJointName(i);
                row.labelText.text = string.IsNullOrWhiteSpace(controllerName) ? row.displayName : controllerName;
            }

            if (row.slider != null)
            {
                row.slider.onValueChanged.RemoveAllListeners();
                row.slider.minValue = robotController.GetJointMinAngle(i);
                row.slider.maxValue = robotController.GetJointMaxAngle(i);
                row.slider.SetValueWithoutNotify(robotController.GetJointAngle(i));
                row.slider.onValueChanged.AddListener(value => OnSliderChanged(capturedIndex, value));
            }

            if (row.minusStepButton != null)
            {
                row.minusStepButton.onClick.RemoveAllListeners();
                row.minusStepButton.onClick.AddListener(() => OnClickStep(capturedIndex, -stepDegrees));
            }

            if (row.plusStepButton != null)
            {
                row.plusStepButton.onClick.RemoveAllListeners();
                row.plusStepButton.onClick.AddListener(() => OnClickStep(capturedIndex, stepDegrees));
            }

            if (row.inputField != null)
            {
                row.inputField.onEndEdit.RemoveAllListeners();
                row.inputField.SetTextWithoutNotify(robotController.GetJointAngle(i).ToString(inputFormat, CultureInfo.InvariantCulture));
                row.inputField.onEndEdit.AddListener(text => OnInputSubmitted(capturedIndex, text));
            }
        }
    }

    private void OnSliderChanged(int index, float value)
    {
        if (isInternalSync || robotController == null)
        {
            return;
        }

        if (!EnsureCommandEditMode())
        {
            return;
        }

        SetTargetAngle(index, value, previewImmediately: true);

        if (enableDebugLog)
        {
            Debug.Log($"[FR5JointPanelUI] Slider changed | Index={index} | Target={targetJointAngles[index]:0.0}");
        }
    }

    private void OnClickStep(int index, float deltaDegrees)
    {
        if (robotController == null)
        {
            return;
        }

        if (!EnsureCommandEditMode())
        {
            return;
        }

        float nextValue = targetJointAngles[index] + deltaDegrees;
        SetTargetAngle(index, nextValue, previewImmediately: true);

        if (enableDebugLog)
        {
            Debug.Log($"[FR5JointPanelUI] Step clicked | Index={index} | Delta={deltaDegrees:0.0} | Target={targetJointAngles[index]:0.0}");
        }
    }

    private void OnInputSubmitted(int index, string rawText)
    {
        if (isInternalSync || robotController == null)
        {
            return;
        }

        if (!EnsureCommandEditMode())
        {
            return;
        }

        float parsedValue;
        if (!float.TryParse(rawText, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue) &&
            !float.TryParse(rawText, NumberStyles.Float, CultureInfo.CurrentCulture, out parsedValue))
        {
            parsedValue = targetJointAngles[index];
        }

        SetTargetAngle(index, parsedValue, previewImmediately: true);

        if (enableDebugLog)
        {
            Debug.Log($"[FR5JointPanelUI] Input submitted | Index={index} | Target={targetJointAngles[index]:0.0}");
        }
    }

    private void OnClickHome()
    {
        EnterCommandEditFromCurrentPose();
        ApplyPreset(homePresetJointAngles);

        if (enableDebugLog)
        {
            Debug.Log("[FR5JointPanelUI] Command home preset applied to target pose.");
        }
    }

    private void OnClickReset()
    {
        EnterCommandEditFromCurrentPose();
        ApplyZeroPreset();

        if (enableDebugLog)
        {
            Debug.Log("[FR5JointPanelUI] Zero preset applied to target pose.");
        }
    }

    private bool EnsureCommandEditMode()
    {
        if (currentMode == JointPanelMode.CommandEdit)
        {
            return true;
        }

        if (!autoEnterCommandModeOnEdit)
        {
            return false;
        }

        EnterCommandEditFromCurrentPose();
        return true;
    }

    private void EnterCommandEditFromCurrentPose()
    {
        if (currentMode == JointPanelMode.CommandEdit)
        {
            return;
        }

        CaptureActualFromRobotController();
        CopyActualToTarget();

        currentMode = JointPanelMode.CommandEdit;

        if (runtimeSyncManager != null)
        {
            runtimeSyncManager.SetRuntimeApplyEnabled(false);
        }

        if (previewTargetPoseOnVirtualRobot)
        {
            ApplyTargetPoseToVirtualRobot();
        }

        SyncAllFromTarget();

        if (enableDebugLog)
        {
            Debug.Log("[FR5JointPanelUI] Mode changed: COMMAND EDIT");
        }
    }

    public void ResumeLiveMonitorMode(bool syncImmediately = true)
    {
        currentMode = JointPanelMode.LiveMonitor;

        if (runtimeSyncManager != null)
        {
            runtimeSyncManager.SetRuntimeApplyEnabled(true);

            if (syncImmediately &&
                runtimeSyncManager.InputMode == scr_FR5RuntimeSyncManager.RuntimeInputMode.LiveRuntimeSource)
            {
                runtimeSyncManager.SyncLiveOnce();
            }
        }

        CaptureActualFromRobotController();
        CopyActualToTarget();
        SyncAllFromActual();

        if (enableDebugLog)
        {
            Debug.Log("[FR5JointPanelUI] Mode changed: LIVE MONITOR");
        }
    }

    public void CancelCommandPreviewAndResumeLive()
    {
        ResumeLiveMonitorMode(syncImmediately: true);
    }

    public void CommitCommandPreviewAndResumeLive()
    {
        // 현재 단계에서는 실제 SDK 전송 없이 미리보기 종료 후 LIVE 복귀만 수행
        ResumeLiveMonitorMode(syncImmediately: true);
    }

    public float[] GetTargetAnglesCopy()
    {
        float[] copy = new float[6];

        for (int i = 0; i < 6; i++)
        {
            copy[i] = targetJointAngles[i];
        }

        return copy;
    }

    public string GetFormattedTargetSummary()
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "[{0:0.0}, {1:0.0}, {2:0.0}, {3:0.0}, {4:0.0}, {5:0.0}]",
            targetJointAngles[0],
            targetJointAngles[1],
            targetJointAngles[2],
            targetJointAngles[3],
            targetJointAngles[4],
            targetJointAngles[5]
        );
    }

    private void CaptureActualFromRobotController()
    {
        if (robotController == null)
        {
            return;
        }

        for (int i = 0; i < 6; i++)
        {
            actualJointAngles[i] = robotController.GetJointAngle(i);
        }
    }

    private void CopyActualToTarget()
    {
        for (int i = 0; i < 6; i++)
        {
            targetJointAngles[i] = actualJointAngles[i];
        }
    }

    private void ApplyPreset(float[] preset)
    {
        for (int i = 0; i < 6; i++)
        {
            float presetValue = 0f;

            if (preset != null && i < preset.Length)
            {
                presetValue = preset[i];
            }

            targetJointAngles[i] = Mathf.Clamp(
                presetValue,
                robotController.GetJointMinAngle(i),
                robotController.GetJointMaxAngle(i)
            );
        }

        if (previewTargetPoseOnVirtualRobot)
        {
            ApplyTargetPoseToVirtualRobot();
        }

        SyncAllFromTarget();
    }

    private void ApplyZeroPreset()
    {
        for (int i = 0; i < 6; i++)
        {
            targetJointAngles[i] = Mathf.Clamp(
                0f,
                robotController.GetJointMinAngle(i),
                robotController.GetJointMaxAngle(i)
            );
        }

        if (previewTargetPoseOnVirtualRobot)
        {
            ApplyTargetPoseToVirtualRobot();
        }

        SyncAllFromTarget();
    }

    private void SetTargetAngle(int index, float value, bool previewImmediately)
    {
        if (index < 0 || index >= 6 || robotController == null)
        {
            return;
        }

        targetJointAngles[index] = Mathf.Clamp(
            value,
            robotController.GetJointMinAngle(index),
            robotController.GetJointMaxAngle(index)
        );

        if (previewImmediately && previewTargetPoseOnVirtualRobot)
        {
            ApplyTargetPoseToVirtualRobot();
        }

        UpdateRowVisuals(index, targetJointAngles[index]);
    }

    private void ApplyTargetPoseToVirtualRobot()
    {
        if (robotController == null)
        {
            return;
        }

        for (int i = 0; i < 6; i++)
        {
            bool applyNow = i == 5;
            robotController.SetJointAngleByIndex(i, targetJointAngles[i], applyNow);
        }
    }

    private void SyncAllFromActual()
    {
        isInternalSync = true;

        int rowCount = jointRows != null ? jointRows.Length : 0;
        int count = Mathf.Min(6, rowCount);

        for (int i = 0; i < count; i++)
        {
            UpdateRowVisuals(i, actualJointAngles[i]);
        }

        isInternalSync = false;
    }

    private void SyncAllFromTarget()
    {
        isInternalSync = true;

        int rowCount = jointRows != null ? jointRows.Length : 0;
        int count = Mathf.Min(6, rowCount);

        for (int i = 0; i < count; i++)
        {
            UpdateRowVisuals(i, targetJointAngles[i]);
        }

        isInternalSync = false;
    }

    private void UpdateRowVisuals(int index, float angle)
    {
        if (jointRows == null || index < 0 || index >= jointRows.Length)
        {
            return;
        }

        JointRowUIBinding row = jointRows[index];
        if (row == null)
        {
            return;
        }

        if (row.slider != null && !Mathf.Approximately(row.slider.value, angle))
        {
            row.slider.SetValueWithoutNotify(angle);
        }

        if (row.valueText != null)
        {
            row.valueText.text = $"{angle:0.0}°";
        }

        if (row.inputField != null && !row.inputField.isFocused)
        {
            row.inputField.SetTextWithoutNotify(angle.ToString(inputFormat, CultureInfo.InvariantCulture));
        }
    }
}
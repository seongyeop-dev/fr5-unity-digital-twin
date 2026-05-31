using System.Collections;
using UnityEngine;

/// <summary>
/// FR5 Robot Manual Controller
///
/// 역할:
/// 1. Joint 상태 보관
/// 2. ShadowPoseApplier 호출
/// 3. Unity TCP / FK TCP / Python TCP 상태 계산
/// 4. Home 기준 상대 위치 계산
/// 5. 외부 UI 스크립트가 참조할 상태 제공
///
/// 중요:
/// - UI(Text / Slider / Button)는 이 스크립트에서 직접 처리하지 않는다
/// - UI는 별도 Panel UI 스크립트가 담당한다
/// </summary>
public class scr_FR5RobotManualController : MonoBehaviour
{
    [System.Serializable]
    public class JointBinding
    {
        [Header("Joint Info")]
        public string jointName = "J";

        [Header("Shadow Joint Transform")]
        public Transform jointTransform;

        [Header("Rotation Axis")]
        public Vector3 rotationAxis = Vector3.forward;

        [Header("Angle Limit")]
        public float minAngle = -180f;
        public float maxAngle = 180f;

        [Header("Runtime")]
        public float currentAngle = 0f;
    }

    // ================================
    // Runtime Pose Data
    // ================================

    private Vector3 currentUnityTCPLocal = Vector3.zero;
    private Vector3 currentUnityTCPRotationLocal = Vector3.zero;

    private Vector3 currentFKTCPLocal = Vector3.zero;
    private Vector3 currentFKTCPRotationLocal = Vector3.zero;

    private Vector3 currentPythonTCPLocal = Vector3.zero;
    private Vector3 currentPythonTCPRotationLocal = Vector3.zero;

    private Vector3 currentTCPErrorUnityVsFKLocal = Vector3.zero;
    private Vector3 currentTCPErrorUnityVsPythonLocal = Vector3.zero;

    private Vector3 homeUnityTCPLocal = Vector3.zero;
    private Vector3 currentRelativeDeltaLocal = Vector3.zero;

    private bool homeReferenceCaptured = false;
    private bool pythonTCPLoaded = false;

    // ================================
    // Runtime State Labels
    // ================================

    private string currentModeLabel = "Manual";
    private string currentSpeedLabel = "100%";
    private string currentStateLabel = "Ready";
    private string currentAlarmLabel = "No Alarm";

    // ================================
    // Inspector References
    // ================================

    [Header("Base / TCP")]
    [SerializeField] private Transform baseTransform;
    [SerializeField] private Transform tcp;

    [Header("Joint Bindings")]
    [SerializeField] private JointBinding[] joints;

    [Header("Kinematics")]
    [SerializeField] private scr_FR5Kinematics kinematics;

    [Header("Shadow Pose Applier")]
    [SerializeField] private scr_FR5ShadowPoseApplier shadowPoseApplier;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = false;

    [Header("Auto Python Validation")]
    [SerializeField] private scr_PythonAutoRunner pythonAutoRunner;
    [SerializeField] private bool autoRunPythonValidationOnManualInput = true;
    [SerializeField] private float autoValidationDelaySeconds = 0.2f;

    private Coroutine autoValidationCoroutine;

    // ================================
    // Public Getter API
    // ================================

    public Vector3 GetCurrentUnityTCPLocalPosition()
    {
        return currentUnityTCPLocal;
    }

    public Vector3 GetCurrentUnityTCPLocalRotation()
    {
        return currentUnityTCPRotationLocal;
    }

    public Vector3 GetCurrentFKTCPLocalPosition()
    {
        return currentFKTCPLocal;
    }

    public Vector3 GetCurrentFKTCPLocalRotation()
    {
        return currentFKTCPRotationLocal;
    }

    public Vector3 GetCurrentPythonTCPLocalPosition()
    {
        return currentPythonTCPLocal;
    }

    public Vector3 GetCurrentPythonTCPLocalRotation()
    {
        return currentPythonTCPRotationLocal;
    }

    public Vector3 GetCurrentTCPErrorUnityVsFKLocal()
    {
        return currentTCPErrorUnityVsFKLocal;
    }

    public Vector3 GetCurrentTCPErrorUnityVsPythonLocal()
    {
        return currentTCPErrorUnityVsPythonLocal;
    }

    public Vector3 GetCurrentRelativeDeltaLocal()
    {
        return currentRelativeDeltaLocal;
    }

    public bool IsPythonTCPLoaded()
    {
        return pythonTCPLoaded;
    }

    public string GetCurrentModeLabel()
    {
        return currentModeLabel;
    }

    public string GetCurrentSpeedLabel()
    {
        return currentSpeedLabel;
    }

    public string GetCurrentStateLabel()
    {
        return currentStateLabel;
    }

    public string GetCurrentAlarmLabel()
    {
        return currentAlarmLabel;
    }

    public int GetJointCount()
    {
        return joints != null ? joints.Length : 0;
    }

    public string GetJointName(int index)
    {
        if (!IsValidJointIndex(index))
        {
            return $"J{index + 1}";
        }

        return string.IsNullOrWhiteSpace(joints[index].jointName) ? $"J{index + 1}" : joints[index].jointName;
    }

    public float GetJointAngle(int index)
    {
        if (!IsValidJointIndex(index))
        {
            return 0f;
        }

        return joints[index].currentAngle;
    }

    public float GetJointMinAngle(int index)
    {
        if (!IsValidJointIndex(index))
        {
            return -180f;
        }

        return joints[index].minAngle;
    }

    public float GetJointMaxAngle(int index)
    {
        if (!IsValidJointIndex(index))
        {
            return 180f;
        }

        return joints[index].maxAngle;
    }

    public float[] GetCurrentJointArray()
    {
        return new float[]
        {
            GetJointAngle(0),
            GetJointAngle(1),
            GetJointAngle(2),
            GetJointAngle(3),
            GetJointAngle(4),
            GetJointAngle(5)
        };
    }

    public void ForceRefreshStatus()
    {
        RefreshAllStatus();
    }

    // ================================
    // Unity Lifecycle
    // ================================

    private void Start()
    {
        ClampAllJointAngles();
        ApplyShadowPose();
        RefreshAllStatus();
        CaptureHomeReference();
        SetReadyState();
    }

    // ================================
    // Public Command API
    // ================================

    public void SetJointAngleByIndex(int index, float value, bool requestAutoValidation = true)
    {
        if (!IsValidJointIndex(index))
        {
            return;
        }

        JointBinding joint = joints[index];
        joint.currentAngle = Mathf.Clamp(value, joint.minAngle, joint.maxAngle);

        SetJoggingState();
        RefreshAllStatus();

        if (requestAutoValidation)
        {
            RequestAutoPythonValidation();
        }
    }

    public void AdjustJointAngleByIndex(int index, float deltaDegrees, bool requestAutoValidation = true)
    {
        if (!IsValidJointIndex(index))
        {
            return;
        }

        float nextAngle = joints[index].currentAngle + deltaDegrees;
        SetJointAngleByIndex(index, nextAngle, requestAutoValidation);
    }

    public void ApplyJointAnglesFromTest(float j1, float j2, float j3, float j4, float j5, float j6)
    {
        float[] values = new float[] { j1, j2, j3, j4, j5, j6 };
        ApplyJointAnglesInternal(values, false);
    }

    public void ApplyJointAngles(float j1, float j2, float j3, float j4, float j5, float j6)
    {
        float[] values = new float[] { j1, j2, j3, j4, j5, j6 };
        ApplyJointAnglesInternal(values, false);
    }

    public void ResetAllJoints()
    {
        if (joints == null)
        {
            return;
        }

        for (int i = 0; i < joints.Length; i++)
        {
            if (joints[i] == null)
            {
                continue;
            }

            joints[i].currentAngle = 0f;
        }

        RefreshAllStatus();
        SetReadyState();
    }

    public void MoveToHome()
    {
        ResetAllJoints();
        CaptureHomeReference();
        RefreshAllStatus();
        SetReadyState();
    }

    public void SetPythonTCP(Vector3 position, Vector3 rotation)
    {
        currentPythonTCPLocal = position;
        currentPythonTCPRotationLocal = rotation;
        pythonTCPLoaded = true;
        RefreshAllStatus();
    }

    public void ClearPythonTCP()
    {
        currentPythonTCPLocal = Vector3.zero;
        currentPythonTCPRotationLocal = Vector3.zero;
        pythonTCPLoaded = false;
        RefreshAllStatus();
    }

    // ================================
    // Main Refresh
    // ================================

    private void RefreshAllStatus()
    {
        ApplyShadowPose();
        UpdateTCPStatus();
        UpdateFKStatus();
        UpdatePythonStatus();
        UpdateTCPError();
        UpdateRelativeDelta();
    }

    private void ApplyShadowPose()
    {
        if (shadowPoseApplier == null)
        {
            return;
        }

        shadowPoseApplier.ApplyPose(
            GetJointAngle(0),
            GetJointAngle(1),
            GetJointAngle(2),
            GetJointAngle(3),
            GetJointAngle(4),
            GetJointAngle(5)
        );
    }

    private void UpdateTCPStatus()
    {
        if (tcp == null)
        {
            currentUnityTCPLocal = Vector3.zero;
            currentUnityTCPRotationLocal = Vector3.zero;
            return;
        }

        if (baseTransform != null)
        {
            currentUnityTCPLocal = baseTransform.InverseTransformPoint(tcp.position);

            Quaternion localRot = Quaternion.Inverse(baseTransform.rotation) * tcp.rotation;
            Vector3 localEuler = localRot.eulerAngles;

            currentUnityTCPRotationLocal = new Vector3(
                NormalizeAngle(localEuler.x),
                NormalizeAngle(localEuler.y),
                NormalizeAngle(localEuler.z)
            );
        }
        else
        {
            currentUnityTCPLocal = tcp.position;

            Vector3 worldEuler = tcp.eulerAngles;
            currentUnityTCPRotationLocal = new Vector3(
                NormalizeAngle(worldEuler.x),
                NormalizeAngle(worldEuler.y),
                NormalizeAngle(worldEuler.z)
            );
        }
    }

    private void UpdateFKStatus()
    {
        RobotPoseResult fk = GetFKResult();

        if (fk == null)
        {
            currentFKTCPLocal = Vector3.zero;
            currentFKTCPRotationLocal = Vector3.zero;
            return;
        }

        currentFKTCPLocal = fk.position;
        currentFKTCPRotationLocal = fk.rotationEuler;
    }

    private void UpdatePythonStatus()
    {
        if (!pythonTCPLoaded)
        {
            currentPythonTCPLocal = Vector3.zero;
            currentPythonTCPRotationLocal = Vector3.zero;
        }
    }

    private void UpdateTCPError()
    {
        currentTCPErrorUnityVsFKLocal = currentUnityTCPLocal - currentFKTCPLocal;

        if (!pythonTCPLoaded)
        {
            currentTCPErrorUnityVsPythonLocal = Vector3.zero;
            return;
        }

        currentTCPErrorUnityVsPythonLocal = currentUnityTCPLocal - currentPythonTCPLocal;
    }

    private void UpdateRelativeDelta()
    {
        if (!homeReferenceCaptured)
        {
            currentRelativeDeltaLocal = Vector3.zero;
            return;
        }

        currentRelativeDeltaLocal = currentUnityTCPLocal - homeUnityTCPLocal;
    }

    // ================================
    // Internal Helpers
    // ================================

    private void ApplyJointAnglesInternal(float[] values, bool requestAutoValidation)
    {
        if (joints == null || joints.Length < 6)
        {
            Debug.LogError("[FR5RobotManualController] Joint array not properly assigned.");
            return;
        }

        for (int i = 0; i < 6; i++)
        {
            if (joints[i] == null)
            {
                continue;
            }

            joints[i].currentAngle = Mathf.Clamp(values[i], joints[i].minAngle, joints[i].maxAngle);
        }

        RefreshAllStatus();
        SetReadyState();

        if (requestAutoValidation)
        {
            RequestAutoPythonValidation();
        }
    }

    private void ClampAllJointAngles()
    {
        if (joints == null)
        {
            return;
        }

        for (int i = 0; i < joints.Length; i++)
        {
            if (joints[i] == null)
            {
                continue;
            }

            joints[i].currentAngle = Mathf.Clamp(joints[i].currentAngle, joints[i].minAngle, joints[i].maxAngle);
        }
    }

    private void CaptureHomeReference()
    {
        UpdateTCPStatus();
        homeUnityTCPLocal = currentUnityTCPLocal;
        homeReferenceCaptured = true;
    }

    private void SetReadyState()
    {
        currentModeLabel = "Manual";
        currentSpeedLabel = "100%";
        currentStateLabel = "Ready";
        currentAlarmLabel = "No Alarm";
    }

    private void SetJoggingState()
    {
        currentModeLabel = "Manual";
        currentSpeedLabel = "100%";
        currentStateLabel = "Jogging";
        currentAlarmLabel = "No Alarm";
    }

    private RobotPoseResult GetFKResult()
    {
        if (kinematics == null)
        {
            if (enableDebugLog)
            {
                Debug.LogError("[FR5RobotManualController] Kinematics not assigned.");
            }

            return null;
        }

        return kinematics.Calculate(
            GetJointAngle(0),
            GetJointAngle(1),
            GetJointAngle(2),
            GetJointAngle(3),
            GetJointAngle(4),
            GetJointAngle(5)
        );
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    private bool IsValidJointIndex(int index)
    {
        return joints != null && index >= 0 && index < joints.Length && joints[index] != null;
    }

    private void RequestAutoPythonValidation()
    {
        if (!autoRunPythonValidationOnManualInput)
        {
            return;
        }

        if (pythonAutoRunner == null)
        {
            return;
        }

        if (autoValidationCoroutine != null)
        {
            StopCoroutine(autoValidationCoroutine);
        }

        autoValidationCoroutine = StartCoroutine(CoAutoRunPythonValidation());
    }

    private IEnumerator CoAutoRunPythonValidation()
    {
        yield return new WaitForSeconds(autoValidationDelaySeconds);

        pythonAutoRunner.RunLiveValidation();
        autoValidationCoroutine = null;
    }
}
using UnityEngine;

/// <summary>
/// FR5 가상 로봇 관절 컨트롤러
/// 
/// 역할:
/// 1. J1~J6 Root 회전 적용
/// 2. 관절 각도 저장 / 조회
/// 3. UI 매니저에서 호출할 함수 제공
/// 
/// 주의:
/// UI 직접 제어는 scr_FR5JointPanelUI가 담당하고,
/// 이 스크립트는 로봇 자세 계산 / 적용만 담당합니다.
/// </summary>
public class scr_VirtualJointController : MonoBehaviour
{
    public enum LocalAxis
    {
        X,
        Y,
        Z
    }

    [Header("관절 Root 연결")]
    [SerializeField] private Transform j1Root;
    [SerializeField] private Transform j2Root;
    [SerializeField] private Transform j3Root;
    [SerializeField] private Transform j4Root;
    [SerializeField] private Transform j5Root;
    [SerializeField] private Transform j6Root;

    [Header("현재 관절 각도 / Degree")]
    [SerializeField] private float j1Degree;
    [SerializeField] private float j2Degree;
    [SerializeField] private float j3Degree;
    [SerializeField] private float j4Degree;
    [SerializeField] private float j5Degree;
    [SerializeField] private float j6Degree;

    [Header("관절 회전축 설정")]
    [SerializeField] private LocalAxis j1Axis = LocalAxis.Z;
    [SerializeField] private LocalAxis j2Axis = LocalAxis.Y;
    [SerializeField] private LocalAxis j3Axis = LocalAxis.Y;
    [SerializeField] private LocalAxis j4Axis = LocalAxis.Y;
    [SerializeField] private LocalAxis j5Axis = LocalAxis.Z;
    [SerializeField] private LocalAxis j6Axis = LocalAxis.Y;

    [Header("관절 방향 부호")]
    [SerializeField] private float j1Sign = 1f;
    [SerializeField] private float j2Sign = -1f;
    [SerializeField] private float j3Sign = -1f;
    [SerializeField] private float j4Sign = -1f;
    [SerializeField] private float j5Sign = 1f;
    [SerializeField] private float j6Sign = 1f;

    [Header("관절 제한값 / Degree")]
    [SerializeField] private float j1Min = -180f;
    [SerializeField] private float j1Max = 180f;
    [SerializeField] private float j2Min = -180f;
    [SerializeField] private float j2Max = 180f;
    [SerializeField] private float j3Min = -180f;
    [SerializeField] private float j3Max = 180f;
    [SerializeField] private float j4Min = -180f;
    [SerializeField] private float j4Max = 180f;
    [SerializeField] private float j5Min = -180f;
    [SerializeField] private float j5Max = 180f;
    [SerializeField] private float j6Min = -180f;
    [SerializeField] private float j6Max = 180f;

    [Header("실행 옵션")]
    [SerializeField] private bool applyEveryFrame = false;
    [SerializeField] private bool enableDebugLog = false;

    private Quaternion j1InitialRotation;
    private Quaternion j2InitialRotation;
    private Quaternion j3InitialRotation;
    private Quaternion j4InitialRotation;
    private Quaternion j5InitialRotation;
    private Quaternion j6InitialRotation;

    private bool isInitialized;

    private void Awake()
    {
        CacheInitialRotations();
    }

    private void Start()
    {
        ApplyPose();
    }

    private void Update()
    {
        if (!applyEveryFrame)
        {
            return;
        }

        ApplyPose();
    }

    [ContextMenu("초기 회전값 다시 저장")]
    public void CacheInitialRotations()
    {
        j1InitialRotation = GetLocalRotationOrIdentity(j1Root);
        j2InitialRotation = GetLocalRotationOrIdentity(j2Root);
        j3InitialRotation = GetLocalRotationOrIdentity(j3Root);
        j4InitialRotation = GetLocalRotationOrIdentity(j4Root);
        j5InitialRotation = GetLocalRotationOrIdentity(j5Root);
        j6InitialRotation = GetLocalRotationOrIdentity(j6Root);

        isInitialized = true;

        if (enableDebugLog)
        {
            Debug.Log("[VirtualJointController] 초기 회전값 저장 완료");
        }
    }

    [ContextMenu("현재 관절 각도 적용")]
    public void ApplyPose()
    {
        if (!isInitialized)
        {
            CacheInitialRotations();
        }

        ApplySingleJoint(j1Root, j1InitialRotation, j1Degree, j1Axis, j1Sign);
        ApplySingleJoint(j2Root, j2InitialRotation, j2Degree, j2Axis, j2Sign);
        ApplySingleJoint(j3Root, j3InitialRotation, j3Degree, j3Axis, j3Sign);
        ApplySingleJoint(j4Root, j4InitialRotation, j4Degree, j4Axis, j4Sign);
        ApplySingleJoint(j5Root, j5InitialRotation, j5Degree, j5Axis, j5Sign);
        ApplySingleJoint(j6Root, j6InitialRotation, j6Degree, j6Axis, j6Sign);
    }

    [ContextMenu("관절 각도 0으로 초기화")]
    public void ResetAllJoints()
    {
        j1Degree = 0f;
        j2Degree = 0f;
        j3Degree = 0f;
        j4Degree = 0f;
        j5Degree = 0f;
        j6Degree = 0f;

        ApplyPose();

        if (enableDebugLog)
        {
            Debug.Log("[VirtualJointController] 모든 관절 0도 초기화 완료");
        }
    }

    public void MoveToHome()
    {
        ResetAllJoints();
    }

    public string GetJointName(int index)
    {
        switch (index)
        {
            case 0: return "J1";
            case 1: return "J2";
            case 2: return "J3";
            case 3: return "J4";
            case 4: return "J5";
            case 5: return "J6";
            default: return string.Empty;
        }
    }

    public float GetJointAngle(int index)
    {
        switch (index)
        {
            case 0: return j1Degree;
            case 1: return j2Degree;
            case 2: return j3Degree;
            case 3: return j4Degree;
            case 4: return j5Degree;
            case 5: return j6Degree;
            default: return 0f;
        }
    }

    public float GetJointMinAngle(int index)
    {
        switch (index)
        {
            case 0: return j1Min;
            case 1: return j2Min;
            case 2: return j3Min;
            case 3: return j4Min;
            case 4: return j5Min;
            case 5: return j6Min;
            default: return -180f;
        }
    }

    public float GetJointMaxAngle(int index)
    {
        switch (index)
        {
            case 0: return j1Max;
            case 1: return j2Max;
            case 2: return j3Max;
            case 3: return j4Max;
            case 4: return j5Max;
            case 5: return j6Max;
            default: return 180f;
        }
    }

    public void SetJointAngleByIndex(int index, float value, bool applyNow = true)
    {
        float clampedValue = Mathf.Clamp(value, GetJointMinAngle(index), GetJointMaxAngle(index));

        switch (index)
        {
            case 0:
                j1Degree = clampedValue;
                break;
            case 1:
                j2Degree = clampedValue;
                break;
            case 2:
                j3Degree = clampedValue;
                break;
            case 3:
                j4Degree = clampedValue;
                break;
            case 4:
                j5Degree = clampedValue;
                break;
            case 5:
                j6Degree = clampedValue;
                break;
            default:
                return;
        }

        if (applyNow)
        {
            ApplyPose();
        }
    }

    public void AdjustJointAngleByIndex(int index, float deltaDegrees, bool applyNow = true)
    {
        float currentAngle = GetJointAngle(index);
        SetJointAngleByIndex(index, currentAngle + deltaDegrees, applyNow);
    }

    private void ApplySingleJoint(
        Transform jointRoot,
        Quaternion initialRotation,
        float degree,
        LocalAxis axis,
        float sign)
    {
        if (jointRoot == null)
        {
            return;
        }

        Vector3 axisVector = ConvertAxis(axis);
        Quaternion jointRotation = Quaternion.AngleAxis(degree * sign, axisVector);
        jointRoot.localRotation = initialRotation * jointRotation;
    }

    private Vector3 ConvertAxis(LocalAxis axis)
    {
        switch (axis)
        {
            case LocalAxis.X:
                return Vector3.right;
            case LocalAxis.Y:
                return Vector3.up;
            case LocalAxis.Z:
                return Vector3.forward;
            default:
                return Vector3.forward;
        }
    }

    private Quaternion GetLocalRotationOrIdentity(Transform target)
    {
        if (target == null)
        {
            return Quaternion.identity;
        }

        return target.localRotation;
    }
}
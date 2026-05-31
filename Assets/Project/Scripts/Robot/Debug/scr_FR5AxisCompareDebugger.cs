using UnityEngine;

/// <summary>
/// FR5 Axis Compare Debugger
///
/// 역할:
/// 1. 현재 Joint 각도를 기준으로 Unity FK를 다시 계산
/// 2. FK 기준 Joint/TCP axis 출력
/// 3. Shadow Transform 기준 Joint/TCP axis 출력
/// 4. Python 출력과 1:1 비교 가능한 로그를 만든다
///
/// 원칙:
/// - FK 계산만 확인
/// - 좌표계 변환 추가 금지
/// - Shadow pose 수정 금지
/// - Console 로그 출력만 수행
/// </summary>
public class scr_FR5AxisCompareDebugger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private scr_FR5RobotManualController fr5Controller;
    [SerializeField] private scr_FR5Kinematics kinematics;

    [Header("Shadow Root")]
    [SerializeField] private Transform joint1Shadow;
    [SerializeField] private Transform joint2Shadow;
    [SerializeField] private Transform joint3Shadow;
    [SerializeField] private Transform joint4Shadow;
    [SerializeField] private Transform joint5Shadow;
    [SerializeField] private Transform joint6Shadow;
    [SerializeField] private Transform tcpShadow;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    [ContextMenu("Debug Compare Current Pose Axis")]
    public void DebugCompareCurrentPoseAxis()
    {
        if (!ValidateReferences())
        {
            return;
        }

        float[] joints = fr5Controller.GetCurrentJointArray();

        if (joints == null || joints.Length != 6)
        {
            Debug.LogError("[FR5AxisCompareDebugger] Invalid joint array.");
            return;
        }

        RobotPoseResult fk = kinematics.Calculate(
            joints[0],
            joints[1],
            joints[2],
            joints[3],
            joints[4],
            joints[5]
        );

        if (fk == null || fk.jointPositions == null || fk.jointRotations == null)
        {
            Debug.LogError("[FR5AxisCompareDebugger] FK result is invalid.");
            return;
        }

        Debug.Log("====================================================================");
        Debug.Log("[FR5AxisCompareDebugger] CURRENT JOINT INPUT");
        Debug.Log(
            $"Joints = [{joints[0]:F1}, {joints[1]:F1}, {joints[2]:F1}, {joints[3]:F1}, {joints[4]:F1}, {joints[5]:F1}]"
        );
        Debug.Log("====================================================================");

        PrintFKAxis("FK_J1", fk.jointRotations[0]);
        PrintFKAxis("FK_J2", fk.jointRotations[1]);
        PrintFKAxis("FK_J3", fk.jointRotations[2]);
        PrintFKAxis("FK_J4", fk.jointRotations[3]);
        PrintFKAxis("FK_J5", fk.jointRotations[4]);
        PrintFKAxis("FK_J6", fk.jointRotations[5]);
        PrintFKAxis("FK_TCP", fk.rotationEuler);

        Debug.Log("--------------------------------------------------------------------");

        PrintTransformAxis("SHADOW_J1", joint1Shadow);
        PrintTransformAxis("SHADOW_J2", joint2Shadow);
        PrintTransformAxis("SHADOW_J3", joint3Shadow);
        PrintTransformAxis("SHADOW_J4", joint4Shadow);
        PrintTransformAxis("SHADOW_J5", joint5Shadow);
        PrintTransformAxis("SHADOW_J6", joint6Shadow);
        PrintTransformAxis("SHADOW_TCP", tcpShadow);

        Debug.Log("====================================================================");
    }

    private void PrintFKAxis(string label, Vector3 euler)
    {
        Quaternion q = Quaternion.Euler(euler);

        Vector3 right = q * Vector3.right;
        Vector3 up = q * Vector3.up;
        Vector3 forward = q * Vector3.forward;

        Debug.Log(
            $"[{label} AXIS]\n" +
            $"RIGHT   = {FormatVector(right)}\n" +
            $"UP      = {FormatVector(up)}\n" +
            $"FORWARD = {FormatVector(forward)}"
        );
    }

    private void PrintTransformAxis(string label, Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning($"[{label}] Target is null.");
            return;
        }

        Vector3 right = target.right;
        Vector3 up = target.up;
        Vector3 forward = target.forward;

        Debug.Log(
            $"[{label} AXIS]\n" +
            $"RIGHT   = {FormatVector(right)}\n" +
            $"UP      = {FormatVector(up)}\n" +
            $"FORWARD = {FormatVector(forward)}"
        );
    }

    private string FormatVector(Vector3 v)
    {
        return $"[{v.x:F6}, {v.y:F6}, {v.z:F6}]";
    }

    private bool ValidateReferences()
    {
        if (fr5Controller == null)
        {
            Debug.LogError("[FR5AxisCompareDebugger] FR5 Controller is not assigned.");
            return false;
        }

        if (kinematics == null)
        {
            Debug.LogError("[FR5AxisCompareDebugger] Kinematics is not assigned.");
            return false;
        }

        if (joint1Shadow == null ||
            joint2Shadow == null ||
            joint3Shadow == null ||
            joint4Shadow == null ||
            joint5Shadow == null ||
            joint6Shadow == null ||
            tcpShadow == null)
        {
            Debug.LogError("[FR5AxisCompareDebugger] One or more shadow transforms are missing.");
            return false;
        }

        return true;
    }
}
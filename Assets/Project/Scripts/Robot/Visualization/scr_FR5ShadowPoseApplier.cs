using UnityEngine;

/// <summary>
/// FR5 Shadow Pose Applier
///
/// 역할:
/// - 최신 FR5Kinematics의 구조 노드 결과를 ShadowRoot에 반영
/// - Python 구조와 동일한 Shadow 구조를 만든다
/// </summary>
public class scr_FR5ShadowPoseApplier : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private scr_FR5Kinematics kinematics;

    [Header("Shadow Root - Structure Chain")]
    [SerializeField] private Transform baseShadow;
    [SerializeField] private Transform joint1Shadow;
    [SerializeField] private Transform d1Shadow;
    [SerializeField] private Transform alpha1Shadow;
    [SerializeField] private Transform joint2Shadow;
    [SerializeField] private Transform a2Shadow;
    [SerializeField] private Transform joint3Shadow;
    [SerializeField] private Transform a3Shadow;
    [SerializeField] private Transform joint4Shadow;
    [SerializeField] private Transform d4Shadow;
    [SerializeField] private Transform alpha4Shadow;
    [SerializeField] private Transform joint5Shadow;
    [SerializeField] private Transform d5Shadow;
    [SerializeField] private Transform alpha5Shadow;
    [SerializeField] private Transform joint6Shadow;
    [SerializeField] private Transform d6Shadow;
    [SerializeField] private Transform tcpShadow;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = false;

    public void ApplyPose(
        float j1,
        float j2,
        float j3,
        float j4,
        float j5,
        float j6)
    {
        if (!ValidateReferences())
        {
            return;
        }

        RobotPoseResult fk = kinematics.Calculate(j1, j2, j3, j4, j5, j6);

        if (fk == null || fk.structurePositions == null || fk.structureRotations == null)
        {
            Debug.LogError("[FR5ShadowPoseApplier] FK structure result is invalid.");
            return;
        }

        ApplyNode(baseShadow, fk, "BASE");
        ApplyNode(joint1Shadow, fk, "JOINT1");
        ApplyNode(d1Shadow, fk, "D1");
        ApplyNode(alpha1Shadow, fk, "ALPHA1");
        ApplyNode(joint2Shadow, fk, "JOINT2");
        ApplyNode(a2Shadow, fk, "A2");
        ApplyNode(joint3Shadow, fk, "JOINT3");
        ApplyNode(a3Shadow, fk, "A3");
        ApplyNode(joint4Shadow, fk, "JOINT4");
        ApplyNode(d4Shadow, fk, "D4");
        ApplyNode(alpha4Shadow, fk, "ALPHA4");
        ApplyNode(joint5Shadow, fk, "JOINT5");
        ApplyNode(d5Shadow, fk, "D5");
        ApplyNode(alpha5Shadow, fk, "ALPHA5");
        ApplyNode(joint6Shadow, fk, "JOINT6");
        ApplyNode(d6Shadow, fk, "D6");
        ApplyNode(tcpShadow, fk, "TCP");

        if (verboseLog)
        {
            Debug.Log($"[FR5ShadowPoseApplier] Applied structure shadow pose | TCP={fk.position}");
        }
    }

    private void ApplyNode(Transform target, RobotPoseResult fk, string nodeName)
    {
        if (target == null)
        {
            return;
        }

        int index = FindStructureIndex(fk.structureNames, nodeName);
        if (index < 0)
        {
            Debug.LogError($"[FR5ShadowPoseApplier] Structure node not found: {nodeName}");
            return;
        }

        target.localPosition = fk.structurePositions[index];
        target.localRotation = Quaternion.Euler(fk.structureRotations[index]);
    }

    private int FindStructureIndex(string[] names, string targetName)
    {
        if (names == null)
        {
            return -1;
        }

        for (int i = 0; i < names.Length; i++)
        {
            if (names[i] == targetName)
            {
                return i;
            }
        }

        return -1;
    }

    private bool ValidateReferences()
    {
        if (kinematics == null)
        {
            Debug.LogError("[FR5ShadowPoseApplier] Kinematics missing.");
            return false;
        }

        if (baseShadow == null ||
            joint1Shadow == null ||
            d1Shadow == null ||
            alpha1Shadow == null ||
            joint2Shadow == null ||
            a2Shadow == null ||
            joint3Shadow == null ||
            a3Shadow == null ||
            joint4Shadow == null ||
            d4Shadow == null ||
            alpha4Shadow == null ||
            joint5Shadow == null ||
            d5Shadow == null ||
            alpha5Shadow == null ||
            joint6Shadow == null ||
            d6Shadow == null ||
            tcpShadow == null)
        {
            Debug.LogError("[FR5ShadowPoseApplier] Shadow structure references missing.");
            return false;
        }

        return true;
    }
}
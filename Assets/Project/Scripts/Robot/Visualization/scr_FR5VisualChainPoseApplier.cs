using UnityEngine;

/// <summary>
/// FR5 Visual Chain Pose Applier
///
/// 역할:
/// - FR5_ShadowRoot의 절대 pose를 읽는다
/// - 각 joint의 부모 기준 local pose를 계산한다
/// - FR5_VisualRoot Anchor 체인에 local transform으로 반영한다
///
/// 원칙:
/// - FK 계산하지 않음
/// - ShadowRoot 수정하지 않음
/// - DebugShadowRoot 사용하지 않음
/// - Visual Anchor 체인만 갱신함
/// </summary>
public class scr_FR5VisualChainPoseApplier : MonoBehaviour
{
    [Header("Source Shadow Root")]
    [SerializeField] private Transform baseShadow;
    [SerializeField] private Transform joint1Shadow;
    [SerializeField] private Transform joint2Shadow;
    [SerializeField] private Transform joint3Shadow;
    [SerializeField] private Transform joint4Shadow;
    [SerializeField] private Transform joint5Shadow;
    [SerializeField] private Transform joint6Shadow;
    [SerializeField] private Transform tcpShadow;

    [Header("Target Visual Anchor Chain")]
    [SerializeField] private Transform visualBaseAnchor;
    [SerializeField] private Transform visualJ1Anchor;
    [SerializeField] private Transform visualJ2Anchor;
    [SerializeField] private Transform visualJ3Anchor;
    [SerializeField] private Transform visualJ4Anchor;
    [SerializeField] private Transform visualJ5Anchor;
    [SerializeField] private Transform visualJ6Anchor;
    [SerializeField] private Transform visualTcpAnchor;

    [Header("Options")]
    [SerializeField] private bool updatePosition = true;
    [SerializeField] private bool updateRotation = true;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = false;

    private void LateUpdate()
    {
        if (!ValidateReferences())
        {
            return;
        }

        // Base Anchor는 체인 기준점으로만 사용
        // world pose를 덮어쓰지 않고 원점/무회전으로 고정
        if (updatePosition)
        {
            visualBaseAnchor.localPosition = Vector3.zero;
        }

        if (updateRotation)
        {
            visualBaseAnchor.localRotation = Quaternion.identity;
        }

        // J1부터 부모 기준 local pose로 반영
        ApplyRelativePose(baseShadow, joint1Shadow, visualJ1Anchor);
        ApplyRelativePose(joint1Shadow, joint2Shadow, visualJ2Anchor);
        ApplyRelativePose(joint2Shadow, joint3Shadow, visualJ3Anchor);
        ApplyRelativePose(joint3Shadow, joint4Shadow, visualJ4Anchor);
        ApplyRelativePose(joint4Shadow, joint5Shadow, visualJ5Anchor);
        ApplyRelativePose(joint5Shadow, joint6Shadow, visualJ6Anchor);
        ApplyRelativePose(joint6Shadow, tcpShadow, visualTcpAnchor);

        if (verboseLog)
        {
            Debug.Log("[FR5VisualChainPoseApplier] Visual chain pose applied.");
        }
    }

    /// <summary>
    /// Base Anchor는 Shadow world pose 그대로 반영
    /// </summary>
    private void ApplyWorldPose(Transform source, Transform target)
    {
        if (source == null || target == null)
        {
            return;
        }

        if (updatePosition)
        {
            target.position = source.position;
        }

        if (updateRotation)
        {
            target.rotation = source.rotation;
        }
    }

    /// <summary>
    /// parentSource -> childSource 관계를 local pose로 변환해서
    /// target Anchor에 반영
    /// </summary>
    private void ApplyRelativePose(Transform parentSource, Transform childSource, Transform target)
    {
        if (parentSource == null || childSource == null || target == null)
        {
            return;
        }

        Vector3 localPosition = parentSource.InverseTransformPoint(childSource.position);
        Quaternion localRotation = Quaternion.Inverse(parentSource.rotation) * childSource.rotation;

        if (updatePosition)
        {
            target.localPosition = localPosition;
        }

        if (updateRotation)
        {
            target.localRotation = localRotation;
        }
    }

    private bool ValidateReferences()
    {
        if (baseShadow == null ||
            joint1Shadow == null ||
            joint2Shadow == null ||
            joint3Shadow == null ||
            joint4Shadow == null ||
            joint5Shadow == null ||
            joint6Shadow == null ||
            tcpShadow == null)
        {
            Debug.LogError("[FR5VisualChainPoseApplier] One or more source shadow transforms are missing.");
            return false;
        }

        if (visualBaseAnchor == null ||
            visualJ1Anchor == null ||
            visualJ2Anchor == null ||
            visualJ3Anchor == null ||
            visualJ4Anchor == null ||
            visualJ5Anchor == null ||
            visualJ6Anchor == null ||
            visualTcpAnchor == null)
        {
            Debug.LogError("[FR5VisualChainPoseApplier] One or more visual anchor transforms are missing.");
            return false;
        }

        return true;
    }
}
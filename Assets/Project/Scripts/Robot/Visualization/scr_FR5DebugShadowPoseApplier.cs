using UnityEngine;

/// <summary>
/// FR5 Debug Shadow Pose Applier
///
/// 역할:
/// - ShadowRoot 구조 노드 전체를 DebugShadowRoot로 복사
/// - 보기 편한 display offset만 추가
/// </summary>
public class scr_FR5DebugShadowPoseApplier : MonoBehaviour
{
    [Header("Source Shadow Root")]
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

    [Header("Target Debug Shadow Root")]
    [SerializeField] private Transform baseDebugShadow;
    [SerializeField] private Transform joint1DebugShadow;
    [SerializeField] private Transform d1DebugShadow;
    [SerializeField] private Transform alpha1DebugShadow;
    [SerializeField] private Transform joint2DebugShadow;
    [SerializeField] private Transform a2DebugShadow;
    [SerializeField] private Transform joint3DebugShadow;
    [SerializeField] private Transform a3DebugShadow;
    [SerializeField] private Transform joint4DebugShadow;
    [SerializeField] private Transform d4DebugShadow;
    [SerializeField] private Transform alpha4DebugShadow;
    [SerializeField] private Transform joint5DebugShadow;
    [SerializeField] private Transform d5DebugShadow;
    [SerializeField] private Transform alpha5DebugShadow;
    [SerializeField] private Transform joint6DebugShadow;
    [SerializeField] private Transform d6DebugShadow;
    [SerializeField] private Transform tcpDebugShadow;

    [Header("Display Offset")]
    [SerializeField] private Vector3 displayOffset = new Vector3(0f, 0.35f, 0f);

    private void LateUpdate()
    {
        CopyPose(baseShadow, baseDebugShadow);
        CopyPose(joint1Shadow, joint1DebugShadow);
        CopyPose(d1Shadow, d1DebugShadow);
        CopyPose(alpha1Shadow, alpha1DebugShadow);
        CopyPose(joint2Shadow, joint2DebugShadow);
        CopyPose(a2Shadow, a2DebugShadow);
        CopyPose(joint3Shadow, joint3DebugShadow);
        CopyPose(a3Shadow, a3DebugShadow);
        CopyPose(joint4Shadow, joint4DebugShadow);
        CopyPose(d4Shadow, d4DebugShadow);
        CopyPose(alpha4Shadow, alpha4DebugShadow);
        CopyPose(joint5Shadow, joint5DebugShadow);
        CopyPose(d5Shadow, d5DebugShadow);
        CopyPose(alpha5Shadow, alpha5DebugShadow);
        CopyPose(joint6Shadow, joint6DebugShadow);
        CopyPose(d6Shadow, d6DebugShadow);
        CopyPose(tcpShadow, tcpDebugShadow);
    }

    private void CopyPose(Transform source, Transform target)
    {
        if (source == null || target == null)
        {
            return;
        }

        target.position = source.position + displayOffset;
        target.rotation = source.rotation;
    }
}
using UnityEngine;

/// <summary>
/// FR5 Visual Pose Follower
///
/// 역할:
/// - DebugShadow pose를 읽는다
/// - Visual Anchor에 위치/회전을 복사한다
/// - Mesh는 Anchor 밑에 붙어서 화면에 보이게 된다
///
/// 원칙:
/// - FK 계산하지 않음
/// - ShadowRoot 수정하지 않음
/// - Visual Anchor만 갱신함
/// </summary>
public class scr_FR5VisualPoseFollower : MonoBehaviour
{
    [Header("Source Debug Shadow")]
    [SerializeField] private Transform baseDebugShadow;
    [SerializeField] private Transform joint1DebugShadow;
    [SerializeField] private Transform joint2DebugShadow;
    [SerializeField] private Transform joint3DebugShadow;
    [SerializeField] private Transform joint4DebugShadow;
    [SerializeField] private Transform joint5DebugShadow;
    [SerializeField] private Transform joint6DebugShadow;
    [SerializeField] private Transform tcpDebugShadow;

    [Header("Target Visual Anchors")]
    [SerializeField] private Transform visualBaseAnchor;
    [SerializeField] private Transform visualJ1Anchor;
    [SerializeField] private Transform visualJ2Anchor;
    [SerializeField] private Transform visualJ3Anchor;
    [SerializeField] private Transform visualJ4Anchor;
    [SerializeField] private Transform visualJ5Anchor;
    [SerializeField] private Transform visualJ6Anchor;
    [SerializeField] private Transform visualTcpAnchor;

    [Header("Options")]
    [SerializeField] private bool followPosition = true;
    [SerializeField] private bool followRotation = true;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = false;

    private void LateUpdate()
    {
        FollowPose(baseDebugShadow, visualBaseAnchor);
        FollowPose(joint1DebugShadow, visualJ1Anchor);
        FollowPose(joint2DebugShadow, visualJ2Anchor);
        FollowPose(joint3DebugShadow, visualJ3Anchor);
        FollowPose(joint4DebugShadow, visualJ4Anchor);
        FollowPose(joint5DebugShadow, visualJ5Anchor);
        FollowPose(joint6DebugShadow, visualJ6Anchor);
        FollowPose(tcpDebugShadow, visualTcpAnchor);
    }

    private void FollowPose(Transform source, Transform target)
    {
        if (source == null || target == null)
        {
            return;
        }

        if (followPosition)
        {
            target.position = source.position;
        }

        if (followRotation)
        {
            target.rotation = source.rotation;
        }
    }
}
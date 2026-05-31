using UnityEngine;

/// <summary>
/// 공통 Kinematics 인터페이스
/// </summary>
public abstract class scr_BaseKinematics : MonoBehaviour
{
    public abstract RobotPoseResult Calculate(
        float j1,
        float j2,
        float j3,
        float j4,
        float j5,
        float j6
    );
}
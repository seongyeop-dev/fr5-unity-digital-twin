using UnityEngine;

/// <summary>
/// FK 계산 결과 데이터
/// </summary>
[System.Serializable]
public class RobotPoseResult
{
    /// <summary>
    /// 최종 TCP 위치
    /// </summary>
    public Vector3 position;

    /// <summary>
    /// 최종 TCP 회전 Euler (degree)
    /// </summary>
    public Vector3 rotationEuler;

    /// <summary>
    /// 최종 TCP 4x4 변환행렬
    /// </summary>
    public Matrix4x4 transform;

    /// <summary>
    /// Joint 기준 위치
    /// index:
    /// 0 = JOINT1
    /// 1 = JOINT2
    /// 2 = JOINT3
    /// 3 = JOINT4
    /// 4 = JOINT5
    /// 5 = JOINT6
    /// </summary>
    public Vector3[] jointPositions;

    /// <summary>
    /// Joint 기준 회전 Euler
    /// index:
    /// 0 = JOINT1
    /// 1 = JOINT2
    /// 2 = JOINT3
    /// 3 = JOINT4
    /// 4 = JOINT5
    /// 5 = JOINT6
    /// </summary>
    public Vector3[] jointRotations;

    /// <summary>
    /// 구조 노드 이름
    /// BASE, JOINT1, D1, ALPHA1, JOINT2, A2, ...
    /// </summary>
    public string[] structureNames;

    /// <summary>
    /// 구조 노드 위치
    /// structureNames와 같은 index 사용
    /// </summary>
    public Vector3[] structurePositions;

    /// <summary>
    /// 구조 노드 회전 Euler
    /// structureNames와 같은 index 사용
    /// </summary>
    public Vector3[] structureRotations;

    public RobotPoseResult(
        Vector3 position,
        Vector3 rotationEuler,
        Matrix4x4 transform,
        Vector3[] jointPositions,
        Vector3[] jointRotations,
        string[] structureNames,
        Vector3[] structurePositions,
        Vector3[] structureRotations)
    {
        this.position = position;
        this.rotationEuler = rotationEuler;
        this.transform = transform;
        this.jointPositions = jointPositions;
        this.jointRotations = jointRotations;
        this.structureNames = structureNames;
        this.structurePositions = structurePositions;
        this.structureRotations = structureRotations;
    }
}
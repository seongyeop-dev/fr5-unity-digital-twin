using UnityEngine;

/// <summary>
/// FR5 Coordinate Mapper
///
/// 역할:
/// - FAIRINO raw frame(Python / SDK 공통 기준)를 Unity frame으로 변환
/// - 위치 / 방향 / 회전 quaternion / fixed-axis Euler 변환을 한 곳에서 관리
///
/// 현재 기준:
/// Raw X -> Unity Z
/// Raw Y -> Unity -X
/// Raw Z -> Unity Y
///
/// 주의:
/// - 이 매핑은 현재 Python/C# 내부 정합 기준의 provisional mapping이다.
/// - 추후 SDK Flange 단일축 테스트 후 최종 확정한다.
/// </summary>
public static class scr_FR5CoordinateMapper
{
    /// <summary>
    /// Raw frame position(m) -> Unity position(m)
    /// </summary>
    public static Vector3 ConvertFrPositionMetersToUnity(Vector3 frPositionMeters)
    {
        return ConvertFrDirectionToUnity(frPositionMeters);
    }

    /// <summary>
    /// Raw frame position(mm) -> Unity position(m)
    /// </summary>
    public static Vector3 ConvertFrPositionMillimetersToUnityMeters(Vector3 frPositionMillimeters)
    {
        return ConvertFrDirectionToUnity(frPositionMillimeters * 0.001f);
    }

    /// <summary>
    /// Raw frame direction/vector -> Unity direction/vector
    /// translation 없이 축 성분만 변환할 때 사용
    /// </summary>
    public static Vector3 ConvertFrDirectionToUnity(Vector3 frDirection)
    {
        return new Vector3(
            -frDirection.y,
             frDirection.z,
             frDirection.x
        );
    }

    /// <summary>
    /// Raw basis quaternion -> Unity quaternion
    /// Python FK / raw rotation matrix / raw basis axis에 사용
    /// </summary>
    public static Quaternion ConvertFrBasisQuaternionToUnityQuaternion(Quaternion frBasisRotation)
    {
        Quaternion converted = new Quaternion(
            -frBasisRotation.y,
             frBasisRotation.z,
             frBasisRotation.x,
             frBasisRotation.w
        );

        return NormalizeQuaternion(converted);
    }

    /// <summary>
    /// Raw basis quaternion -> Unity Euler(deg)
    /// </summary>
    public static Vector3 ConvertFrBasisQuaternionToUnityEuler(Quaternion frBasisRotation)
    {
        return NormalizeEuler(
            ConvertFrBasisQuaternionToUnityQuaternion(frBasisRotation).eulerAngles
        );
    }

    /// <summary>
    /// SDK rx, ry, rz (fixed-axis X/Y/Z rotation) -> Unity quaternion
    /// 현재는 provisional interpretation으로 사용
    /// 추후 SDK Flange 축 테스트 후 최종 고정
    /// </summary>
    public static Quaternion ConvertFrFixedEulerDegreesToUnityQuaternion(Vector3 frFixedEulerDegrees)
    {
        Quaternion rawRotation = BuildFrFixedEulerQuaternion(frFixedEulerDegrees);
        return ConvertFrBasisQuaternionToUnityQuaternion(rawRotation);
    }

    /// <summary>
    /// SDK rx, ry, rz (fixed-axis X/Y/Z rotation) -> Unity Euler(deg)
    /// </summary>
    public static Vector3 ConvertFrFixedEulerDegreesToUnityEuler(Vector3 frFixedEulerDegrees)
    {
        return NormalizeEuler(
            ConvertFrFixedEulerDegreesToUnityQuaternion(frFixedEulerDegrees).eulerAngles
        );
    }

    /// <summary>
    /// Raw basis vectors -> Unity quaternion
    /// </summary>
    public static Quaternion BuildRawBasisToUnityQuaternion(
        Vector3 rawRight,
        Vector3 rawUp,
        Vector3 rawForward)
    {
        Vector3 unityForward = ConvertFrDirectionToUnity(rawForward).normalized;
        Vector3 unityUp = ConvertFrDirectionToUnity(rawUp).normalized;

        if (unityForward.sqrMagnitude < 1e-8f || unityUp.sqrMagnitude < 1e-8f)
        {
            return Quaternion.identity;
        }

        return Quaternion.LookRotation(unityForward, unityUp);
    }

    public static Vector3 NormalizeEuler(Vector3 euler)
    {
        return new Vector3(
            NormalizeAngle(euler.x),
            NormalizeAngle(euler.y),
            NormalizeAngle(euler.z)
        );
    }

    private static Quaternion BuildFrFixedEulerQuaternion(Vector3 frFixedEulerDegrees)
    {
        Quaternion qx = Quaternion.AngleAxis(frFixedEulerDegrees.x, Vector3.right);
        Quaternion qy = Quaternion.AngleAxis(frFixedEulerDegrees.y, Vector3.up);
        Quaternion qz = Quaternion.AngleAxis(frFixedEulerDegrees.z, Vector3.forward);

        // provisional raw-frame interpretation
        return qz * qy * qx;
    }

    private static Quaternion NormalizeQuaternion(Quaternion q)
    {
        float mag = Mathf.Sqrt(
            (q.x * q.x) +
            (q.y * q.y) +
            (q.z * q.z) +
            (q.w * q.w)
        );

        if (mag < 1e-8f)
        {
            return Quaternion.identity;
        }

        return new Quaternion(
            q.x / mag,
            q.y / mag,
            q.z / mag,
            q.w / mag
        );
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}
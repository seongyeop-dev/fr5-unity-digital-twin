using System;
using UnityEngine;

/// <summary>
/// FR5 SDK / Runtime Pose Sample
///
/// 역할:
/// - SDK 또는 Mock 입력에서 읽은 최신 상태를 담는 공용 데이터 구조
/// - Joint / Flange / TCP / State 정보를 하나로 묶는다
/// - Unity Runtime Sync, Validation, Logging 계층의 공통 입력 형식으로 사용한다
/// </summary>
[Serializable]
public class FR5SdkPoseSample
{
    [Header("Meta")]
    public bool isValid = false;
    public string source = "None";
    public double timestampSeconds = 0.0;

    [Header("Joint (deg)")]
    public float[] jointDegrees = new float[6] { 0f, 0f, 0f, 0f, 0f, 0f };

    [Header("Flange Pose")]
    public Vector3 flangePositionMillimeters = Vector3.zero;
    public Vector3 flangeRotationDegrees = Vector3.zero;

    [Header("TCP Pose")]
    public Vector3 tcpPositionMillimeters = Vector3.zero;
    public Vector3 tcpRotationDegrees = Vector3.zero;

    [Header("Tool / User")]
    public int toolIndex = -1;
    public int userIndex = -1;

    [Header("Robot State")]
    public string robotMode = "Unknown";
    public string robotState = "Unknown";
    public int alarmCode = 0;

    public FR5SdkPoseSample Clone()
    {
        FR5SdkPoseSample clone = new FR5SdkPoseSample();

        clone.isValid = isValid;
        clone.source = source;
        clone.timestampSeconds = timestampSeconds;

        clone.jointDegrees = new float[6];
        for (int i = 0; i < 6; i++)
        {
            clone.jointDegrees[i] = (jointDegrees != null && jointDegrees.Length > i) ? jointDegrees[i] : 0f;
        }

        clone.flangePositionMillimeters = flangePositionMillimeters;
        clone.flangeRotationDegrees = flangeRotationDegrees;
        clone.tcpPositionMillimeters = tcpPositionMillimeters;
        clone.tcpRotationDegrees = tcpRotationDegrees;

        clone.toolIndex = toolIndex;
        clone.userIndex = userIndex;
        clone.robotMode = robotMode;
        clone.robotState = robotState;
        clone.alarmCode = alarmCode;

        return clone;
    }

    public static FR5SdkPoseSample CreateInvalid(string sourceName = "None")
    {
        return new FR5SdkPoseSample
        {
            isValid = false,
            source = sourceName,
            timestampSeconds = 0.0
        };
    }
}
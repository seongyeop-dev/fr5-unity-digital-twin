using System;
using System.IO;
using UnityEngine;

/// <summary>
/// FR5 Python Bridge Client
///
/// 역할:
/// - 외부 Python Bridge가 저장한 JSON 상태 파일을 읽는다
/// - IFR5RuntimePoseSource 형식으로 Unity Runtime에 제공한다
///
/// 장점:
/// - Unity에 제조사 Python SDK를 직접 넣지 않아도 됨
/// - 포트폴리오에서 SIM / LIVE 구조를 쉽게 설명 가능
/// - 추후 ROS2 도입 전 중간 단계로 매우 적합
/// </summary>
public class scr_FR5PythonBridgeClient : MonoBehaviour, IFR5RuntimePoseSource
{
    [Serializable]
    private class FR5BridgeSampleJson
    {
        public bool isValid = false;
        public string source = "PythonBridge";
        public double timestampSeconds = 0.0;

        public float[] jointDegrees = new float[6];
        public float[] flangePositionMillimeters = new float[3];
        public float[] flangeRotationDegrees = new float[3];
        public float[] tcpPositionMillimeters = new float[3];
        public float[] tcpRotationDegrees = new float[3];

        public int toolIndex = -1;
        public int userIndex = -1;
        public string robotMode = "Unknown";
        public string robotState = "Unknown";
        public int alarmCode = 0;
    }

    [Header("Bridge File")]
    [SerializeField] private string bridgeFolderName = "Bridge";
    [SerializeField] private string bridgeFileName = "fr5_live_state.json";
    [SerializeField] private bool autoConnectOnStart = true;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private bool isConnected = false;
    private FR5SdkPoseSample latestSample = FR5SdkPoseSample.CreateInvalid("PythonBridge");

    public bool IsConnected => isConnected;

    private void Start()
    {
        if (autoConnectOnStart)
        {
            Connect();
        }
    }

    public bool Connect()
    {
        isConnected = true;

        if (verboseLog)
        {
            Debug.Log("[FR5PythonBridgeClient] Connected.");
        }

        return true;
    }

    public void Disconnect()
    {
        isConnected = false;
        latestSample = FR5SdkPoseSample.CreateInvalid("PythonBridge");

        if (verboseLog)
        {
            Debug.Log("[FR5PythonBridgeClient] Disconnected.");
        }
    }

    public bool PollLatestState()
    {
        if (!isConnected)
        {
            return false;
        }

        string filePath = Path.Combine(
            Application.streamingAssetsPath,
            bridgeFolderName,
            bridgeFileName
        );

        if (!File.Exists(filePath))
        {
            if (verboseLog)
            {
                Debug.LogWarning($"[FR5PythonBridgeClient] Bridge file not found: {filePath}");
            }
            return false;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            FR5BridgeSampleJson raw = JsonUtility.FromJson<FR5BridgeSampleJson>(json);

            if (raw == null || !raw.isValid)
            {
                return false;
            }

            FR5SdkPoseSample sample = new FR5SdkPoseSample();
            sample.isValid = raw.isValid;
            sample.source = raw.source;
            sample.timestampSeconds = raw.timestampSeconds;

            for (int i = 0; i < 6; i++)
            {
                sample.jointDegrees[i] = (raw.jointDegrees != null && raw.jointDegrees.Length > i)
                    ? raw.jointDegrees[i]
                    : 0f;
            }

            sample.flangePositionMillimeters = ToVector3(raw.flangePositionMillimeters);
            sample.flangeRotationDegrees = ToVector3(raw.flangeRotationDegrees);
            sample.tcpPositionMillimeters = ToVector3(raw.tcpPositionMillimeters);
            sample.tcpRotationDegrees = ToVector3(raw.tcpRotationDegrees);

            sample.toolIndex = raw.toolIndex;
            sample.userIndex = raw.userIndex;
            sample.robotMode = raw.robotMode;
            sample.robotState = raw.robotState;
            sample.alarmCode = raw.alarmCode;

            latestSample = sample;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[FR5PythonBridgeClient] Failed to read bridge JSON: " + ex.Message);
            return false;
        }
    }

    public bool TryGetLatestSample(out FR5SdkPoseSample sample)
    {
        if (latestSample == null)
        {
            sample = FR5SdkPoseSample.CreateInvalid("PythonBridge");
            return false;
        }

        sample = latestSample.Clone();
        return sample.isValid;
    }

    private Vector3 ToVector3(float[] arr)
    {
        if (arr == null || arr.Length < 3)
        {
            return Vector3.zero;
        }

        return new Vector3(arr[0], arr[1], arr[2]);
    }
}
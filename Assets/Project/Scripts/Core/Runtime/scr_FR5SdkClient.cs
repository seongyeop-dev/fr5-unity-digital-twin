using UnityEngine;

/// <summary>
/// FR5 SDK Client
///
/// 역할:
/// - 현재는 Mock SDK 입력 제공
/// - 추후 실제 SDK 연결 클라이언트의 기본형으로 사용 가능
/// - IFR5RuntimePoseSource 인터페이스 구현
/// </summary>
public class scr_FR5SdkClient : MonoBehaviour, IFR5RuntimePoseSource
{
    public enum SdkBackendMode
    {
        Mock,
        PythonBridgePlaceholder,
        CSharpSdkPlaceholder
    }

    [Header("Backend")]
    [SerializeField] private SdkBackendMode backendMode = SdkBackendMode.Mock;
    [SerializeField] private bool autoConnectOnStart = true;

    [Header("Mock Options")]
    [SerializeField] private bool autoUpdateMockSample = true;
    [SerializeField] private bool animateMockJoints = false;
    [SerializeField] private float mockAnimationAmplitudeDeg = 20f;
    [SerializeField] private float mockAnimationSpeed = 1.0f;

    [Header("Mock Joint Values (deg)")]
    [SerializeField] private float mockJ1 = 0f;
    [SerializeField] private float mockJ2 = 0f;
    [SerializeField] private float mockJ3 = 0f;
    [SerializeField] private float mockJ4 = 0f;
    [SerializeField] private float mockJ5 = 0f;
    [SerializeField] private float mockJ6 = 0f;

    [Header("Mock Flange Pose")]
    [SerializeField] private Vector3 mockFlangePositionMillimeters = Vector3.zero;
    [SerializeField] private Vector3 mockFlangeRotationDegrees = Vector3.zero;

    [Header("Mock TCP Pose")]
    [SerializeField] private Vector3 mockTcpPositionMillimeters = Vector3.zero;
    [SerializeField] private Vector3 mockTcpRotationDegrees = Vector3.zero;

    [Header("Mock State")]
    [SerializeField] private int mockToolIndex = 0;
    [SerializeField] private int mockUserIndex = 0;
    [SerializeField] private string mockRobotMode = "Auto";
    [SerializeField] private string mockRobotState = "Ready";
    [SerializeField] private int mockAlarmCode = 0;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private bool isConnected = false;
    private FR5SdkPoseSample latestSample = FR5SdkPoseSample.CreateInvalid("None");

    public bool IsConnected => isConnected;
    public SdkBackendMode BackendMode => backendMode;

    private void Start()
    {
        if (autoConnectOnStart)
        {
            Connect();
        }
    }

    private void Update()
    {
        if (!isConnected)
        {
            return;
        }

        if (backendMode == SdkBackendMode.Mock && autoUpdateMockSample)
        {
            UpdateMockSample();
        }
    }

    public bool Connect()
    {
        isConnected = true;

        switch (backendMode)
        {
            case SdkBackendMode.Mock:
                UpdateMockSample();
                break;

            case SdkBackendMode.PythonBridgePlaceholder:
            case SdkBackendMode.CSharpSdkPlaceholder:
                latestSample = FR5SdkPoseSample.CreateInvalid(backendMode.ToString());
                break;
        }

        if (verboseLog)
        {
            Debug.Log($"[FR5SdkClient] Connected: {backendMode}");
        }

        return true;
    }

    public void Disconnect()
    {
        isConnected = false;
        latestSample = FR5SdkPoseSample.CreateInvalid(backendMode.ToString());

        if (verboseLog)
        {
            Debug.Log("[FR5SdkClient] Disconnected.");
        }
    }

    public bool PollLatestState()
    {
        if (!isConnected)
        {
            return false;
        }

        switch (backendMode)
        {
            case SdkBackendMode.Mock:
                UpdateMockSample();
                return latestSample != null && latestSample.isValid;

            case SdkBackendMode.PythonBridgePlaceholder:
            case SdkBackendMode.CSharpSdkPlaceholder:
                return false;
        }

        return false;
    }

    public bool TryGetLatestSample(out FR5SdkPoseSample sample)
    {
        if (latestSample == null)
        {
            sample = FR5SdkPoseSample.CreateInvalid("NullSample");
            return false;
        }

        sample = latestSample.Clone();
        return sample.isValid;
    }

    public void SetMockJointDegrees(float j1, float j2, float j3, float j4, float j5, float j6)
    {
        mockJ1 = j1;
        mockJ2 = j2;
        mockJ3 = j3;
        mockJ4 = j4;
        mockJ5 = j5;
        mockJ6 = j6;

        if (backendMode == SdkBackendMode.Mock && isConnected)
        {
            UpdateMockSample();
        }
    }

    private void UpdateMockSample()
    {
        float j1 = mockJ1;
        float j2 = mockJ2;
        float j3 = mockJ3;
        float j4 = mockJ4;
        float j5 = mockJ5;
        float j6 = mockJ6;

        if (animateMockJoints)
        {
            float t = Time.time * mockAnimationSpeed;
            j1 += Mathf.Sin(t) * mockAnimationAmplitudeDeg;
            j2 += Mathf.Sin(t + 0.8f) * mockAnimationAmplitudeDeg;
            j3 += Mathf.Sin(t + 1.6f) * mockAnimationAmplitudeDeg;
            j4 += Mathf.Sin(t + 2.4f) * mockAnimationAmplitudeDeg;
            j5 += Mathf.Sin(t + 3.2f) * mockAnimationAmplitudeDeg;
            j6 += Mathf.Sin(t + 4.0f) * mockAnimationAmplitudeDeg;
        }

        FR5SdkPoseSample sample = new FR5SdkPoseSample();
        sample.isValid = true;
        sample.source = backendMode.ToString();
        sample.timestampSeconds = Time.realtimeSinceStartupAsDouble;

        sample.jointDegrees[0] = j1;
        sample.jointDegrees[1] = j2;
        sample.jointDegrees[2] = j3;
        sample.jointDegrees[3] = j4;
        sample.jointDegrees[4] = j5;
        sample.jointDegrees[5] = j6;

        sample.flangePositionMillimeters = mockFlangePositionMillimeters;
        sample.flangeRotationDegrees = mockFlangeRotationDegrees;
        sample.tcpPositionMillimeters = mockTcpPositionMillimeters;
        sample.tcpRotationDegrees = mockTcpRotationDegrees;

        sample.toolIndex = mockToolIndex;
        sample.userIndex = mockUserIndex;
        sample.robotMode = mockRobotMode;
        sample.robotState = mockRobotState;
        sample.alarmCode = mockAlarmCode;

        latestSample = sample;
    }
}
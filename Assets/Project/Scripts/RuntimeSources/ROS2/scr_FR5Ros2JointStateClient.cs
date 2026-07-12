using System;
using System.Reflection;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

/// <summary>
/// FR5 ROS2 JointState Client
///
/// Role:
/// 1. Subscribes to ROS2 /fr5/joint_states through ROS-TCP-Connector.
/// 2. Converts sensor_msgs/JointState position values from radians to Unity degree values.
/// 3. Maps joint values by joint name instead of fixed index only.
/// 4. Provides the latest sample through IFR5RuntimePoseSource.
/// 5. Stops ROS endpoint retry activity when this source is disconnected from RuntimeSyncManager.
/// </summary>
public class scr_FR5Ros2JointStateClient : MonoBehaviour, IFR5RuntimePoseSource
{
    [Header("ROS2 Topic")]
    [SerializeField] private string topicName = "/fr5/joint_states";
    [SerializeField] private bool autoConnectOnStart = false;

    [Header("Joint Mapping")]
    [SerializeField]
    private string[] expectedJointNames =
    {
        "joint1",
        "joint2",
        "joint3",
        "joint4",
        "joint5",
        "joint6"
    };

    [Header("Connection Check")]
    [SerializeField] private float messageTimeoutSeconds = 1.0f;

    [Header("Source Switch Options")]
    [Tooltip("If false, ROS2 source enters Waiting mode without opening a TCP connection. Use false for Windows-only replay tests.")]
    [SerializeField] private bool connectToTcpEndpointWhenSelected = false;

    [Tooltip("If enabled, Disconnect() also asks ROSConnection to stop its TCP retry loop. Keep this on for single-source Unity tests.")]
    [SerializeField] private bool stopRosConnectionOnDisconnect = true;

    [Tooltip("If enabled and ROSConnection supports Unsubscribe(string), the topic callback is removed when this source is disconnected.")]
    [SerializeField] private bool unsubscribeTopicOnDisconnect = false;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;
    [SerializeField] private bool logEveryReceivedMessage = false;
    [SerializeField] private bool logReflectionWarnings = false;

    [Header("Runtime Status")]
    [SerializeField] private bool isConnected = false;
    [SerializeField] private bool hasSubscribed = false;
    [SerializeField] private bool lastPollSucceeded = false;
    [SerializeField] private bool lastSampleValid = false;
    [SerializeField] private string lastSampleSource = "ROS2";
    [SerializeField] private string lastRobotState = "Disconnected";
    [SerializeField] private string lastPollTime = "-";
    [SerializeField] private string lastErrorMessage = "None";
    [SerializeField] private string lastJointSummary = "-";

    [Header("Gripper Feedback Visual")]
    [SerializeField] private bool applyGripperVisualFromJointState = true;
    [SerializeField] private bool autoResolveGripperJawByName = false;
    [SerializeField] private Transform gripperJawA;
    [SerializeField] private Transform gripperJawB;

    [Tooltip("Gazebo jaw joint position is in meters. Unity local offset = jaw position * this scale. 0.02m close -> 0.0002 local when scale is 0.01.")]
    [SerializeField] private float jawPositionToUnityLocalScale = 0.01f;

    [SerializeField] private bool clampJawPosition = true;
    [SerializeField] private float minJawPosition = 0f;
    [SerializeField] private float maxJawPosition = 0.02f;

    [SerializeField] private Vector3 jawAClosedLocalDirection = Vector3.right;
    [SerializeField] private Vector3 jawBClosedLocalDirection = Vector3.left;
    [SerializeField] private string jawAJointName = "jaw_a_joint";
    [SerializeField] private string jawBJointName = "jaw_b_joint";

    [Header("Gripper Feedback Status")]
    [SerializeField] private bool latestJawPositionReceived = false;
    [SerializeField] private float latestJawAPosition = 0f;
    [SerializeField] private float latestJawBPosition = 0f;

    private Vector3 gripperJawAOpenLocalPosition;
    private Vector3 gripperJawBOpenLocalPosition;
    private bool gripperOpenPoseCaptured = false;

    private ROSConnection rosConnection;
    private bool latestMessageReceived = false;
    private float lastMessageUnityTime = -1f;
    private FR5SdkPoseSample latestSample = FR5SdkPoseSample.CreateInvalid("ROS2");

    public bool IsConnected => isConnected;
    public bool LastPollSucceeded => lastPollSucceeded;
    public bool LastSampleValid => lastSampleValid;
    public string LastSampleSource => lastSampleSource;
    public string LastRobotState => lastRobotState;
    public string LastPollTime => lastPollTime;
    public string LastErrorMessage => lastErrorMessage;
    public string TopicName => topicName;
    public bool HasSubscribed => hasSubscribed;
    public bool ConnectToTcpEndpointWhenSelected => connectToTcpEndpointWhenSelected;
    public bool LatestMessageReceived => latestMessageReceived;
    public string LastJointSummary => lastJointSummary;
    public bool LatestJawPositionReceived => latestJawPositionReceived;
    public float LatestJawAPosition => latestJawAPosition;
    public float LatestJawBPosition => latestJawBPosition;

    private void Start()
    {
        ResolveGripperJawReferences();
        CaptureGripperOpenPoseIfNeeded();

        if (autoConnectOnStart)
        {
            Connect();
        }
        else
        {
            SetPollStatus(false, false, "ROS2 client is waiting for manual source selection.", "Disconnected");
        }
    }

    private void OnDisable()
    {
        Disconnect();
    }

    /// <summary>
    /// Connects this runtime source and subscribes to /fr5/joint_states.
    /// This does not guarantee that ros_tcp_endpoint is running.
    /// If the endpoint is missing, the source stays in Waiting / Poll Failed state.
    /// </summary>
    public bool Connect()
    {
        isConnected = true;
        latestMessageReceived = false;
        latestSample = FR5SdkPoseSample.CreateInvalid("ROS2");
        lastJointSummary = "-";
        latestJawPositionReceived = false;

        if (!connectToTcpEndpointWhenSelected)
        {
            SetPollStatus(
                false,
                false,
                "ROS2 source is selected, but TCP endpoint connection is disabled for offline Unity testing.",
                "Waiting ROS2 Endpoint"
            );

            if (verboseLog)
            {
                Debug.Log("[FR5Ros2JointStateClient] ROS2 source selected in safe waiting mode. TCP connection was not opened.");
            }

            return true;
        }

        rosConnection = ROSConnection.GetOrCreateInstance();

        if (!hasSubscribed)
        {
            rosConnection.Subscribe<JointStateMsg>(topicName, OnJointStateReceived);
            hasSubscribed = true;
        }

        // Some ROS-TCP-Connector versions expose a no-arg Connect().
        // Calling it by reflection keeps this script compatible across connector versions.
        TryInvokeRosConnectionNoArgMethod("Connect");

        SetPollStatus(false, false, "Waiting for ROS2 JointState message.", "Waiting ROS2 Endpoint");

        if (verboseLog)
        {
            Debug.Log($"[FR5Ros2JointStateClient] Connected. Subscribed topic: {topicName}");
        }

        return true;
    }

    /// <summary>
    /// Disconnects this runtime source.
    /// When RuntimeSyncManager switches back to UnityReplay or C# Bridge,
    /// this method also stops the ROSConnection retry loop so connection-failed logs do not keep appearing.
    /// </summary>
    public void Disconnect()
    {
        isConnected = false;
        latestMessageReceived = false;
        latestSample = FR5SdkPoseSample.CreateInvalid("ROS2");
        lastJointSummary = "-";
        latestJawPositionReceived = false;

        if (unsubscribeTopicOnDisconnect)
        {
            TryUnsubscribeTopic();
        }

        if (stopRosConnectionOnDisconnect)
        {
            TryStopRosConnectionRetryLoop();
        }

        SetPollStatus(false, false, "ROS2 client disconnected.", "Disconnected");

        if (verboseLog)
        {
            Debug.Log("[FR5Ros2JointStateClient] Disconnected.");
        }
    }

    public bool PollLatestState()
    {
        if (!isConnected)
        {
            SetPollStatus(false, false, "ROS2 client is not connected.", "Disconnected");
            return false;
        }

        if (!latestMessageReceived)
        {
            SetPollStatus(false, false, "No JointState message received yet.", "Waiting ROS2 Endpoint");
            return false;
        }

        float elapsed = Time.time - lastMessageUnityTime;

        if (elapsed > messageTimeoutSeconds)
        {
            SetPollStatus(false, false, $"JointState timeout. Elapsed={elapsed:F2}s", "ROS2 Timeout");
            return false;
        }

        bool valid = latestSample != null && latestSample.isValid;

        SetPollStatus(
            true,
            valid,
            valid ? "ROS2 JointState ready." : "Latest ROS2 sample is invalid.",
            valid ? "ROS2 Live" : "Sample Invalid"
        );

        return valid;
    }

    public bool TryGetLatestSample(out FR5SdkPoseSample sample)
    {
        if (latestSample == null)
        {
            sample = FR5SdkPoseSample.CreateInvalid("ROS2");
            return false;
        }

        sample = latestSample.Clone();
        return sample.isValid;
    }

    public string GetDisplayRobotStateLabel()
    {
        if (!isConnected)
        {
            return "Disconnected";
        }

        if (!latestMessageReceived)
        {
            return "Waiting ROS2 Endpoint";
        }

        if (!lastPollSucceeded)
        {
            return lastRobotState;
        }

        return lastSampleValid ? "ROS2 Live" : "Sample Invalid";
    }

    public string GetDisplayMessageLabel(string fallbackMessage)
    {
        if (!isConnected)
        {
            return "ROS2 client is not connected.";
        }

        if (!latestMessageReceived)
        {
            return "Waiting for ros_tcp_endpoint and /fr5/joint_states.";
        }

        if (!lastPollSucceeded)
        {
            return string.IsNullOrWhiteSpace(lastErrorMessage)
                ? "ROS2 poll failed."
                : lastErrorMessage;
        }

        if (!lastSampleValid)
        {
            return string.IsNullOrWhiteSpace(lastErrorMessage)
                ? "ROS2 sample is invalid."
                : lastErrorMessage;
        }

        if (!string.IsNullOrWhiteSpace(fallbackMessage) && fallbackMessage != "Idle")
        {
            return fallbackMessage;
        }

        return "ROS2 JointState ready.";
    }

    private void OnJointStateReceived(JointStateMsg msg)
    {
        if (!isConnected)
        {
            return;
        }

        if (msg == null)
        {
            latestSample = FR5SdkPoseSample.CreateInvalid("ROS2");
            SetPollStatus(false, false, "Received JointState message is null.", "Message Null");
            return;
        }

        if (!TryBuildJointDegrees(msg, out float[] jointDegrees, out string errorMessage))
        {
            latestSample = FR5SdkPoseSample.CreateInvalid("ROS2");
            latestMessageReceived = true;
            lastMessageUnityTime = Time.time;
            SetPollStatus(false, false, errorMessage, "Mapping Failed");
            return;
        }

        FR5SdkPoseSample sample = new FR5SdkPoseSample();
        sample.isValid = true;
        sample.source = "ROS2";
        sample.timestampSeconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

        for (int i = 0; i < 6; i++)
        {
            sample.jointDegrees[i] = jointDegrees[i];
        }

        // ROS2 JointState currently provides joint angle data only.
        // FK / TCP values are calculated by the Unity runtime path.
        sample.flangePositionMillimeters = Vector3.zero;
        sample.flangeRotationDegrees = Vector3.zero;
        sample.tcpPositionMillimeters = Vector3.zero;
        sample.tcpRotationDegrees = Vector3.zero;

        sample.toolIndex = -1;
        sample.userIndex = -1;
        sample.robotMode = "ROS2";
        sample.robotState = "ROS2 Live";
        sample.alarmCode = 0;

        latestSample = sample;
        latestMessageReceived = true;
        lastMessageUnityTime = Time.time;
        lastJointSummary = FormatJointSummary(jointDegrees);

        if (TryBuildJawPositions(msg, out float jawA, out float jawB))
        {
            latestJawPositionReceived = true;
            latestJawAPosition = jawA;
            latestJawBPosition = jawB;
            ApplyGripperVisualFromFeedback(jawA, jawB);
        }

        SetPollStatus(true, true, "ROS2 JointState received.", "ROS2 Live");

        if (verboseLog && logEveryReceivedMessage)
        {
            Debug.Log($"[FR5Ros2JointStateClient] Received JointState | Joints={lastJointSummary}");
        }
    }

    private bool TryBuildJointDegrees(JointStateMsg msg, out float[] jointDegrees, out string errorMessage)
    {
        jointDegrees = new float[6];
        errorMessage = "None";

        if (msg.name == null || msg.name.Length == 0)
        {
            errorMessage = "JointState.name is empty.";
            return false;
        }

        if (msg.position == null || msg.position.Length == 0)
        {
            errorMessage = "JointState.position is empty.";
            return false;
        }

        for (int i = 0; i < expectedJointNames.Length; i++)
        {
            int msgIndex = FindJointIndex(msg.name, expectedJointNames[i]);

            if (msgIndex < 0)
            {
                errorMessage = $"Joint name not found: {expectedJointNames[i]}";
                return false;
            }

            if (msgIndex >= msg.position.Length)
            {
                errorMessage = $"Position index out of range for joint: {expectedJointNames[i]}";
                return false;
            }

            jointDegrees[i] = (float)(msg.position[msgIndex] * Mathf.Rad2Deg);
        }

        return true;
    }

    private int FindJointIndex(string[] names, string expectedName)
    {
        string normalizedExpected = NormalizeJointName(expectedName);

        for (int i = 0; i < names.Length; i++)
        {
            if (NormalizeJointName(names[i]) == normalizedExpected)
            {
                return i;
            }
        }

        return -1;
    }

    private string NormalizeJointName(string jointName)
    {
        if (string.IsNullOrWhiteSpace(jointName))
        {
            return string.Empty;
        }

        return jointName
            .Trim()
            .ToLowerInvariant()
            .Replace("_", "")
            .Replace("-", "")
            .Replace(" ", "");
    }

    private bool TryBuildJawPositions(JointStateMsg msg, out float jawA, out float jawB)
    {
        jawA = 0f;
        jawB = 0f;

        if (msg == null || msg.name == null || msg.position == null)
        {
            return false;
        }

        bool hasJawA = TryGetJointPosition(msg, jawAJointName, out jawA);
        bool hasJawB = TryGetJointPosition(msg, jawBJointName, out jawB);

        return hasJawA && hasJawB;
    }

    private bool TryGetJointPosition(JointStateMsg msg, string jointName, out float position)
    {
        position = 0f;

        int msgIndex = FindJointIndex(msg.name, jointName);

        if (msgIndex < 0 || msgIndex >= msg.position.Length)
        {
            return false;
        }

        position = (float)msg.position[msgIndex];
        return true;
    }

    private void ApplyGripperVisualFromFeedback(float jawA, float jawB)
    {
        if (!applyGripperVisualFromJointState)
        {
            return;
        }

        ResolveGripperJawReferences();
        CaptureGripperOpenPoseIfNeeded();

        if (!gripperOpenPoseCaptured)
        {
            return;
        }

        if (clampJawPosition)
        {
            jawA = Mathf.Clamp(jawA, minJawPosition, maxJawPosition);
            jawB = Mathf.Clamp(jawB, minJawPosition, maxJawPosition);
        }

        if (gripperJawA != null)
        {
            gripperJawA.localPosition = gripperJawAOpenLocalPosition
                + jawAClosedLocalDirection.normalized * jawA * jawPositionToUnityLocalScale;
        }

        if (gripperJawB != null)
        {
            gripperJawB.localPosition = gripperJawBOpenLocalPosition
                + jawBClosedLocalDirection.normalized * jawB * jawPositionToUnityLocalScale;
        }
    }

    private void ResolveGripperJawReferences()
    {
        if (!autoResolveGripperJawByName)
        {
            return;
        }

        if (gripperJawA == null)
        {
            GameObject jawAObject = GameObject.Find("Jaw_A");

            if (jawAObject != null)
            {
                gripperJawA = jawAObject.transform;
            }
        }

        if (gripperJawB == null)
        {
            GameObject jawBObject = GameObject.Find("Jaw_B");

            if (jawBObject != null)
            {
                gripperJawB = jawBObject.transform;
            }
        }
    }

    private void CaptureGripperOpenPoseIfNeeded()
    {
        if (gripperOpenPoseCaptured)
        {
            return;
        }

        ResolveGripperJawReferences();

        if (gripperJawA == null || gripperJawB == null)
        {
            return;
        }

        gripperJawAOpenLocalPosition = gripperJawA.localPosition;
        gripperJawBOpenLocalPosition = gripperJawB.localPosition;
        gripperOpenPoseCaptured = true;
    }

    private void SetPollStatus(bool pollSucceeded, bool sampleValid, string message, string robotState)
    {
        lastPollSucceeded = pollSucceeded;
        lastSampleValid = sampleValid;
        lastSampleSource = "ROS2";
        lastErrorMessage = string.IsNullOrWhiteSpace(message) ? "None" : message;
        lastRobotState = string.IsNullOrWhiteSpace(robotState) ? "Unknown" : robotState;
        lastPollTime = DateTime.Now.ToString("HH:mm:ss");
    }

    private void TryUnsubscribeTopic()
    {
        if (rosConnection == null)
        {
            return;
        }

        MethodInfo method = rosConnection.GetType().GetMethod(
            "Unsubscribe",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new Type[] { typeof(string) },
            null
        );

        if (method == null)
        {
            LogReflectionWarning("ROSConnection.Unsubscribe(string) was not found. Subscription is kept.");
            return;
        }

        try
        {
            method.Invoke(rosConnection, new object[] { topicName });
            hasSubscribed = false;
        }
        catch (Exception exception)
        {
            LogReflectionWarning($"ROSConnection.Unsubscribe failed: {exception.Message}");
        }
    }

    private void TryStopRosConnectionRetryLoop()
    {
        if (rosConnection == null)
        {
            return;
        }

        bool stopped = false;

        // ROS-TCP-Connector method names differ by package version.
        // Try all known no-arg disconnect/close style methods and keep this compatible.
        stopped |= TryInvokeRosConnectionNoArgMethod("Disconnect");
        stopped |= TryInvokeRosConnectionNoArgMethod("DisconnectFromRos");
        stopped |= TryInvokeRosConnectionNoArgMethod("CloseConnection");
        stopped |= TryInvokeRosConnectionNoArgMethod("Close");
        stopped |= TryInvokeRosConnectionNoArgMethod("Shutdown");

        if (!stopped)
        {
            LogReflectionWarning("No ROSConnection disconnect method was found. TCP retry loop may continue until Play Mode stops.");
        }
    }

    private bool TryInvokeRosConnectionNoArgMethod(string methodName)
    {
        if (rosConnection == null)
        {
            return false;
        }

        MethodInfo method = rosConnection.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            Type.EmptyTypes,
            null
        );

        if (method == null)
        {
            return false;
        }

        try
        {
            method.Invoke(rosConnection, null);
            return true;
        }
        catch (Exception exception)
        {
            LogReflectionWarning($"ROSConnection.{methodName} failed: {exception.Message}");
            return false;
        }
    }

    private void LogReflectionWarning(string message)
    {
        if (!logReflectionWarnings)
        {
            return;
        }

        Debug.LogWarning($"[FR5Ros2JointStateClient] {message}");
    }

    private string FormatJointSummary(float[] joints)
    {
        if (joints == null || joints.Length < 6)
        {
            return "-";
        }

        return
            $"[{joints[0]:F3}, {joints[1]:F3}, {joints[2]:F3}, " +
            $"{joints[3]:F3}, {joints[4]:F3}, {joints[5]:F3}]";
    }
}

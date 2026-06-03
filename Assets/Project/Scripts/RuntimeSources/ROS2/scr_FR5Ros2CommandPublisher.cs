using System;
using System.Globalization;
using System.Text;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

/// <summary>
/// Publishes dry-run FR5 command JSON from Unity to ROS2.
/// This publisher only sends std_msgs/String to /fr5/unity_command.
/// It does not publish robot trajectory commands.
/// </summary>
public class scr_FR5Ros2CommandPublisher : MonoBehaviour
{
    [Header("ROS2 Command Topic")]
    [SerializeField] private string topicName = "/fr5/unity_command";
    [SerializeField] private bool registerOnStart = true;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private ROSConnection rosConnection;
    private bool publisherRegistered = false;

    public string TopicName => topicName;
    public bool PublisherRegistered => publisherRegistered;

    private void Start()
    {
        if (registerOnStart)
        {
            EnsurePublisherRegistered();
        }
    }

    public bool PublishMoveJ(float[] jointsDeg, int speedPercent)
    {
        return PublishCommand("MOVE_J", NormalizeJoints(jointsDeg), speedPercent);
    }

    public bool PublishStop()
    {
        return PublishCommand("STOP", null, 0);
    }

    public bool PublishHome(int speedPercent)
    {
        return PublishCommand("HOME", null, speedPercent);
    }

    public bool PublishReset()
    {
        return PublishCommand("RESET", null, 0);
    }

    private bool PublishCommand(string commandName, float[] jointsDeg, int speedPercent)
    {
        if (!EnsurePublisherRegistered())
        {
            return false;
        }

        string json = BuildCommandJson(commandName, jointsDeg, speedPercent);

        try
        {
            rosConnection.Publish(topicName, new StringMsg(json));

            if (verboseLog)
            {
                Debug.Log($"[FR5Ros2CommandPublisher] Published {commandName} to {topicName}: {json}");
            }

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[FR5Ros2CommandPublisher] Failed to publish {commandName}: {exception.Message}");
            return false;
        }
    }

    private bool EnsurePublisherRegistered()
    {
        if (publisherRegistered)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(topicName))
        {
            Debug.LogWarning("[FR5Ros2CommandPublisher] Topic name is empty.");
            return false;
        }

        try
        {
            rosConnection = ROSConnection.GetOrCreateInstance();

            if (rosConnection == null)
            {
                Debug.LogWarning("[FR5Ros2CommandPublisher] ROSConnection is not available.");
                return false;
            }

            rosConnection.RegisterPublisher<StringMsg>(topicName);
            publisherRegistered = true;

            if (verboseLog)
            {
                Debug.Log($"[FR5Ros2CommandPublisher] Registered publisher: {topicName}");
            }

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[FR5Ros2CommandPublisher] Failed to register publisher: {exception.Message}");
            return false;
        }
    }

    private string BuildCommandJson(string commandName, float[] jointsDeg, int speedPercent)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("{");
        AppendStringField(builder, "source", "Unity", appendComma: true);
        AppendStringField(builder, "robot", "FR5", appendComma: true);
        AppendStringField(builder, "command", commandName, appendComma: true);

        if (jointsDeg != null)
        {
            builder.Append("\"joints_deg\":");
            AppendFloatArray(builder, jointsDeg);
            builder.Append(",");
        }

        builder.Append("\"speed_percent\":");
        builder.Append(Mathf.Clamp(speedPercent, 0, 100).ToString(CultureInfo.InvariantCulture));
        builder.Append(",");
        builder.Append("\"timestamp_unix_ms\":");
        builder.Append(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture));
        builder.Append("}");
        return builder.ToString();
    }

    private void AppendStringField(StringBuilder builder, string fieldName, string value, bool appendComma)
    {
        builder.Append("\"");
        builder.Append(fieldName);
        builder.Append("\":\"");
        builder.Append(EscapeJson(value));
        builder.Append("\"");

        if (appendComma)
        {
            builder.Append(",");
        }
    }

    private void AppendFloatArray(StringBuilder builder, float[] values)
    {
        builder.Append("[");

        for (int i = 0; i < values.Length; i++)
        {
            builder.Append(values[i].ToString("0.###", CultureInfo.InvariantCulture));

            if (i < values.Length - 1)
            {
                builder.Append(",");
            }
        }

        builder.Append("]");
    }

    private float[] NormalizeJoints(float[] source)
    {
        float[] result = new float[6];

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = source != null && i < source.Length ? source[i] : 0f;
        }

        return result;
    }

    private string EscapeJson(string value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}

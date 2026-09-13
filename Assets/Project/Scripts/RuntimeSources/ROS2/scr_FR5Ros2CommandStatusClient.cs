using System;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

/// <summary>Read-only command status subscription. Does not publish, connect/disconnect, or control a robot.</summary>
[DisallowMultipleComponent]
public class scr_FR5Ros2CommandStatusClient : MonoBehaviour
{
    [Serializable]
    public sealed class CommandStatus
    {
        public string source;
        public string robot;
        public string command;
        public bool accepted;
        public bool executed;
        public string state;
        public string message;
        public long timestamp_unix_ms;
        public string request_id; // Optional: absent on legacy responses.
        public string fr5_take;   // Optional: absent on legacy responses.
    }

    [SerializeField] private string topicName = "/fr5/command_status";
    [SerializeField] private bool subscribeOnStart = true;
    private ROSConnection connection;
    private string subscribedTopic;
    private float nextWarningTime;
    public bool HasSubscribed => connection != null && subscribedTopic != null;
    public CommandStatus LastStatus { get; private set; }
    public event Action<CommandStatus> StatusReceived;

    private void Start()
    {
        if (subscribeOnStart) EnsureSubscribed();
    }

    public bool EnsureSubscribed()
    {
        if (!Application.isPlaying || !isActiveAndEnabled) return false;
        if (HasSubscribed) return string.Equals(subscribedTopic, topicName, StringComparison.Ordinal);
        if (string.IsNullOrWhiteSpace(topicName)) return false;
        try
        {
            connection = ROSConnection.GetOrCreateInstance();
            if (connection == null) return false;
            connection.Subscribe<StringMsg>(topicName, OnStatusReceived);
            subscribedTopic = topicName;
            return true; // Registration only, not proof of an active backend.
        }
        catch (Exception exception)
        {
            WarnThrottled("Subscribe failed: " + exception.Message);
            return false;
        }
    }

    public static bool TryParseStatusJson(string json, out CommandStatus status)
    {
        status = null;
        if (string.IsNullOrWhiteSpace(json) || json.Length > 16384) return false;
        string trimmed = json.Trim();
        if (!trimmed.StartsWith("{", StringComparison.Ordinal) ||
            !trimmed.EndsWith("}", StringComparison.Ordinal)) return false;
        try
        {
            CommandStatus parsed = JsonUtility.FromJson<CommandStatus>(trimmed);
            if (parsed == null || string.IsNullOrWhiteSpace(parsed.source) ||
                string.IsNullOrWhiteSpace(parsed.robot) || string.IsNullOrWhiteSpace(parsed.command))
                return false;
            status = parsed;
            return true; // Unknown state and absent future fields are allowed.
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void OnStatusReceived(StringMsg message)
    {
        // ROS-TCP-Connector dispatches subscriptions from ROSConnection.Update (main thread).
        // Its Unsubscribe(topic) removes ALL listeners. Keep this guarded callback across disable/
        // enable instead of removing another subscriber. ROSConnection teardown owns final cleanup.
        if (this == null || !Application.isPlaying || !isActiveAndEnabled) return;
        if (!TryParseStatusJson(message == null ? null : message.data, out CommandStatus status))
        {
            WarnThrottled("Invalid command status JSON ignored.");
            return;
        }
        LastStatus = status; // Legacy command status remains observable, but is not a TAKE result.
        StatusReceived?.Invoke(status);
    }

    private void WarnThrottled(string message)
    {
        if (Time.unscaledTime < nextWarningTime) return;
        nextWarningTime = Time.unscaledTime + 5f;
        Debug.LogWarning("[FR5 Command Status] " + message, this);
    }
}

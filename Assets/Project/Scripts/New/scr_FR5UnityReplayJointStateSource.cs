using System;
using System.IO;
using UnityEngine;

/// <summary>
/// FR5 Unity Replay JointState Source
///
/// 역할:
/// 1. StreamingAssets/FR5Replay 폴더의 JSON replay 파일을 읽는다.
/// 2. ROS2 /fr5/joint_states에서 변환한 joint1~joint6 값을 시간 순서대로 재생한다.
/// 3. RuntimeSyncManager가 다른 LIVE Source처럼 Poll해서 사용할 수 있도록 IFR5RuntimePoseSource를 구현한다.
/// 4. 나중에 ROS2 실시간 Subscribe 모드와 동일한 FR5SdkPoseSample 구조를 사용한다.
///
/// 사용 위치:
/// Assets/StreamingAssets/FR5Replay/fr5_joint_states_*.json
/// </summary>
public class scr_FR5UnityReplayJointStateSource : MonoBehaviour, IFR5RuntimePoseSource
{
    [Serializable]
    private class ReplayRoot
    {
        public string source_bag;
        public string topic;
        public string message_type;
        public string[] joint_order;
        public int sample_count;
        public float duration_sec;
        public ReplayUnit unit;
        public ReplaySample[] samples;
    }

    [Serializable]
    private class ReplayUnit
    {
        public string positions_rad;
        public string positions_deg;
    }

    [Serializable]
    private class ReplaySample
    {
        public float t;
        public double stamp_sec;
        public string[] names;
        public float[] positions_rad;
        public float[] positions_deg;
    }

    [Header("Replay File")]
    [Tooltip("StreamingAssets 기준 replay 폴더명입니다.")]
    [SerializeField] private string replayFolderName = "FR5Replay";

    [Tooltip("비워두면 FR5Replay 폴더에서 가장 최근 json 파일을 자동 선택합니다.")]
    [SerializeField] private string jsonFileName = "fr5_joint_states_20260520_110951.json";

    [Header("Playback")]
    [SerializeField] private bool playOnConnect = true;
    [SerializeField] private bool loopPlayback = true;
    [SerializeField] private bool holdLastSampleWhenFinished = true;
    [SerializeField] private float playbackSpeed = 1.0f;

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

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;
    [SerializeField] private bool logEveryAppliedSample = false;

    [Header("Runtime Status")]
    [SerializeField] private bool isConnected = false;
    [SerializeField] private bool isLoaded = false;
    [SerializeField] private bool isPlaying = false;
    [SerializeField] private bool lastPollSucceeded = false;
    [SerializeField] private bool lastSampleValid = false;
    [SerializeField] private string loadedFilePath = "-";
    [SerializeField] private string lastPollTime = "-";
    [SerializeField] private string lastErrorMessage = "None";
    [SerializeField] private string lastRobotState = "Disconnected";
    [SerializeField] private string lastJointSummary = "-";
    [SerializeField] private int currentSampleIndex = 0;
    [SerializeField] private int sampleCount = 0;
    [SerializeField] private float durationSeconds = 0f;

    private ReplayRoot replayData;
    private float playbackStartUnityTime = 0f;
    private float pausedElapsedSeconds = 0f;
    private FR5SdkPoseSample latestSample = FR5SdkPoseSample.CreateInvalid("UnityReplay");

    public bool IsConnected => isConnected;
    public bool IsLoaded => isLoaded;
    public bool IsPlaying => isPlaying;
    public bool LoopPlayback => loopPlayback;
    public bool HoldLastSampleWhenFinished => holdLastSampleWhenFinished;
    public float PlaybackSpeed => playbackSpeed;
    public bool LastPollSucceeded => lastPollSucceeded;
    public bool LastSampleValid => lastSampleValid;
    public string LastPollTime => lastPollTime;
    public string LastErrorMessage => lastErrorMessage;
    public string LoadedFilePath => loadedFilePath;
    public string LastRobotState => lastRobotState;
    public string LastJointSummary => lastJointSummary;
    public int CurrentSampleIndex => currentSampleIndex;
    public int SampleCount => sampleCount;
    public float DurationSeconds => durationSeconds;

    public float CurrentReplayTimeSeconds
    {
        get
        {
            if (replayData == null || replayData.samples == null || replayData.samples.Length == 0)
            {
                return 0f;
            }

            int safeIndex = Mathf.Clamp(currentSampleIndex, 0, replayData.samples.Length - 1);
            return replayData.samples[safeIndex].t;
        }
    }

    public string GetReplayProgressLabel()
    {
        if (sampleCount <= 0)
        {
            return "0 / 0";
        }

        int displayIndex = Mathf.Clamp(currentSampleIndex + 1, 1, sampleCount);
        return $"{displayIndex} / {sampleCount}";
    }

    public string GetReplayTimeLabel()
    {
        return $"{CurrentReplayTimeSeconds:0.00} / {durationSeconds:0.00}s";
    }

    public string GetReplayStateLabel()
    {
        if (!isConnected)
        {
            return "DISCONNECTED";
        }

        if (!isLoaded)
        {
            return "NOT LOADED";
        }

        return isPlaying ? "PLAYING" : "PAUSED";
    }

    private void Start()
    {
        if (playOnConnect)
        {
            Connect();
        }
    }

    public bool Connect()
    {
        if (!isLoaded)
        {
            if (!LoadReplayFile())
            {
                isConnected = false;
                isPlaying = false;
                return false;
            }
        }

        isConnected = true;
        isPlaying = playOnConnect;
        playbackStartUnityTime = Time.time;
        pausedElapsedSeconds = 0f;
        currentSampleIndex = 0;

        BuildSampleAtIndex(0);

        SetPollStatus(
            true,
            latestSample != null && latestSample.isValid,
            "Unity replay connected.",
            isPlaying ? "Unity Replay Playing" : "Unity Replay Paused"
        );

        if (verboseLog)
        {
            Debug.Log(
                "[FR5UnityReplayJointStateSource] Connected | " +
                $"Samples={sampleCount} | Duration={durationSeconds:F3}s | File={loadedFilePath}"
            );
        }

        return true;
    }

    public void Disconnect()
    {
        isConnected = false;
        isPlaying = false;

        SetPollStatus(false, false, "Unity replay disconnected.", "Disconnected");

        if (verboseLog)
        {
            Debug.Log("[FR5UnityReplayJointStateSource] Disconnected.");
        }
    }

    public bool PollLatestState()
    {
        if (!isConnected)
        {
            SetPollStatus(false, false, "Unity replay source is not connected.", "Disconnected");
            return false;
        }

        if (!isLoaded || replayData == null || replayData.samples == null || replayData.samples.Length == 0)
        {
            SetPollStatus(false, false, "Unity replay data is not loaded.", "Replay Missing");
            return false;
        }

        if (!isPlaying)
        {
            bool pausedValid = latestSample != null && latestSample.isValid;
            SetPollStatus(pausedValid, pausedValid, "Unity replay paused.", "Unity Replay Paused");
            return pausedValid;
        }

        float elapsedSeconds = GetPlaybackElapsedSeconds();
        int nextIndex = FindSampleIndexByTime(elapsedSeconds);

        if (!BuildSampleAtIndex(nextIndex))
        {
            SetPollStatus(false, false, "Failed to build Unity replay sample.", "Sample Invalid");
            return false;
        }

        SetPollStatus(true, true, "Unity replay sample ready.", "Unity Replay");

        if (verboseLog && logEveryAppliedSample)
        {
            Debug.Log(
                "[FR5UnityReplayJointStateSource] Sample applied | " +
                $"Index={currentSampleIndex}/{sampleCount} | " +
                $"Time={elapsedSeconds:F3}s | Joints={lastJointSummary}"
            );
        }

        return true;
    }

    public bool TryGetLatestSample(out FR5SdkPoseSample sample)
    {
        if (latestSample == null)
        {
            sample = FR5SdkPoseSample.CreateInvalid("UnityReplay");
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

        if (!isLoaded)
        {
            return "Replay Missing";
        }

        if (!isPlaying)
        {
            return "Unity Replay Paused";
        }

        return lastSampleValid ? "Unity Replay" : "Sample Invalid";
    }

    public string GetDisplayMessageLabel(string fallbackMessage)
    {
        if (!isConnected)
        {
            return "Unity replay source is not connected.";
        }

        if (!isLoaded)
        {
            return string.IsNullOrWhiteSpace(lastErrorMessage)
                ? "Unity replay data is not loaded."
                : lastErrorMessage;
        }

        if (!lastPollSucceeded)
        {
            return string.IsNullOrWhiteSpace(lastErrorMessage)
                ? "Unity replay poll failed."
                : lastErrorMessage;
        }

        if (!string.IsNullOrWhiteSpace(fallbackMessage) && fallbackMessage != "Idle")
        {
            return fallbackMessage;
        }

        return $"Unity replay sample {currentSampleIndex + 1}/{sampleCount}";
    }

    [ContextMenu("Load Replay File")]
    public void LoadReplayFileFromInspector()
    {
        LoadReplayFile();
    }

    [ContextMenu("Restart Replay")]
    public void RestartReplayFromInspector()
    {
        RestartReplay();
    }

    [ContextMenu("Pause Replay")]
    public void PauseReplayFromInspector()
    {
        PauseReplay();
    }

    [ContextMenu("Resume Replay")]
    public void ResumeReplayFromInspector()
    {
        ResumeReplay();
    }

    public void RestartReplay()
    {
        if (!isLoaded && !LoadReplayFile())
        {
            return;
        }

        playbackStartUnityTime = Time.time;
        pausedElapsedSeconds = 0f;
        currentSampleIndex = 0;
        isPlaying = true;

        BuildSampleAtIndex(0);
        SetPollStatus(true, true, "Unity replay restarted.", "Unity Replay");

        if (verboseLog)
        {
            Debug.Log("[FR5UnityReplayJointStateSource] Replay restarted.");
        }
    }

    public void PauseReplay()
    {
        if (!isConnected)
        {
            return;
        }

        pausedElapsedSeconds = GetPlaybackElapsedSeconds();
        isPlaying = false;

        SetPollStatus(true, latestSample != null && latestSample.isValid, "Unity replay paused.", "Unity Replay Paused");
    }

    public void ResumeReplay()
    {
        if (!isConnected)
        {
            return;
        }

        playbackStartUnityTime = Time.time - pausedElapsedSeconds;
        isPlaying = true;

        SetPollStatus(true, latestSample != null && latestSample.isValid, "Unity replay resumed.", "Unity Replay");
    }

    /// <summary>
    /// Stop replay playback and return to the first sample.
    /// This is used by the UI STOP button during Unity Replay mode.
    /// </summary>
    public void StopReplay()
    {
        if (!isLoaded && !LoadReplayFile())
        {
            return;
        }

        pausedElapsedSeconds = 0f;
        playbackStartUnityTime = Time.time;
        isPlaying = false;

        BuildSampleAtIndex(0);
        SetPollStatus(true, latestSample != null && latestSample.isValid, "Unity replay stopped.", "Unity Replay Stopped");

        if (verboseLog)
        {
            Debug.Log("[FR5UnityReplayJointStateSource] Replay stopped and reset to first sample.");
        }
    }

    /// <summary>
    /// Move replay playback back to the first sample.
    /// The current play / pause state is preserved.
    /// </summary>
    public void ResetReplayToStart()
    {
        if (!isLoaded && !LoadReplayFile())
        {
            return;
        }

        bool shouldKeepPlaying = isPlaying;

        pausedElapsedSeconds = 0f;
        playbackStartUnityTime = Time.time;

        BuildSampleAtIndex(0);
        isPlaying = shouldKeepPlaying;

        SetPollStatus(true, latestSample != null && latestSample.isValid, "Unity replay reset to first sample.", isPlaying ? "Unity Replay" : "Unity Replay Paused");

        if (verboseLog)
        {
            Debug.Log("[FR5UnityReplayJointStateSource] Replay reset to first sample.");
        }
    }

    /// <summary>
    /// Set replay playback speed while preserving the current replay time as much as possible.
    /// </summary>
    public void SetPlaybackSpeed(float targetSpeed)
    {
        float currentReplayTime = isLoaded ? GetPlaybackElapsedSeconds() : 0f;
        float nextSpeed = Mathf.Clamp(targetSpeed, 0.1f, 5.0f);

        playbackSpeed = nextSpeed;

        if (isPlaying)
        {
            playbackStartUnityTime = Time.time - (currentReplayTime / Mathf.Max(0.001f, playbackSpeed));
        }
        else
        {
            pausedElapsedSeconds = currentReplayTime;
        }

        SetPollStatus(true, latestSample != null && latestSample.isValid, $"Unity replay speed changed to {playbackSpeed:0.##}x.", isPlaying ? "Unity Replay" : "Unity Replay Paused");

        if (verboseLog)
        {
            Debug.Log($"[FR5UnityReplayJointStateSource] Replay speed changed: {playbackSpeed:0.##}x");
        }
    }

    /// <summary>
    /// Increase or decrease replay playback speed.
    /// </summary>
    public void AdjustPlaybackSpeed(float delta)
    {
        SetPlaybackSpeed(playbackSpeed + delta);
    }

    /// <summary>
    /// Return a compact playback speed label for UI logs.
    /// </summary>
    public string GetPlaybackSpeedLabel()
    {
        return $"{playbackSpeed:0.##}x";
    }

    private bool LoadReplayFile()
    {
        string selectedPath = ResolveReplayJsonPath();

        if (string.IsNullOrWhiteSpace(selectedPath) || !File.Exists(selectedPath))
        {
            isLoaded = false;
            latestSample = FR5SdkPoseSample.CreateInvalid("UnityReplay");

            SetPollStatus(false, false, $"Replay json file not found: {selectedPath}", "File Missing");
            Debug.LogError($"[FR5UnityReplayJointStateSource] Replay json file not found: {selectedPath}");
            return false;
        }

        try
        {
            string jsonText = File.ReadAllText(selectedPath);
            replayData = JsonUtility.FromJson<ReplayRoot>(jsonText);

            if (replayData == null || replayData.samples == null || replayData.samples.Length == 0)
            {
                isLoaded = false;
                latestSample = FR5SdkPoseSample.CreateInvalid("UnityReplay");

                SetPollStatus(false, false, "Replay json has no samples.", "Empty Replay");
                Debug.LogError("[FR5UnityReplayJointStateSource] Replay json has no samples.");
                return false;
            }

            loadedFilePath = selectedPath;
            sampleCount = replayData.samples.Length;
            durationSeconds = replayData.duration_sec > 0f
                ? replayData.duration_sec
                : replayData.samples[replayData.samples.Length - 1].t;

            isLoaded = true;
            currentSampleIndex = 0;

            BuildSampleAtIndex(0);
            SetPollStatus(true, true, "Unity replay file loaded.", "Replay Loaded");

            if (verboseLog)
            {
                Debug.Log(
                    "[FR5UnityReplayJointStateSource] Replay loaded | " +
                    $"Samples={sampleCount} | Duration={durationSeconds:F3}s | Path={loadedFilePath}"
                );
            }

            return true;
        }
        catch (Exception exception)
        {
            isLoaded = false;
            latestSample = FR5SdkPoseSample.CreateInvalid("UnityReplay");

            SetPollStatus(false, false, exception.Message, "Load Failed");
            Debug.LogError($"[FR5UnityReplayJointStateSource] Failed to load replay file: {exception.Message}");
            return false;
        }
    }

    private string ResolveReplayJsonPath()
    {
        string folderPath = Path.Combine(Application.streamingAssetsPath, replayFolderName);

        if (!Directory.Exists(folderPath))
        {
            return Path.Combine(folderPath, jsonFileName);
        }

        if (!string.IsNullOrWhiteSpace(jsonFileName))
        {
            return Path.Combine(folderPath, jsonFileName);
        }

        string[] jsonFiles = Directory.GetFiles(folderPath, "*.json");

        if (jsonFiles == null || jsonFiles.Length == 0)
        {
            return string.Empty;
        }

        Array.Sort(
            jsonFiles,
            (left, right) => File.GetLastWriteTimeUtc(right).CompareTo(File.GetLastWriteTimeUtc(left))
        );

        return jsonFiles[0];
    }

    private float GetPlaybackElapsedSeconds()
    {
        float elapsed = Mathf.Max(0f, (Time.time - playbackStartUnityTime) * Mathf.Max(0.001f, playbackSpeed));

        if (durationSeconds <= 0f)
        {
            return elapsed;
        }

        if (loopPlayback)
        {
            return Mathf.Repeat(elapsed, durationSeconds);
        }

        if (elapsed >= durationSeconds)
        {
            if (holdLastSampleWhenFinished)
            {
                isPlaying = false;
                pausedElapsedSeconds = durationSeconds;
                return durationSeconds;
            }

            return durationSeconds;
        }

        return elapsed;
    }

    private int FindSampleIndexByTime(float targetTimeSeconds)
    {
        if (replayData == null || replayData.samples == null || replayData.samples.Length == 0)
        {
            return 0;
        }

        int low = 0;
        int high = replayData.samples.Length - 1;
        int result = 0;

        while (low <= high)
        {
            int mid = (low + high) / 2;
            float sampleTime = replayData.samples[mid].t;

            if (sampleTime <= targetTimeSeconds)
            {
                result = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return Mathf.Clamp(result, 0, replayData.samples.Length - 1);
    }

    private bool BuildSampleAtIndex(int index)
    {
        if (replayData == null || replayData.samples == null || replayData.samples.Length == 0)
        {
            latestSample = FR5SdkPoseSample.CreateInvalid("UnityReplay");
            return false;
        }

        int safeIndex = Mathf.Clamp(index, 0, replayData.samples.Length - 1);
        ReplaySample frame = replayData.samples[safeIndex];

        if (!TryBuildJointDegrees(frame, out float[] jointDegrees, out string errorMessage))
        {
            latestSample = FR5SdkPoseSample.CreateInvalid("UnityReplay");
            lastJointSummary = "-";
            lastErrorMessage = errorMessage;
            return false;
        }

        FR5SdkPoseSample sample = new FR5SdkPoseSample();
        sample.isValid = true;
        sample.source = "UnityReplay";
        sample.timestampSeconds = frame.stamp_sec;

        for (int i = 0; i < 6; i++)
        {
            sample.jointDegrees[i] = jointDegrees[i];
        }

        sample.flangePositionMillimeters = Vector3.zero;
        sample.flangeRotationDegrees = Vector3.zero;
        sample.tcpPositionMillimeters = Vector3.zero;
        sample.tcpRotationDegrees = Vector3.zero;

        sample.toolIndex = -1;
        sample.userIndex = -1;
        sample.robotMode = "Replay";
        sample.robotState = "Unity Replay";
        sample.alarmCode = 0;

        latestSample = sample;
        currentSampleIndex = safeIndex;
        lastJointSummary = FormatJointSummary(jointDegrees);

        return true;
    }

    private bool TryBuildJointDegrees(ReplaySample frame, out float[] jointDegrees, out string errorMessage)
    {
        jointDegrees = new float[6];
        errorMessage = "None";

        if (frame == null)
        {
            errorMessage = "Replay frame is null.";
            return false;
        }

        if (frame.positions_deg != null && frame.positions_deg.Length >= 6)
        {
            for (int i = 0; i < 6; i++)
            {
                jointDegrees[i] = frame.positions_deg[i];
            }

            return true;
        }

        if (frame.positions_rad != null && frame.positions_rad.Length >= 6)
        {
            for (int i = 0; i < 6; i++)
            {
                jointDegrees[i] = frame.positions_rad[i] * Mathf.Rad2Deg;
            }

            return true;
        }

        errorMessage = "Replay frame has no valid joint position array.";
        return false;
    }

    private void SetPollStatus(bool pollSucceeded, bool sampleValid, string message, string robotState)
    {
        lastPollSucceeded = pollSucceeded;
        lastSampleValid = sampleValid;
        lastErrorMessage = string.IsNullOrWhiteSpace(message) ? "None" : message;
        lastRobotState = string.IsNullOrWhiteSpace(robotState) ? "Unknown" : robotState;
        lastPollTime = DateTime.Now.ToString("HH:mm:ss");
    }

    private string FormatJointSummary(float[] joints)
    {
        if (joints == null || joints.Length < 6)
        {
            return "-";
        }

        return
            $"[{joints[0]:F1}, {joints[1]:F1}, {joints[2]:F1}, " +
            $"{joints[3]:F1}, {joints[4]:F1}, {joints[5]:F1}]";
    }
}

using UnityEngine;

public class scr_FR5CSharpSdkClient : MonoBehaviour, IFR5RuntimePoseSource
{
    [Header("Connection")]
    [SerializeField] private string robotIp = "192.168.58.2";
    [SerializeField] private bool autoConnectOnStart = false;

    [Header("Command Speed")]
    [SerializeField] private int defaultCommandSpeedPercent = 100;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private IFR5HardwareAdapter hardwareAdapter;
    private FR5SdkPoseSample latestSample = FR5SdkPoseSample.CreateInvalid("CSharpSdk");
    private string lastCommandMessage = "No command";
    private int commandSequence = 0;
    private string lastCommandName = "NONE";
    private string lastCommandTimeLabel = "--:--:--";
    private bool lastCommandSucceeded = false;

    public bool IsConnected => hardwareAdapter != null && hardwareAdapter.IsConnected();

    private void Awake()
    {
        hardwareAdapter = new FR5HardwareAdapterStub();
        hardwareAdapter.SetCommandSpeedPercent(defaultCommandSpeedPercent);
    }

    private void Start()
    {
        if (autoConnectOnStart)
        {
            Connect();
        }
    }

    public bool Connect()
    {
        if (hardwareAdapter == null)
        {
            Debug.LogError("[FR5CSharpSdkClient] Hardware adapter is null.");
            return false;
        }

        bool connected = hardwareAdapter.Connect(robotIp);

        if (verboseLog)
        {
            Debug.Log($"[FR5CSharpSdkClient] Connect result: {connected} | IP={robotIp}");
        }

        return connected;
    }

    public void Disconnect()
    {
        if (hardwareAdapter == null) return;
        hardwareAdapter.Disconnect();

        if (verboseLog)
        {
            Debug.Log("[FR5CSharpSdkClient] Disconnected.");
        }
    }

    public bool PollLatestState()
    {
        if (hardwareAdapter == null || !hardwareAdapter.IsConnected())
        {
            return false;
        }

        bool ok = hardwareAdapter.TryReadCurrentState(out FR5SdkPoseSample sample);

        if (!ok || sample == null || !sample.isValid)
        {
            return false;
        }

        latestSample = sample.Clone();
        return true;
    }

    public bool TryGetLatestSample(out FR5SdkPoseSample sample)
    {
        if (latestSample == null)
        {
            sample = FR5SdkPoseSample.CreateInvalid("CSharpSdk");
            return false;
        }

        sample = latestSample.Clone();
        return sample.isValid;
    }

    public int GetCommandSpeedPercent()
    {
        return hardwareAdapter != null ? hardwareAdapter.GetCommandSpeedPercent() : defaultCommandSpeedPercent;
    }

    public string GetFormattedCommandSpeedLabel()
    {
        return $"{GetCommandSpeedPercent()}%";
    }

    public string GetLastCommandMessage()
    {
        return lastCommandMessage;
    }

    public string GetLastCommandName()
    {
        return lastCommandName;
    }

    public string GetLastCommandTimeLabel()
    {
        return lastCommandTimeLabel;
    }

    public bool GetLastCommandSucceeded()
    {
        return lastCommandSucceeded;
    }

    public string GetLastCommandSummary()
    {
        return $"#{commandSequence} {lastCommandName} | {GetFormattedCommandSpeedLabel()}";
    }

    public void SetCommandSpeedPercent(int speedPercent)
    {
        if (hardwareAdapter == null)
        {
            lastCommandMessage = "Hardware adapter is null.";
            Debug.LogError("[FR5CSharpSdkClient] " + lastCommandMessage);
            return;
        }

        hardwareAdapter.SetCommandSpeedPercent(speedPercent);
    }

    public void AdjustCommandSpeedPercent(int deltaPercent)
    {
        SetCommandSpeedPercent(Mathf.Clamp(GetCommandSpeedPercent() + deltaPercent, 1, 100));
    }

    public bool SendMoveJ(float[] jointDegrees)
    {
        lastCommandName = "MOVE J";
        return Execute(() => hardwareAdapter.TrySendMoveJ(Normalize(jointDegrees), out lastCommandMessage));
    }

    public bool SendMoveL(float[] jointDegrees)
    {
        lastCommandName = "MOVE L";
        return Execute(() => hardwareAdapter.TrySendMoveL(Normalize(jointDegrees), out lastCommandMessage));
    }

    public bool SendMoveC(float[] jointDegrees)
    {
        lastCommandName = "MOVE C";
        return Execute(() => hardwareAdapter.TrySendMoveC(Normalize(jointDegrees), out lastCommandMessage));
    }

    public bool StopMotion()
    {
        lastCommandName = "STOP";
        return Execute(() => hardwareAdapter.TryStopMotion(out lastCommandMessage));
    }

    public bool PauseMotion()
    {
        lastCommandName = "PAUSE";
        return Execute(() => hardwareAdapter.TryPauseMotion(out lastCommandMessage));
    }

    public bool ResumeMotion()
    {
        lastCommandName = "RESUME";
        return Execute(() => hardwareAdapter.TryResumeMotion(out lastCommandMessage));
    }

    private bool Execute(System.Func<bool> fn)
    {
        if (hardwareAdapter == null)
        {
            lastCommandMessage = "Hardware adapter is null.";
            lastCommandSucceeded = false;
            lastCommandTimeLabel = System.DateTime.Now.ToString("HH:mm:ss");
            Debug.LogError("[FR5CSharpSdkClient] " + lastCommandMessage);
            return false;
        }

        bool ok = fn.Invoke();

        commandSequence++;
        lastCommandSucceeded = ok;
        lastCommandTimeLabel = System.DateTime.Now.ToString("HH:mm:ss");

        if (verboseLog)
        {
            Debug.Log("[FR5CSharpSdkClient] " + lastCommandMessage);
        }

        return ok;
    }

    private float[] Normalize(float[] source)
    {
        float[] result = new float[6];
        for (int i = 0; i < 6; i++)
        {
            result[i] = (source != null && i < source.Length) ? source[i] : 0f;
        }
        return result;
    }
}
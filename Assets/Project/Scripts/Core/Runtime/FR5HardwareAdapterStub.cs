using System.Text;
using UnityEngine;

public class FR5HardwareAdapterStub : IFR5HardwareAdapter
{
    private bool connected = false;
    private int commandSpeedPercent = 100;

    public bool Connect(string robotIp)
    {
        connected = true;
        Debug.Log($"[FR5HardwareAdapterStub] Connect | IP={robotIp}");
        return true;
    }

    public void Disconnect()
    {
        connected = false;
        Debug.Log("[FR5HardwareAdapterStub] Disconnect");
    }

    public bool IsConnected()
    {
        return connected;
    }

    public bool TryReadCurrentState(out FR5SdkPoseSample sample)
    {
        sample = FR5SdkPoseSample.CreateInvalid("CSharpSdkStub");
        return false;
    }

    public int GetCommandSpeedPercent()
    {
        return commandSpeedPercent;
    }

    public void SetCommandSpeedPercent(int speedPercent)
    {
        commandSpeedPercent = Mathf.Clamp(speedPercent, 1, 100);
        Debug.Log($"[FR5HardwareAdapterStub] Command Speed = {commandSpeedPercent}%");
    }

    public bool TrySendMoveJ(float[] jointDegrees, out string resultMessage)
    {
        resultMessage = BuildMotionMessage("MOVE J", jointDegrees);
        Debug.Log("[FR5HardwareAdapterStub] " + resultMessage);
        return true;
    }

    public bool TrySendMoveL(float[] jointDegrees, out string resultMessage)
    {
        resultMessage = BuildMotionMessage("MOVE L", jointDegrees);
        Debug.Log("[FR5HardwareAdapterStub] " + resultMessage);
        return true;
    }

    public bool TrySendMoveC(float[] jointDegrees, out string resultMessage)
    {
        resultMessage = BuildMotionMessage("MOVE C", jointDegrees);
        Debug.Log("[FR5HardwareAdapterStub] " + resultMessage);
        return true;
    }

    public bool TryStopMotion(out string resultMessage)
    {
        resultMessage = $"DRY RUN | STOP | Speed={commandSpeedPercent}% | Connected={connected}";
        Debug.Log("[FR5HardwareAdapterStub] " + resultMessage);
        return true;
    }

    public bool TryPauseMotion(out string resultMessage)
    {
        resultMessage = $"DRY RUN | PAUSE | Speed={commandSpeedPercent}% | Connected={connected}";
        Debug.Log("[FR5HardwareAdapterStub] " + resultMessage);
        return true;
    }

    public bool TryResumeMotion(out string resultMessage)
    {
        resultMessage = $"DRY RUN | RESUME | Speed={commandSpeedPercent}% | Connected={connected}";
        Debug.Log("[FR5HardwareAdapterStub] " + resultMessage);
        return true;
    }

    private string BuildMotionMessage(string commandName, float[] jointDegrees)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append($"DRY RUN | {commandName} | Speed={commandSpeedPercent}% | Connected={connected} | Target=[");

        for (int i = 0; i < 6; i++)
        {
            float value = (jointDegrees != null && i < jointDegrees.Length) ? jointDegrees[i] : 0f;
            sb.Append(value.ToString("0.0"));
            if (i < 5) sb.Append(", ");
        }

        sb.Append("]");
        return sb.ToString();
    }
}
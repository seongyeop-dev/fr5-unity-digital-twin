public interface IFR5HardwareAdapter
{
    bool Connect(string robotIp);
    void Disconnect();
    bool IsConnected();
    bool TryReadCurrentState(out FR5SdkPoseSample sample);

    int GetCommandSpeedPercent();
    void SetCommandSpeedPercent(int speedPercent);

    bool TrySendMoveJ(float[] jointDegrees, out string resultMessage);
    bool TrySendMoveL(float[] jointDegrees, out string resultMessage);
    bool TrySendMoveC(float[] jointDegrees, out string resultMessage);

    bool TryStopMotion(out string resultMessage);
    bool TryPauseMotion(out string resultMessage);
    bool TryResumeMotion(out string resultMessage);
}
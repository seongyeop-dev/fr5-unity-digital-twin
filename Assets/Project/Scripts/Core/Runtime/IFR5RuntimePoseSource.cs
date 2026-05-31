public interface IFR5RuntimePoseSource
{
    bool IsConnected { get; }

    bool Connect();

    void Disconnect();

    bool PollLatestState();

    bool TryGetLatestSample(out FR5SdkPoseSample sample);
}
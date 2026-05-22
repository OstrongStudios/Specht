namespace Specht.Core.Services;

public interface IDiscoveryService
{
    void Start();
    void Stop();
    void Refresh();
    bool IsRunning { get; }
}

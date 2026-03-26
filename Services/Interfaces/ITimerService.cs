namespace WallpaperManager.Services.Interfaces;

public interface ITimerService : IDisposable
{
    void Start(TimeSpan interval);
    void Stop();
    void SetInterval(TimeSpan interval);
    bool IsRunning { get; }
    TimeSpan Interval { get; }
    event EventHandler? Tick;
}

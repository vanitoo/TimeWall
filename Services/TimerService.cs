using Serilog;
using WallpaperManager.Services.Interfaces;

namespace WallpaperManager.Services;

public class TimerService : ITimerService, IDisposable
{
    private System.Timers.Timer? _timer;
    private TimeSpan _interval;
    private bool _disposed;

    public bool IsRunning => _timer?.Enabled ?? false;

    public TimeSpan Interval
    {
        get => _interval;
        set
        {
            _interval = value;
            if (_timer != null)
            {
                _timer.Interval = value.TotalMilliseconds;
            }
        }
    }

    public event EventHandler? Tick;

    public TimerService()
    {
        _interval = TimeSpan.FromHours(2);
    }

    public void Start(TimeSpan interval)
    {
        Stop();

        _interval = interval;
        _timer = new System.Timers.Timer(interval.TotalMilliseconds);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
        _timer.Start();

        Log.Information("Timer started with interval: {Interval}", interval);
    }

    public void Stop()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Elapsed -= OnTimerElapsed;
            _timer.Dispose();
            _timer = null;
            Log.Information("Timer stopped");
        }
    }

    public void SetInterval(TimeSpan interval)
    {
        _interval = interval;
        if (_timer != null)
        {
            _timer.Interval = interval.TotalMilliseconds;
            Log.Debug("Timer interval changed to: {Interval}", interval);
        }
    }

    private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            Log.Debug("Timer tick");
            Tick?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in timer tick handler");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
    }
}

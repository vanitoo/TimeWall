namespace WallpaperManager.Helpers;

public sealed class AsyncLock
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Task<IDisposable> _releaser;

    public AsyncLock()
    {
        _releaser = Task.FromResult((IDisposable)new Releaser(this));
    }

    public IDisposable Lock()
    {
        _semaphore.Wait();
        return new Releaser(this);
    }

    public async Task<IDisposable> LockAsync()
    {
        await _semaphore.WaitAsync();
        return new Releaser(this);
    }

    public bool IsLocked => _semaphore.CurrentCount == 0;

    private sealed class Releaser : IDisposable
    {
        private readonly AsyncLock _lock;

        internal Releaser(AsyncLock lock_)
        {
            _lock = lock_;
        }

        public void Dispose()
        {
            _lock._semaphore.Release();
        }
    }
}

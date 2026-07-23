namespace ComfortScreen.Infrastructure;

internal enum SingleInstanceStartResult
{
    Primary,
    Secondary
}

internal sealed class SingleInstanceCoordinator : IDisposable
{
    private readonly string _mutexName;
    private readonly string _pipeName;
    private readonly Action<Exception> _logException;
    private readonly TimeSpan _notificationTimeout;
    private readonly ManualResetEventSlim _releaseMutex = new(false);
    private Thread? _mutexThread;
    private int _started;
    private int _disposed;

    internal SingleInstanceCoordinator(
        string mutexName,
        string pipeName,
        Action<Exception>? logException = null,
        TimeSpan? notificationTimeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mutexName);
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);

        _mutexName = mutexName;
        _pipeName = pipeName;
        _logException = logException ?? (_ => { });
        _notificationTimeout = notificationTimeout ?? TimeSpan.FromMilliseconds(500);
    }

    internal event EventHandler? ActivationRequested;

    internal SingleInstanceStartResult Start(bool notifyPrimary)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            throw new InvalidOperationException("The single-instance coordinator has already been started.");
        }

        _ = notifyPrimary;
        _ = _pipeName;
        _ = _notificationTimeout;
        _ = ActivationRequested;

        return AcquireMutexOwnership()
            ? SingleInstanceStartResult.Primary
            : SingleInstanceStartResult.Secondary;
    }

    private bool AcquireMutexOwnership()
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _mutexThread = new Thread(() => RunMutexOwner(completion))
        {
            IsBackground = true,
            Name = "ComfortScreen single-instance mutex owner"
        };
        _mutexThread.Start();
        return completion.Task.GetAwaiter().GetResult();
    }

    private void RunMutexOwner(TaskCompletionSource<bool> completion)
    {
        Mutex? mutex = null;
        var ownsMutex = false;

        try
        {
            mutex = new Mutex(initiallyOwned: false, _mutexName);
            try
            {
                ownsMutex = mutex.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                ownsMutex = true;
            }

            completion.TrySetResult(ownsMutex);
            if (ownsMutex)
            {
                _releaseMutex.Wait();
                mutex.ReleaseMutex();
            }
        }
        catch (Exception ex)
        {
            if (!completion.TrySetException(ex))
            {
                _logException(ex);
            }
        }
        finally
        {
            mutex?.Dispose();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _releaseMutex.Set();
        _mutexThread?.Join();
        _releaseMutex.Dispose();
    }
}

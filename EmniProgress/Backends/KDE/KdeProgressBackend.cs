using EmniProgress.Core;

namespace EmniProgress.Backends.KDE;

public class KdeProgressBackend : IProgressBackend
{
    private KdeJob? _job;
    private bool _disposed;

    /// <inheritdoc/>
    public async Task StartAsync(string title, string description, string appName = "", string? iconName = null)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(KdeProgressBackend));
        if (_job != null) throw new InvalidOperationException("A progress notification is already running.");

        _job = await KdeJob.StartAsync(title, description, appName, iconName).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(float value, string? message = null)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(KdeProgressBackend));
        if (_job == null) throw new InvalidOperationException("No progress notification is running.");

        int percent = (int)Math.Round(value);
        await _job.UpdatePercentAsync(percent);
        if (message != null)
        {
            await _job.SetInfoAsync(message);
        }
    }

    /// <inheritdoc/>
    public async Task FinishAsync(bool success = true, string? message = null)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(KdeProgressBackend));
        if (_job == null) throw new InvalidOperationException("No progress notification is running.");

        if (!success)
        {
            await _job.FailAsync(message ?? string.Empty);
        }
        else
        {
            await _job.FinishAsync(message);
        }
        
        await _job.DisposeAsync();
        _job = null;
    }
    
    /// <inheritdoc/>
    /// <remarks>
    /// Note that in KDE's job system, cancelling will be silent and won't show a failure message.
    /// </remarks>
    public async Task CancelAsync(string? message = null)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(KdeProgressBackend));
        if (_job == null) throw new InvalidOperationException("No progress notification is running.");

        await _job.FailAsync(message ?? "Cancelled", silent: true);
        await _job.DisposeAsync();
        _job = null;
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;

        if (_job != null)
        {
            _job.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _job = null;
        }
        return ValueTask.CompletedTask;
    }
}
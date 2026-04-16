using EmniProgress.Core;

namespace EmniProgress.Backends.KDE;

public class KdeProgressBackend : IProgressBackend
{
    private KdeJob? _job;
    private bool _disposed;

    private int _queuedCapabilities = 0;
    private Func<Task>? _queuedOnCancelRequested = null;
    private Func<Task>? _queuedOnSuspendRequested = null;
    private Func<Task>? _queuedOnResumeRequested = null;


    /*/// <summary>
    /// Sets the callback to be invoked when the user requests cancellation of the job.
    /// </summary>
    /// <remarks>
    /// This MUST be called before StartAsync to have an effect.
    /// </remarks>
    /// <param name="onCancelRequested">The callback to invoke when cancellation is requested. Pass null to disable cancellation.</param>
    /// <exception cref="ObjectDisposedException"></exception>
    public void OnCancel(Func<Task>? onCancelRequested)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(KdeProgressBackend));

        _queuedOnCancelRequested = onCancelRequested;

        int newCapabilities = _queuedOnCancelRequested != null
            ? _queuedCapabilities | KdeJobCapabilities.Killable
            : _queuedCapabilities & ~KdeJobCapabilities.Killable;
        _queuedCapabilities = newCapabilities;
    }

    /// <summary>
    /// Sets the callback to be invoked when the user requests suspension of the job.
    /// </summary>
    /// <remarks>
    /// This MUST be called before StartAsync to have an effect.
    /// </remarks>
    /// <param name="onSuspendRequested">The callback to invoke when suspension is requested. Pass null to disable suspension.</param>
    /// <exception cref="ObjectDisposedException"></exception>
    public void OnSuspend(Func<Task>? onSuspendRequested)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(KdeProgressBackend));

        _queuedOnSuspendRequested = onSuspendRequested;

        int newCapabilities = _queuedOnSuspendRequested != null
            ? _queuedCapabilities | KdeJobCapabilities.Suspendable
            : _queuedCapabilities & ~KdeJobCapabilities.Suspendable;
        _queuedCapabilities = newCapabilities;
    }

    /// <summary>
    /// Sets the callback to be invoked when the user requests resumption of the job.
    /// </summary>
    /// <remarks>
    /// This MUST be called before StartAsync to have an effect.
    /// </remarks>
    /// <param name="onResumeRequested">The callback to invoke when resumption is requested. Pass null to disable resumption.</param>
    /// <exception cref="ObjectDisposedException"></exception>
    public void OnResume(Func<Task>? onResumeRequested)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(KdeProgressBackend));

        _queuedOnResumeRequested = onResumeRequested;

        int newCapabilities = _queuedOnResumeRequested != null
            ? _queuedCapabilities | KdeJobCapabilities.Suspendable
            : _queuedCapabilities & ~KdeJobCapabilities.Suspendable;
        _queuedCapabilities = newCapabilities;
    }*/

    /// <inheritdoc/>
    public async Task StartAsync(string title, string description, string appName = "", string? iconName = null)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(KdeProgressBackend));
        if (_job != null) throw new InvalidOperationException("A progress notification is already running.");
        
        _job = await KdeJob.StartAsync(title, description, appName, iconName, _queuedCapabilities, _queuedOnCancelRequested, _queuedOnSuspendRequested, _queuedOnResumeRequested);
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

    /// <summary>
    /// Updates the progress notification with a set of properties.
    /// </summary>
    /// <param name="properties">The properties to update for the progress notification.</param>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task UpdateAsync(IDictionary<string, object> properties)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(KdeProgressBackend));
        if (_job == null) throw new InvalidOperationException("No progress notification is running.");

        await _job.UpdateAsync(properties);
    }
    
    /// <summary>
    /// Updates the total and processed amounts for the progress notification, along with an optional <see cref="KdeJobUnit"/> unit.
    /// </summary>
    /// <param name="total">The total amount of work to be done.</param>
    /// <param name="processed">The amount of work that has been processed so far.</param>
    /// <param name="unit">The unit of measurement for the amounts (e.g. "files", "bytes"). Optional.</param>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task UpdateAmountAsync(ulong total, ulong processed, string unit = "")
    {
        if (_disposed) throw new ObjectDisposedException(nameof(KdeProgressBackend));
        if (_job == null) throw new InvalidOperationException("No progress notification is running.");

        await _job.SetTotalAmountAsync(total, unit);
        await _job.SetProcessedAmountAsync(processed, unit);
    }
    
    /// <summary>
    /// Updates the speed of the progress notification in bytes per second.
    /// </summary>
    /// <param name="bytesPerSecond">The speed in bytes per second.</param>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task UpdateSpeedAsync(ulong bytesPerSecond)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(KdeProgressBackend));
        if (_job == null) throw new InvalidOperationException("No progress notification is running.");

        await _job.SetSpeedAsync(bytesPerSecond);
    }
    
    /// <summary>
    /// Updates a description field for the progress notification.
    /// KDE's job system supports multiple description fields, each identified by a number.
    /// The meaning of each field is determined by the application and KDE will display them in the order of their numbers.
    /// For example, a file manager might use field 1 for the source path and field 2 for the destination path when copying files.
    /// </summary>
    /// <param name="number">The number identifying the description field to update. Must be greater than 0.</param>
    /// <param name="name">The key of the description field.</param>
    /// <param name="value">The value to set for the description field.</param>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task UpdateDescriptionFieldAsync(uint number, string name, string value)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(KdeProgressBackend));
        if (_job == null) throw new InvalidOperationException("No progress notification is running.");

        await _job.SetDescriptionFieldAsync(number, name, value);
    }
    
    /// <summary>
    /// Clears a description field for the progress notification.
    /// </summary>
    /// <param name="number">The number identifying the description field to clear. Must be greater than 0.</param>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task ClearDescriptionFieldAsync(uint number)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(KdeProgressBackend));
        if (_job == null) throw new InvalidOperationException("No progress notification is running.");

        await _job.ClearDescriptionFieldAsync(number);
    }
    
    /// <summary>
    /// Sets whether the job is suspended or not. A suspended job is paused and can be resumed by the user.
    /// </summary>
    /// <param name="suspended">Whether the job should be suspended or not.</param>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task SetSuspendedAsync(bool suspended)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(KdeProgressBackend));
        if (_job == null) throw new InvalidOperationException("No progress notification is running.");

        await _job.SetSuspendedAsync(suspended);
    }
    
    /// <summary>
    /// Sets the destination URL for the job. This is typically used for file operations to indicate the destination path.
    /// </summary>
    /// <param name="destUrl">The destination URL to set for the job.</param>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task SetDestUrlAsync(string destUrl)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(KdeProgressBackend));
        if (_job == null) throw new InvalidOperationException("No progress notification is running.");

        await _job.SetDestUrlAsync(destUrl);
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
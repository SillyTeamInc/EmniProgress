using EmniProgress.Core;

namespace EmniProgress.Backends;

/// <summary>
/// A progress backend that forwards calls to multiple backends.
/// Useful for showing progress in multiple places at once, like in a notification and in the console.
/// </summary>
/// <param name="backends">The backends to forward calls to.</param>
public class CompositeProgressBackend(params IProgressBackend[] backends) : IProgressBackend
{
    /// <inheritdoc/>
    public Task StartAsync(string title, string description, string appName = "", string iconName = "") =>
        Task.WhenAll(backends.Select(b => b.StartAsync(title, description, appName, iconName)));
    /// <inheritdoc/>
    public Task UpdateAsync(float value, string? message = null) =>
        Task.WhenAll(backends.Select(b => b.UpdateAsync(value, message)));
    /// <inheritdoc/>
    public Task FinishAsync(bool success = true, string? message = null) =>
        Task.WhenAll(backends.Select(b => b.FinishAsync(success, message)));
    
    /// <summary>
    /// Gets the first backend of the specified type, or null if no such backend exists.
    /// </summary>
    /// <typeparam name="T">The type of the backend to get.</typeparam>
    /// <returns>The first backend of the specified type, or null if no such backend exists.</returns>
    public T? GetBackend<T>() where T : class, IProgressBackend
    {
        return backends.OfType<T>().FirstOrDefault();
    }

    /// <inheritdoc/>
    public Task CancelAsync(string? message = null)
    {
        return Task.WhenAll(backends.Select(b => b.CancelAsync(message)));
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(backends.Select(b => b.DisposeAsync().AsTask()));
    }
}
using EmniProgress.Core;

namespace EmniProgress.Backends;

/// <summary>
/// A progress backend that forwards calls to multiple backends.
/// Useful for showing progress in multiple places at once, like in a notification and in the console.
/// </summary>
/// <param name="backends">The backends to forward calls to.</param>
public class CompositeProgressBackend(params IProgressBackend[] backends) : IProgressBackend
{
    public Task StartAsync(string title, string description, string appName = "", string iconName = "") =>
        Task.WhenAll(backends.Select(b => b.StartAsync(title, description, appName, iconName)));

    public Task UpdateAsync(float value, string? message = null) =>
        Task.WhenAll(backends.Select(b => b.UpdateAsync(value, message)));

    public Task FinishAsync(bool success = true, string? message = null) =>
        Task.WhenAll(backends.Select(b => b.FinishAsync(success, message)));

    public Task CancelAsync(string? message = null)
    {
        return Task.WhenAll(backends.Select(b => b.CancelAsync(message)));
    }

    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(backends.Select(b => b.DisposeAsync().AsTask()));
    }
}
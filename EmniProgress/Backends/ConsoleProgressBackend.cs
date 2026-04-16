using EmniProgress.Core;

namespace EmniProgress.Backends;

public sealed class ConsoleProgressBackend : IProgressBackend
{
    private string _title = "";

    /// <inheritdoc/>
    public Task StartAsync(string title, string description, string appName = "", string iconPath = "")
    {
        _title = title;
        Console.WriteLine($"[{appName}] {title}{(string.IsNullOrEmpty(description) ? "" : $": {description}")}");
        return Task.CompletedTask;
    }
    
    private string _previousUpdate = "";

    /// <inheritdoc/>
    public Task UpdateAsync(float value, string? message = null)
    {
        int filled = (int)(value / 5);
        var bar = new string('█', filled) + new string('░', 20 - filled);
        string clear = new string(' ', _previousUpdate.Length);
        _previousUpdate = $"[{bar}] {value:F0}%  {message ?? _title}   ";
        Console.Write($"\r{clear}\r{_previousUpdate}");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task FinishAsync(bool success = true, string? message = null)
    {
        Console.WriteLine($"\n{(success ? "✓" : "✗")} {message ?? _title}\n");
        return Task.CompletedTask;
    }
    
    /// <inheritdoc/>
    public Task CancelAsync(string? message = null)
    {
        Console.WriteLine($"\n! {message ?? _title}\n");
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
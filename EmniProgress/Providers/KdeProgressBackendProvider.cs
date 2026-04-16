using EmniProgress.Backends;
using EmniProgress.Backends.KDE;
using EmniProgress.Core;

namespace EmniProgress.Providers;

/// <inheritdoc />
public class KdeProgressBackendProvider : IProgressBackendProvider
{
    /// <inheritdoc />
    public int Priority => 20;

    /// <inheritdoc />
    public bool IsAvailable()
    {
        var desktop = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "";
        return desktop.Contains("KDE", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IProgressBackend Create() => new KdeProgressBackend();
}
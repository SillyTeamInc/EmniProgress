using EmniProgress.Backends;
using EmniProgress.Backends.KDE;
using EmniProgress.Core;

namespace EmniProgress.Providers;

public class KdeProgressBackendProvider : IProgressBackendProvider
{
    public int Priority => 20;

    public bool IsAvailable()
    {
        var desktop = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "";
        return desktop.Contains("KDE", StringComparison.OrdinalIgnoreCase);
    }

    public IProgressBackend Create() => new KdeProgressBackend();
}
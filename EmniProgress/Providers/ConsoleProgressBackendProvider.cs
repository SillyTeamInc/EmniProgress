using EmniProgress.Backends;
using EmniProgress.Core;

namespace EmniProgress.Providers;

public class ConsoleProgressBackendProvider : IProgressBackendProvider
{
    public int Priority => 0;

    public bool IsAvailable() => true;

    public IProgressBackend Create() => new ConsoleProgressBackend();
}
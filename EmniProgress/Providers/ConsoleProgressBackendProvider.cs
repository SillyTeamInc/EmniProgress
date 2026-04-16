using EmniProgress.Backends;
using EmniProgress.Core;

namespace EmniProgress.Providers;

/// <inheritdoc />
public class ConsoleProgressBackendProvider : IProgressBackendProvider
{
    /// <inheritdoc />
    public int Priority => 0;
    
    /// <inheritdoc />
    public bool IsAvailable() => true;
    
    /// <inheritdoc />
    public IProgressBackend Create() => new ConsoleProgressBackend();
}
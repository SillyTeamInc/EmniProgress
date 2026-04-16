using EmniProgress.Backends;
using EmniProgress.Core;
using EmniProgress.Providers;

namespace EmniProgress.Factory;

/// <summary>
/// A factory for creating progress backends based on the available providers and their priorities.
/// </summary>
public static class EmniFactory
{
    private static readonly List<IProgressBackendProvider> _providers = new()
    {
        new KdeProgressBackendProvider(),
        new ConsoleProgressBackendProvider(),
    };

    /// <summary>
    /// Registers a custom progress backend provider.
    /// If a provider of the same type already exists, it will be replaced.
    /// </summary>
    /// <param name="provider">The provider to register.</param>
    public static void Register(IProgressBackendProvider provider)
    {
        _providers.RemoveAll(p => p.GetType() == provider.GetType());
        _providers.Add(provider);
    }

    /// <summary>
    /// Creates a progress backend based on the available providers and their priorities.
    /// </summary>
    /// <returns>A composite progress backend that includes the preferred backend and the console backend as a fallback.</returns>
    public static IProgressBackend Create()
    {
        var preferred = _providers
            .OrderByDescending(p => p.Priority)
            .Where(p => p.Priority > 0)
            .FirstOrDefault(p => p.IsAvailable());

        var console = new ConsoleProgressBackend();

        if (preferred == null)
        {
            return new CompositeProgressBackend(console);
        }

        return new CompositeProgressBackend(preferred.Create(), console);
    }
}
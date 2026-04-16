using EmniProgress.Backends;
using EmniProgress.Core;
using EmniProgress.Providers;

namespace EmniProgress.Factory;

public static class EmniFactory
{
    private static readonly List<IProgressBackendProvider> _providers = new()
    {
        new KdeProgressBackendProvider(),
        new ConsoleProgressBackendProvider(),
    };

    public static void Register(IProgressBackendProvider provider) => _providers.Add(provider);

    public static IProgressBackend Create()
    {
        var preferred = _providers
            .OrderByDescending(p => p.Priority)
            .Where(p => p.Priority > 0)
            .FirstOrDefault(p => p.IsAvailable());

        var console = new ConsoleProgressBackend();

        if (preferred == null)
            return console;

        return new CompositeProgressBackend(preferred.Create(), console);
    }
}
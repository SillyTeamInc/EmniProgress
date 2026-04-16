namespace EmniProgress.Core;

public interface IProgressBackendProvider
{
    int Priority { get; }
    bool IsAvailable();
    IProgressBackend Create();
}
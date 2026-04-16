namespace EmniProgress.Core;

/// <summary>
/// Defines a provider for progress backends, allowing for dynamic selection based on availability and priority.
/// </summary>
public interface IProgressBackendProvider
{
    /// <summary>
    /// Gets the priority of the provider. Higher values indicate higher priority.
    /// </summary>
    int Priority { get; }
    /// <summary>
    /// Determines whether the provider is available for use. This can be used to check for necessary conditions, such as the presence of a specific desktop environment or library.
    /// </summary>
    /// <returns>True if the provider is available; otherwise, false.</returns>
    bool IsAvailable();
    
    /// <summary>
    /// Creates an instance of the progress backend provided by this provider.
    /// </summary>
    /// <returns>An instance of the progress backend.</returns>
    IProgressBackend Create();
}
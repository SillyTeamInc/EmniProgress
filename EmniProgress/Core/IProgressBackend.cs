namespace EmniProgress.Core;

public interface IProgressBackend : IAsyncDisposable
{
    /// <summary>
    /// Starts a new progress notification.
    /// </summary>
    /// <param name="title">The title of the progress notification.</param>
    /// <param name="description">The description of the progress notification.</param>
    /// <param name="appName">The name of the application to display in the progress notification. (Optional, may be ignored by some backends)</param>
    /// <param name="iconPath">The path to an icon to display in the progress notification. (Optional, may be ignored by some backends)</param>
    /// <returns>A task that represents the asynchronous operation of starting the progress notification.</returns>
    Task StartAsync(string title, string description, string appName = "", string iconPath = "");
    
    /// <summary>
    /// Updates the progress notification.
    /// </summary>
    /// <param name="value">The current value of the progress notification. (0 to 100)</param>
    /// <param name="message">An optional message to display with the progress notification.</param>
    /// <remarks>
    /// The value is a float between 0 and 100 representing the percentage of completion.
    /// This value may be rounded or clamped by the backend!
    /// </remarks>
    Task UpdateAsync(float value, string? message = null);
    
    /// <summary>
    /// Finishes the progress notification.
    /// </summary>
    /// <param name="success">Whether the operation was successful or not.</param>
    /// <param name="message">An optional message to display when finishing the progress notification.</param>
    Task FinishAsync(bool success = true, string? message = null);

    /// <summary>
    /// Cancels the progress notification.
    /// </summary>
    /// <param name="message">An optional message to display when cancelling the progress notification.</param>
    Task CancelAsync(string? message = null);
}


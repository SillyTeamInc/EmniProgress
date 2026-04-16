using Tmds.DBus;
namespace EmniProgress.Backends.KDE;
// ReSharper disable InconsistentNaming
[DBusInterface("org.kde.JobViewServerV2")]
public interface IJobViewServerV2 : IDBusObject
{
    Task<ObjectPath> requestViewAsync(string desktopEntry, int capabilities, IDictionary<string, object> hints);
}
[DBusInterface("org.kde.JobViewV2")]
public interface IJobViewV2 : IDBusObject
{
    Task setPercentAsync(uint percent);
    Task setInfoMessageAsync(string message);
}
[DBusInterface("org.kde.JobViewV3")]
public interface IJobViewV3 : IDBusObject
{
    Task terminateAsync(uint errorCode, string errorMessage, IDictionary<string, object> hints);
    Task updateAsync(IDictionary<string, object> properties);
}

public sealed class KdeJob : IAsyncDisposable
{
    private readonly Connection _connection;
    private readonly IJobViewV2 _jobV2;
    private readonly IJobViewV3 _jobV3;
    private bool _finished;

    private KdeJob(Connection connection, IJobViewV2 jobV2, IJobViewV3 jobV3)
    {
        _connection = connection;
        _jobV2 = jobV2;
        _jobV3 = jobV3;
    }

    public static async Task<KdeJob> StartAsync(string title, string description, string? appName = null, string? iconName = null)
    {
        var conn = new Connection(Address.Session);
        await conn.ConnectAsync().ConfigureAwait(false);

        const string kuiserver = "org.kde.kuiserver";
        const string jobService = "org.kde.JobViewServer";

        var serverV2 = conn.CreateProxy<IJobViewServerV2>(kuiserver, "/JobViewServer");
        var hints = new Dictionary<string, object>
        {
            { "title", title },
            { "application-display-name", appName ?? string.Empty },
            { "application-icon", iconName ?? string.Empty },
            { "description", description },
            { "percent", (uint)0 },     
            { "requires-job-tracker", true }
        };

        ObjectPath jobPath = await serverV2.requestViewAsync(
            appName ?? string.Empty,
            0,
            hints
        ).ConfigureAwait(false);

        var jobV2 = conn.CreateProxy<IJobViewV2>(jobService, jobPath);
        var jobV3 = conn.CreateProxy<IJobViewV3>(jobService, jobPath);

        return new KdeJob(conn, jobV2, jobV3);
    }

    public Task UpdatePercentAsync(int percent)
    {
        if (_finished) return Task.CompletedTask;
        percent = Math.Clamp(percent, 0, 100);
        return _jobV2.setPercentAsync((uint)percent);
    }

    public Task SetInfoAsync(string? info)
    {
        if (_finished) return Task.CompletedTask;
        return _jobV2.setInfoMessageAsync(info ?? string.Empty);
    }

    public Task FailAsync(string? errorMessage, bool silent = false)
    {
        if (_finished) return Task.CompletedTask;
        _finished = true;
        // this is stupid.
        uint code = (uint)(silent ? 1 : 2);
        return _jobV3.terminateAsync(code, errorMessage ?? string.Empty, new Dictionary<string, object>());
    }

    public Task FinishAsync(string? message = "")
    {
        if (_finished) return Task.CompletedTask;
        _finished = true;
        return _jobV3.terminateAsync(0, message ?? string.Empty, new Dictionary<string, object>());
    }

    public async ValueTask DisposeAsync()
    {
        if (!_finished)
        {
            try { await FinishAsync().ConfigureAwait(false); } catch { }
        }
        _connection?.Dispose();
    }
}
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
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
    Task setSpeedAsync(ulong bytesPerSecond);
    Task setTotalAmountAsync(ulong amount, string unit);
    Task setProcessedAmountAsync(ulong amount, string unit);
    Task setDestUrlAsync(object destUrl);
    Task<bool> setDescriptionFieldAsync(uint number, string name, string value);
    Task clearDescriptionFieldAsync(uint number);
    Task setSuspendedAsync(bool suspended);
}

[DBusInterface("org.kde.JobViewV3")]
public interface IJobViewV3 : IDBusObject
{
    Task terminateAsync(uint errorCode, string errorMessage, IDictionary<string, object> hints);
    Task updateAsync(IDictionary<string, object> properties);
}

/// <summary>
/// Units for KDE job amounts.
/// </summary>
public static class KdeJobUnit
{
    public const string Bytes = "bytes";
    public const string Files = "files";
    public const string Directories = "dirs";
    public const string Items = "items";
}

public static class KdeJobCapabilities
{
    public const int None = 0;
    public const int Killable = 1;
    public const int Suspendable = 2;
}

public sealed class KdeJob : IAsyncDisposable
{
    private readonly Connection _connection;
    private readonly IJobViewV2 _jobV2;
    private readonly IJobViewV3 _jobV3;
    private bool _finished;

    private IDisposable? _suspendSub;
    private IDisposable? _resumeSub;
    private IDisposable? _cancelSub;

    private Func<Task>? OnSuspendRequested { get; set; }
    private Func<Task>? OnResumeRequested { get; set; }
    private Func<Task>? OnCancelRequested { get; set; }

    private KdeJob(Connection connection, IJobViewV2 jobV2, IJobViewV3 jobV3)
    {
        _connection = connection;
        _jobV2 = jobV2;
        _jobV3 = jobV3;
    }

    /// <summary>
    /// Starts a new KDE job with the given parameters. The returned KdeJob object can be used to update or finish the job.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="description"></param>
    /// <param name="appName"></param>
    /// <param name="iconName"></param>
    /// <param name="capabilities">The capabilities of the job, a bitmask of <see cref="KdeJobCapabilities"/> values.</param>
    /// <param name="onSuspendRequested"></param>
    /// <param name="onResumeRequested"></param>
    /// <param name="onCancelRequested"></param>
    /// <returns></returns>
    public static async Task<KdeJob> StartAsync(
        string title,
        string description,
        string? appName = null,
        string? iconName = null,
        int capabilities = 0,
        Func<Task>? onSuspendRequested = null,
        Func<Task>? onResumeRequested = null,
        Func<Task>? onCancelRequested = null)
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
            { "transient", false },
        };

        ObjectPath jobPath = await serverV2.requestViewAsync(
            appName ?? string.Empty,
            capabilities,
            hints
        ).ConfigureAwait(false);
    
        string uniqueOwner = await conn.ResolveServiceOwnerAsync(jobService).ConfigureAwait(false);

        var jobV2 = conn.CreateProxy<IJobViewV2>(uniqueOwner, jobPath);
        var jobV3 = conn.CreateProxy<IJobViewV3>(uniqueOwner, jobPath);
        
        
        
        var job = new KdeJob(conn, jobV2, jobV3)
        {
            OnSuspendRequested = onSuspendRequested,
            OnResumeRequested = onResumeRequested,
            OnCancelRequested = onCancelRequested,
        };
        
        Debug.WriteLine($"[emni] KDE Job created at {jobPath} with service {uniqueOwner}");
        
        // if someone knows a better way of doing this please contact me 
        // https://ratted.systems/emi
        job._cancelSub = await WatchRawSignalAsync(conn, jobPath,
            "org.kde.JobViewV2", "cancelRequested", uniqueOwner,
            () =>
            {         
                Debug.WriteLine("[emni] Cancel requested");
                if (job.OnCancelRequested != null)
                    _ = job.OnCancelRequested.Invoke();
            }).ConfigureAwait(false);
        
        job._suspendSub = await WatchRawSignalAsync(conn, jobPath,
            "org.kde.JobViewV2", "suspendRequested", uniqueOwner,
            () =>
            {
                Debug.WriteLine("[emni] Suspend requested");
                if (job.OnSuspendRequested != null) 
                    _ = job.OnSuspendRequested.Invoke();
            }).ConfigureAwait(false);   
        
        job._resumeSub = await WatchRawSignalAsync(conn, jobPath,
            "org.kde.JobViewV2", "resumeRequested", uniqueOwner,
            () =>
            {
                Debug.WriteLine("[emni] Resume requested");
                if (job.OnResumeRequested != null)      
                    _ = job.OnResumeRequested.Invoke();
            }).ConfigureAwait(false);
        
        return job;
    }
    
    private static readonly Type? _signalHandlerType = 
        typeof(Connection).Assembly.GetType("Tmds.DBus.Protocol.SignalHandler");
    private static readonly MethodInfo? _watchSignalMethod = 
        typeof(Connection).GetMethod("WatchSignalAsync", BindingFlags.Instance | BindingFlags.NonPublic);

    private static async Task<IDisposable> WatchRawSignalAsync(
        Connection conn,
        ObjectPath path,
        string iface,
        string signalName,
        string sender,
        Action onSignal)
    {
        // Tmds.DBus doesn't expose signal watching for remote objects publicly :c
        // So we have to do this cursed shit.
        if (_signalHandlerType == null)
            throw new InvalidOperationException("Tmds.DBus.Protocol.SignalHandler not found, library internals may have changed.");
        if (_watchSignalMethod == null)
            throw new InvalidOperationException("Connection.WatchSignalAsync not found, library internals may have changed.");

        var parameters = _signalHandlerType.GetMethod("Invoke")!
            .GetParameters()
            .Select(p => Expression.Parameter(p.ParameterType))
            .ToArray();

        var handler = Expression.Lambda(
            _signalHandlerType,
            Expression.Call(Expression.Constant(onSignal), typeof(Action).GetMethod("Invoke")!),
            parameters
        ).Compile();

        return await ((Task<IDisposable>)_watchSignalMethod.Invoke(conn,
                    new object[] { path, iface, signalName, sender, handler })!
            ).ConfigureAwait(false);
    }
    
    public Task UpdateAsync(IDictionary<string, object> properties)
    {
        if (_finished) return Task.CompletedTask;
        return _jobV3.updateAsync(properties);
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

    public Task SetSpeedAsync(ulong bytesPerSecond)
    {
        if (_finished) return Task.CompletedTask;
        return _jobV2.setSpeedAsync(bytesPerSecond);
    }

    public Task SetTotalAmountAsync(ulong amount, string unit = KdeJobUnit.Bytes)
    {
        if (_finished) return Task.CompletedTask;
        return _jobV2.setTotalAmountAsync(amount, unit);
    }
    
    public Task SetProcessedAmountAsync(ulong amount, string unit = KdeJobUnit.Bytes)
    {
        if (_finished) return Task.CompletedTask;
        return _jobV2.setProcessedAmountAsync(amount, unit);
    }

    public Task SetDestUrlAsync(string url)
    {
        if (_finished) return Task.CompletedTask;
        return _jobV2.setDestUrlAsync(url);
    }
    
    public Task SetDescriptionFieldAsync(uint row, string label, string value)
    {
        if (_finished) return Task.CompletedTask;
        if (row > 1) throw new ArgumentOutOfRangeException(nameof(row), "KDE only supports description rows 0 and 1.");
        return _jobV2.setDescriptionFieldAsync(row, label, value);
    }

    public Task ClearDescriptionFieldAsync(uint row)
    {
        if (_finished) return Task.CompletedTask;
        return _jobV2.clearDescriptionFieldAsync(row);
    }

    public Task SetSuspendedAsync(bool suspended)
    {
        if (_finished) return Task.CompletedTask;
        return _jobV2.setSuspendedAsync(suspended);
    }

    public Task FailAsync(string? errorMessage, bool silent = false)
    {
        if (_finished) return Task.CompletedTask;
        _finished = true;
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
        /*_suspendSub?.Dispose();
        _resumeSub?.Dispose();
        _cancelSub?.Dispose();*/

        if (!_finished)
        {
            try { await FinishAsync().ConfigureAwait(false); } catch { }
        }
        _connection?.Dispose();
    }
}
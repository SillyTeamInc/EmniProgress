using System.Diagnostics;
using EmniProgress;
using EmniProgress.Backends;
using EmniProgress.Backends.KDE;
using EmniProgress.Factory;
using Tmds.DBus;

namespace Example;

public class Program
{
    static async Task Main(string[] args)
    {
        await using var progress = (CompositeProgressBackend)EmniFactory.Create();

        await progress.StartAsync("Example", "Progress", "ExampleApp");
        await Task.Delay(1000);
        for (int i = 0; i <= 99; i++)
        {
            progress.GetBackend<KdeProgressBackend>()?.SetDestUrlAsync($"https://ratted.systems/emi");
            await progress.UpdateAsync(i, $"Uploading");
            await Task.Delay(50);
        }

        await progress.UpdateAsync(99, "Uploaded!");
        await progress.FinishAsync(true, "Done");
    }
}
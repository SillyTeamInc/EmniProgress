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

        await progress.StartAsync("Uploading", "Progress", "ExampleApp", "system-software-update");
        await Task.Delay(3000);
        for (int i = 0; i <= 99; i++)
        {
            KdeProgressBackend? kde = progress.GetBackend<KdeProgressBackend>();
            kde?.SetDestUrlAsync($"https://ratted.systems/u/fmsO87.txt");
            var updates = new Dictionary<string, object>
            {
                { "percent", (uint)i },
                { "infoMessage", "to https://ratted.systems/u/fmsO87.txt" },
                { "speed", 1024000uL } // 1 MB/s
            };
            await kde?.UpdateAsync(updates)!;
            await Task.Delay(200);
        }

        progress.GetBackend<KdeProgressBackend>()?.SetDestUrlAsync($"file:///home/emi/Desktop");
        await progress.UpdateAsync(100, "Uploaded!");
        await progress.FinishAsync(true, "Copied to clipboard!");
    }
}
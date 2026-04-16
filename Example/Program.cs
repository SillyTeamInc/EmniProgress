using System.Diagnostics;
using EmniProgress;
using EmniProgress.Backends;
using EmniProgress.Backends.KDE;
using EmniProgress.Factory;
using Tmds.DBus;

namespace Example;

public class Program
{
    private static bool Cancelled = false;
    static async Task Main(string[] args)
    {
        await using var progress = (CompositeProgressBackend)EmniFactory.Create();
        var kde = progress.GetBackend<KdeProgressBackend>();
        kde?.OnCancel(() =>
        {
            Cancelled = true;
            return Task.CompletedTask;
        });
        
        await progress.StartAsync("Uploading", "Progress", "ExampleApp", "system-software-update");
        await Task.Delay(3000);
        for (int i = 0; i <= 99; i++)
        {
            kde?.SetDestUrlAsync($"https://ratted.systems/u/fmsO87.txt");
            await progress.UpdateAsync(i, $"Uploading");
            await Task.Delay(200);
            if (Cancelled)
            {
                await progress.CancelAsync("Upload cancelled by user.");
                return;
            }
        }

        progress.GetBackend<KdeProgressBackend>()?.SetDestUrlAsync($"file:///home/emi/Desktop");
        await progress.UpdateAsync(100, "Uploaded!");
        await progress.FinishAsync(true, "Copied to clipboard!");
    }
}
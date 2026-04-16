using EmniProgress;
using EmniProgress.Factory;
using Tmds.DBus;

namespace Example;

public class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            await using var progress = EmniFactory.Create();

            await progress.StartAsync("Uploading file...", "AfngIj.png", "RattedSystemsCli");
            for (int i = 0; i <= 60; i++)
            {
                await progress.UpdateAsync(i, $"Uploading...");
                await Task.Delay(50);
            }

            await progress.FinishAsync(success: false, "Socket was disconnected!");

            await progress.StartAsync("Uploading file...", "NGg5zg1.png", "RattedSystemsCli");
            for (int i = 0; i <= 60; i++)
            {
                await progress.UpdateAsync(i, $"Uploading...");
                await Task.Delay(50);
            }

            await progress.CancelAsync("Upload cancelled by user!");


            await progress.StartAsync("Uploading file...", "SDju7fz.png", "RattedSystemsCli");
            for (int i = 0; i <= 99; i++)
            {
                await progress.UpdateAsync(i, $"Uploading...");
                await Task.Delay((int)(35));
            }

            await progress.UpdateAsync(100, "Uploaded!");
            await progress.FinishAsync(success: true, "Copied upload to clipboard!");


        }
        catch (Exception e)
        {
            Console.WriteLine("An error occurred: " + e);
        }

    }
}
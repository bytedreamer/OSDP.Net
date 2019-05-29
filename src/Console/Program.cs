using System;
using System.Threading;
using System.Threading.Tasks;
using OSDP.Net;

namespace Console
{
    class Program
    {
        static async Task Main(string[] args) 
        {
            var controlPanel = new ControlPanel(new SerialPortOsdpConnection());

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var keyBoardTask = Task.Run(() =>
                {
                    System.Console.WriteLine("Press enter to cancel");
                    System.Console.ReadKey();

                    // Cancel the task
                    cancellationTokenSource.Cancel();
                });
                try
                {
                    var longRunningTask = controlPanel.StartPolling(0, cancellationTokenSource.Token);

                    await longRunningTask;
                    System.Console.WriteLine("Press enter to continue");
                }
                catch (TaskCanceledException)
                {
                    System.Console.WriteLine("Task was cancelled");
                }

                await keyBoardTask;
            }

            controlPanel.Shutdown(); 
        }
    }
}


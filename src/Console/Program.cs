using System.Threading;
using System.Threading.Tasks;
using OSDP.Net;

namespace Console
{
    internal static class Program
    {
        static async Task Main() 
        {
            var controlPanel = new ControlPanel();

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var keyBoardTask = Task.Run(() =>
                {
                    System.Console.WriteLine("Press enter to shutdown bus");
                    System.Console.ReadKey();

                    // Cancel the task
                    // ReSharper disable once AccessToDisposedClosure
                    cancellationTokenSource.Cancel();
                });
                
                try
                {
                    var longRunningTask = controlPanel.AddBus(new Bus(new SerialPortOsdpConnection()), cancellationTokenSource.Token);

                    await longRunningTask;
                    System.Console.WriteLine("Press enter to continue");
                }
                catch (TaskCanceledException)
                {
                    System.Console.WriteLine("Shutting down bus");
                }

                await keyBoardTask;
            }

            controlPanel.Shutdown(); 
        }
    }
}


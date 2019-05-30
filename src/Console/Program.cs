using System.Threading;
using System.Threading.Tasks;
using OSDP.Net;

namespace Console
{
    class Program
    {
        static async Task Main(string[] args) 
        {
            var controlPanel = new ControlPanel();

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
                    var longRunningTask = controlPanel.AddBus(new Bus(new SerialPortOsdpConnection()), cancellationTokenSource.Token);

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


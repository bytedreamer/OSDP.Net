using Microsoft.Extensions.Configuration;
using OSDP.Net;
using OSDP.Net.Connections;
using OSDP.Net.Model.CommandData;

namespace PivDataReader;

internal class Program
{
    private static Guid _connectionId;

    private static async Task Main()
    {
        var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true);
        var config = builder.Build();
        
        var osdpSection = config.GetSection("OSDP");
        string portName = osdpSection["PortName"];
        int baudRate = int.Parse(osdpSection["BaudRate"]);
        byte deviceAddress = byte.Parse(osdpSection["DeviceAddress"]);
        bool useSecureChannel = bool.Parse(osdpSection["UseSecureChannel"]);
        byte[] securityKey = Convert.FromHexString(osdpSection["SecurityKey"]);
        byte maximumReceiveSize = byte.Parse(osdpSection["MaximumReceivedSized"]);
        bool useNetwork = bool.Parse(osdpSection["UseNetwork"]);
        string networkAddress = osdpSection["NetworkAddress"];
        int networkPort = int.Parse(osdpSection["NetworkPort"]);
        
        var pivDataSection = config.GetSection("PIVData");
        byte[] objectId = Convert.FromHexString(pivDataSection["ObjectId"]);
        byte elementId = Convert.FromHexString(pivDataSection["ElementId"])[0];
        ushort offset = ushort.Parse(pivDataSection["Offset"]);
        
        var panel = new ControlPanel();
        panel.ConnectionStatusChanged += async (_, eventArgs) =>
        {
            Console.WriteLine();
            Console.Write(
                $"Device is {(eventArgs.IsConnected ? "Online" : "Offline")} in {(eventArgs.IsSecureChannelEstablished ? "Secure" : "Clear Text")} mode");

            if (eventArgs.IsConnected)
            {
                await panel.ACUReceivedSize(_connectionId, deviceAddress, maximumReceiveSize);
            }
        };
        panel.RawCardDataReplyReceived += (_, eventArgs) =>
        {
            Console.WriteLine();
            Console.WriteLine("Received raw card data");
            Console.Write(eventArgs.RawCardData.ToString());
        };
        panel.NakReplyReceived += (_, args) =>
        {
            Console.WriteLine();
            Console.Write($"Received NAK {args.Nak}");
        };

        if (useNetwork)
        {
            _connectionId = panel.StartConnection(new TcpClientOsdpConnection(networkAddress, networkPort, baudRate)
                { ReplyTimeout = TimeSpan.FromSeconds(2) });
        }
        else
        {
            _connectionId = panel.StartConnection(new SerialPortOsdpConnection(portName, baudRate)
                { ReplyTimeout = TimeSpan.FromSeconds(2) });
        }
        panel.AddDevice(_connectionId, deviceAddress, true, useSecureChannel, securityKey);

        bool exit = false;

        while (!exit)
        {
            Console.WriteLine();
            Console.WriteLine("PIV Data Reader");
            Console.WriteLine();

            Console.WriteLine("1) Get PIV Data");
            
            Console.WriteLine();
            Console.WriteLine("0) Exit");
            Console.WriteLine();
            Console.Write("Select an action:");

            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.D1:
                    await GetPivData();
                    break;
                case ConsoleKey.D0:
                    exit = true;
                    break;
            }

            Console.WriteLine();

            if (!exit) Console.Clear();
        }

        panel.Shutdown();
        
        async Task GetPivData()
        {
            Console.Clear();
            Console.Write($"Enter the Object ID as a hex string [{BitConverter.ToString(objectId).Replace("-", string.Empty)}]: ");
            string? entry = Console.ReadLine();
            if (!string.IsNullOrEmpty(entry))
            {
                objectId = Convert.FromHexString(entry);
            }
            Console.Write($"Enter the Element ID as a hex value [{elementId:X}]: ");
            entry = Console.ReadLine();
            if (!string.IsNullOrEmpty(entry))
            {
                elementId = Convert.FromHexString(entry)[0];
            }
            Console.Write($"Enter the offset in decimal [{offset:F0}]: ");
            entry = Console.ReadLine();
            if (!string.IsNullOrEmpty(entry))
            {
                offset = ushort.Parse(entry);
            }

            Console.WriteLine();
            Console.Write("***Attempting to get PIV data***");
            Console.WriteLine();

            try
            {
                var data = await panel.GetPIVData(_connectionId, deviceAddress,
                    new GetPIVData(objectId, elementId, offset), TimeSpan.FromSeconds(30));
                await File.WriteAllBytesAsync("PivData.bin", data);
                Console.Write(BitConverter.ToString(data));
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Timeout waiting for PIV data");
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Exception: {exception}");
            }
            
            Console.WriteLine();
            Console.Write("Press enter to continue");
            Console.ReadLine();
        }
    }
}



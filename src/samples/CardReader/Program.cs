using System.Collections;
using System.Numerics;
using Microsoft.Extensions.Configuration;
using OSDP.Net.Connections;
using OSDP.Net.Model.ReplyData;

namespace CardReader;

internal class Program
{
    private static async Task Main()
    {
        var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true);
        var config = builder.Build();
        var osdpSection = config.GetSection("OSDP");

        var portName = osdpSection["PortName"];
        var baudRate = int.Parse(osdpSection["BaudRate"] ?? "9600");
        var deviceAddress = byte.Parse(osdpSection["DeviceAddress"] ?? "0");

        var connection = new SerialPortOsdpConnection(portName, baudRate);
        using var device = new MySampleDevice();
        device.StartListening(connection);

        await Task.Factory.StartNew(() =>
        {
            // The card format number for this example is 128 bit (as raw card data)
            const string cardNumberHexValue = "30313233343536373839303030303032";
            var cardNumberInt = BigInteger.Parse(cardNumberHexValue, System.Globalization.NumberStyles.HexNumber);
            var cardNumberBitString = BigIntegerToBinaryString(cardNumberInt).PadLeft(128, '0');
            var cardNumber = new BitArray(cardNumberBitString.Select(c => c == '1').ToArray());

            while (true)
            {
                // ReSharper disable once AccessToDisposedClosure
                if (!device.IsConnected) continue;

                Console.WriteLine($"Device is connected!\nPress any key to send card data.");
                Console.ReadKey();
                device.EnqueuePollReply(new RawCardData(deviceAddress, FormatCode.NotSpecified, cardNumber));
                Console.WriteLine($"Sent card data: {cardNumberHexValue}");
                return;
            }
        });

        Console.WriteLine("Press any key to finish the program.");
        Console.ReadKey();

        device.StopListening();
    }

    private static string BigIntegerToBinaryString(BigInteger value)
    {
        var result = "";

        while (value > 0)
        {
            result = value % 2 + result;
            value /= 2;
        }

        return string.IsNullOrEmpty(result) ? "0" : result;
    }
}

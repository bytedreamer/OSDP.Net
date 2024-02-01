using System.Collections;
using System.Collections.Concurrent;
using System.Numerics;
using Microsoft.Extensions.Configuration;
using OSDP.Net;
using OSDP.Net.Connections;
using OSDP.Net.Model;
using OSDP.Net.Model.ReplyData;

namespace CardReader;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true);
        var config = builder.Build();
        var osdpSection = config.GetSection("OSDP");

        var portName = osdpSection["PortName"];
        var baudRate = int.Parse(osdpSection["BaudRate"] ?? "9600");
        var deviceAddress = byte.Parse(osdpSection["DeviceAddress"] ?? "0");
        var readerNumber = byte.Parse(osdpSection["ReaderAddress"] ?? "0");
        var useSecureChannel = bool.Parse(osdpSection["UseSecureChannel"] ?? "False");
        var securityKey = Convert.FromHexString(osdpSection["SecurityKey"] ?? "[]");

        var outgoingReplies = new ConcurrentQueue<PayloadData>();
        var connection = new SerialPortOsdpConnection(portName, baudRate);
        using var device = new Device(deviceAddress, true, useSecureChannel, securityKey);
        device.StartListening(connection, new CommandProcessing(outgoingReplies));

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

                Console.WriteLine($"Device is connected!\nSending card data.");
                outgoingReplies.Enqueue(new RawCardData(readerNumber, FormatCode.NotSpecified, cardNumber));
                return;
            }
        });

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

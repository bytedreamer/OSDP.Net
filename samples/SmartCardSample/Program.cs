using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Configuration;
using OSDP.Net;
using OSDP.Net.Connections;
using OSDP.Net.Model.CommandData;

namespace SmartCardSample
{
    internal class Program
    {
        private static bool _readyForSmartCardRead = true;
        private static Guid _connectionId;

        private static async Task Main()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true);
            var config = builder.Build();
            var osdpSection = config.GetSection("OSDP");
            string portName = osdpSection["PortName"];
            int baudRate = int.Parse(osdpSection["BaudRate"]);
            byte deviceAddress = byte.Parse(osdpSection["DeviceAddress"]);
            byte readerNumber = byte.Parse(osdpSection["ReaderNumber"]);
            
            var panel = new ControlPanel();
            panel.ConnectionStatusChanged += (sender, eventArgs) =>
            {
                Console.WriteLine($"Device is {(eventArgs.IsConnected ? "Online" : "Offline")}");

            };
            panel.NakReplyReceived += (sender, args) =>
            {
                Console.WriteLine($"Received NAK {args.Nak}");
            };
            panel.ExtendedReadReplyReceived += (sender, eventArgs) =>
            {
                Task.Run(async () =>
                {
                    if (!_readyForSmartCardRead)
                    {
                        return;
                    }

                    try
                    {
                        if (eventArgs.ExtendedRead.Mode == 1 && eventArgs.ExtendedRead.PReply == 1)
                        {
                            _readyForSmartCardRead = false;

                            var response = await panel.ExtendedWriteData(_connectionId, deviceAddress,
                                ExtendedWrite.ModeOneSmartCardScan(readerNumber));
                            if (eventArgs.ExtendedRead.Mode == 1 && response.ReplyData?.PReply == 1)
                            {
                                Console.WriteLine("Card Present");
                                while (true)
                                {
                                    Console.WriteLine("Enter APDU data, leave blank to terminate:");
                                    var data = Console.ReadLine()?.Trim();
                                    if (string.IsNullOrWhiteSpace(data))
                                    {
                                        break;
                                    }

                                    response = await panel.ExtendedWriteData(_connectionId, deviceAddress,
                                        ExtendedWrite.ModeOnePassAPDUCommand(readerNumber, StringToByteArray(data)));
                                    if (response.ReplyData == null)
                                    {
                                        break;
                                    }

                                    Console.WriteLine(
                                        $"Received extended reply {response.ReplyData.Mode}:{response.ReplyData.PReply}:{BitConverter.ToString(response.ReplyData.PData).Replace("-", string.Empty)}");
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    if (eventArgs.ExtendedRead.Mode == 1)
                    {
                        try
                        {
                            Console.WriteLine("Disconnecting from SmartCard");
                            await panel.ExtendedWriteData(_connectionId, deviceAddress,
                                ExtendedWrite.ModeOneTerminateSmartCardConnection(readerNumber));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                        finally
                        {
                            _readyForSmartCardRead = true;
                        }
                    }
                });
            };
            panel.RawCardDataReplyReceived += (sender, eventArgs) => 
            {
                Console.WriteLine($"Raw card read {FormatData(eventArgs.RawCardData.Data)}");
            }; 
            
            _connectionId = panel.StartConnection(new SerialPortOsdpConnection(portName, baudRate));
            panel.AddDevice(_connectionId, 0, true, true);

            Timer timer = new Timer(5000);
            timer.Elapsed += (sender, eventArgs) =>
            {
                Task.Run(async () =>
                {
                    timer.Stop();
                    try
                    {
                        if (_readyForSmartCardRead && panel.IsOnline(_connectionId, deviceAddress))
                        {
                            Console.WriteLine("Checking SmartCard settings");
                            var response = await panel.ExtendedWriteData(_connectionId, deviceAddress, ExtendedWrite.ReadModeSetting());
                            if (response.ReplyData == null)
                            {
                                panel.ResetDevice(_connectionId, 0);
                            }
                            else if (response.ReplyData.Mode == 0 && response.ReplyData.PReply == 1 && response.ReplyData.PData[0] == 0)
                            {
                                await panel.ExtendedWriteData(_connectionId, 0, ExtendedWrite.ModeOneConfiguration());
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    finally
                    {
                        timer.Start();
                    }
                });
            };
            
            timer.Start();

            await Task.Delay(TimeSpan.FromDays(7));
        }

        private static string FormatData(BitArray bitArray)
        {
            var builder = new StringBuilder();
            foreach (bool bit in bitArray)
            {
                builder.Append(bit ? "1" : "0");
            }

            return builder.ToString();
            
        }

        public static byte[] StringToByteArray(string hex) {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }
    }
}

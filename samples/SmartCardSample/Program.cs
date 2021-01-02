using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using OSDP.Net;
using OSDP.Net.Connections;
using OSDP.Net.Model.CommandData;

namespace SmartCardSample
{
    class Program
    {
        private const string PortName = "/dev/tty.usbserial-AB0JI236";
        private const int BaudRate = 9600;
        private const byte DeviceAddress = 0;
        private const byte ReaderNumber = 0;

        private static bool _readyForSmartCardRead = true;
        private static Guid _connectionId;

        static async Task Main()
        {
            var panel = new ControlPanel();
            panel.ConnectionStatusChanged += (sender, eventArgs) =>
            {
                Console.WriteLine($"Device is {(eventArgs.IsConnected ? "Online" : "Offline")}");

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

                            var response = await panel.ExtendedWriteData(_connectionId, DeviceAddress,
                                ExtendedWrite.ModeOneSmartCardScan(ReaderNumber));
                            if (eventArgs.ExtendedRead.Mode == 1 && response.ReplyData?.PReply == 1)
                            {
                                Console.WriteLine("Card Present");
                                while (true)
                                {
                                    Console.WriteLine("Enter APDU data:");
                                    var data = Console.ReadLine()?.Trim();
                                    if (string.IsNullOrWhiteSpace(data))
                                    {
                                        break;
                                    }

                                    response = await panel.ExtendedWriteData(_connectionId, DeviceAddress,
                                        ExtendedWrite.ModeOnePassAPDUCommand(ReaderNumber, StringToByteArray(data)));
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
                            await panel.ExtendedWriteData(_connectionId, DeviceAddress,
                                ExtendedWrite.ModeOneTerminateSmartCardConnection(ReaderNumber));
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
            
            _connectionId = panel.StartConnection(new SerialPortOsdpConnection(PortName, BaudRate));
            panel.AddDevice(_connectionId, 0, true, true);

            Timer timer = new Timer(5000);
            timer.Elapsed += (sender, eventArgs) =>
            {
                Task.Run(async () =>
                {
                    timer.Stop();
                    try
                    {
                        if (_readyForSmartCardRead)
                        {
                            Console.WriteLine("Checking SmartCard settings");
                            var response = await panel.ExtendedWriteData(_connectionId, DeviceAddress, ExtendedWrite.ReadModeSetting());
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

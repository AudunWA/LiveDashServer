using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace LiveDashServer
{
    class ForwarderConnection
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private ConcurrentDictionary<string, int> _dataCount = new ConcurrentDictionary<string, int>();
        private Dictionary<string, DateTime> _dataTimes = new Dictionary<string, DateTime>();
        private ConcurrentDictionary<string, double> _frequencies = new ConcurrentDictionary<string, double>();

        private async Task UpdateFrequenciesAsync(CancellationToken token = default)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    const int DELAY = 1000;
                    foreach (var pair in _dataCount)
                    {
                        _frequencies[pair.Key] = Math.Max(1, pair.Value / (DELAY / 1000d));
                        _dataCount[pair.Key] = 0;
                    }

                    await Task.Delay(DELAY, token);
                }
            }
            catch (Exception e)
            {
                ;
            }
        }
        public async Task ListenAsync()
        {
            try
            {
                TcpListener listener = new TcpListener(IPAddress.Any, 1221);
                listener.Start();
                _ = Task.Run(() => UpdateFrequenciesAsync());

                while (true)
                {
                    Program.Server.StartSimulator();
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    _logger.Info("Got connection from pit!");
                    Program.Server.StopSimulator();
                    using (NetworkStream stream = client.GetStream())
                    {
                        while (client.Connected)
                        {
                            await ReadMessageAsync(stream, client);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        private async Task ReadMessageAsync(NetworkStream stream, TcpClient client)
        {
            try
            {
                byte[] timestampBytes = await ReadFixedAmountAsync(stream, sizeof(long));
                if (timestampBytes == null)
                {
                    client.Close();
                    return;
                }

                long timestamp = BitConverter.ToInt64(timestampBytes, 0);

                byte[] valuesCountBytes = await ReadFixedAmountAsync(stream, 1);
                if (valuesCountBytes == null)
                {
                    client.Close();
                    return;
                }

                for (int i = 0; i < valuesCountBytes[0]; i++)
                {
                    byte[] channelNameLengthBytes = await ReadFixedAmountAsync(stream, 1);
                    if (channelNameLengthBytes == null)
                    {
                        client.Close();
                        return;
                    }

                    byte[] channelNameBytes = await ReadFixedAmountAsync(stream, channelNameLengthBytes[0]);
                    if (channelNameBytes == null)
                    {
                        client.Close();
                        return;
                    }


                    byte[] dataBytes = await ReadFixedAmountAsync(stream, sizeof(double));
                    if (dataBytes == null)
                    {
                        client.Close();
                        return;
                    }

                    string channelName = Encoding.UTF8.GetString(channelNameBytes);
                    double data = BitConverter.ToDouble(dataBytes, 0);

                    if(!_dataTimes.TryGetValue(channelName, out DateTime lastMessageTime))
                        lastMessageTime = DateTime.Now;

                    double period = Math.Max(0.000001, (DateTime.Now - lastMessageTime).TotalSeconds);
                    double frequency = 1 / period;
                    _dataTimes[channelName] = DateTime.Now;
                    if (!_dataCount.TryAdd(channelName, 0))
                        _dataCount[channelName]++;

                    //if (!_frequencies.TryGetValue(channelName, out double frequency))
                    //    frequency = 1;
                    if (true || _dataCount[channelName] % Math.Max(1, (int) (frequency / 10)) == 0)
                    {
                        if (frequency > 10)
                        {
                            _logger.Trace("{0}: {1} - {2} Hz", channelName, data, frequency);
                        }

                        Program.Server.WriteToAllClients(
                            $"{{ \"channel\": \"{channelName}\", \"data\": {data.ToString().Replace(',', '.')} }}");
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        private async Task<byte[]> ReadFixedAmountAsync(NetworkStream stream, int count)
        {
            int totalBytes = 0;
            byte[] bytes = new byte[count];
            while (totalBytes < count)
            {
                int bytesReceived = await stream.ReadAsync(bytes, 0, count - totalBytes);
                if (bytesReceived == 0)
                {
                    // End of stream or something
                    _logger.Warn("No more bytes to read from forwarder.");
                    return null;
                }

                totalBytes += bytesReceived;
            }

            return bytes;
        }
    }
}

﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace LiveDashServer
{
    /// <summary>
    /// Used to receive data from Analyze, which forwards data directly from the car
    /// </summary>
    public class ForwarderConnection
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// A dictionary containing which is used to measure the time elapsed since we last got a message in a given data channel
        /// </summary>
        private readonly Dictionary<string, Stopwatch> _dataTimes = new Dictionary<string, Stopwatch>();

        /// <summary>
        /// A dictionary which maps real channels to a respective calculated channel
        /// </summary>
        private readonly Dictionary<string, List<ICalculatedChannel>> _calculatedChannels = new Dictionary<string, List<ICalculatedChannel>>();

        /// <summary>
        /// Indicates if there is an instance of Analyze connected to this forwarder
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Populates the calculated channel-dictioanry
        /// </summary>
        private void InitCalculatedChannels()
        {
            // TODO: Load from some config file
            CalculatedChannelEuclidian channel = new CalculatedChannelEuclidian("LiveDash_velocity", "ADC_FL_GearTempFL", "ADC_FR_GearTempFR", 3.6);
            _calculatedChannels.Add(channel.ChannelName1, new List<ICalculatedChannel>{channel});
            _calculatedChannels.Add(channel.ChannelName2, new List<ICalculatedChannel>{channel});
        }


        /// <summary>
        /// The main loop of the forwarder connection. Listens and processes connections, and runs forever
        /// </summary>
        /// <returns></returns>
        public async Task ListenAsync()
        {
            try
            {
                InitCalculatedChannels();
                TcpListener listener = new TcpListener(IPAddress.Any, 1221); // TODO: Make configurable
                listener.Start();

                while (true)
                {
                    IsConnected = false;
                    Program.Server.UpdateConsoleTitle();
                    Program.Server.StartSimulator();
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    IsConnected = true;
                    Program.Server.UpdateConsoleTitle();
                    _logger.Info("Got connection from pit!");
                    Program.Server.StopSimulator();
                    using (NetworkStream stream = client.GetStream())
                    {
                        while (client.Connected)
                        {
                            ForwarderMessage message = await ReadMessageAsync(stream);
                            if (message == null)
                            {
                                client.Close();
                                break;
                            }

                            await HandleMessageAsync(message);
                        }
                    }
                    _logger.Info("Lost connection from pit");
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        /// <summary>
        /// Processes a forwarder message after it has been parsed
        /// </summary>
        /// <param name="message">The forwarder message</param>
        private async Task HandleMessageAsync(ForwarderMessage message)
        {
            HashSet<ICalculatedChannel> calculatedChannelsToUpdate = new HashSet<ICalculatedChannel>();
            foreach (var dataPair in message.DataValues)
            {
                long period;
                if (!_dataTimes.TryGetValue(dataPair.Key, out Stopwatch lastMessageStopwatch))
                {
                    lastMessageStopwatch = new Stopwatch();
                    period = int.MaxValue;
                }
                else
                {
                    period = Math.Max(1, lastMessageStopwatch.ElapsedMilliseconds);
                }

                double frequency = 1000d / period;
                //if (!_dataCount.TryAdd(channelName, 0))
                //    _dataCount[channelName]++;

                if (frequency > 500)
                {
                    //_logger.Trace("{0}: {1} - {2} Hz", dataPair.Key, dataPair.Value, frequency);
                }
                else if(frequency <= 10)
                {
                    if (_calculatedChannels.TryGetValue(dataPair.Key, out var calculatedChannels))
                    {
                        foreach (ICalculatedChannel calculatedChannel in calculatedChannels)
                        {
                            calculatedChannel.UpdateValue(dataPair.Key, dataPair.Value);
                            calculatedChannelsToUpdate.Add(calculatedChannel);
                        }
                    }

                    _dataTimes[dataPair.Key] = lastMessageStopwatch;
                    lastMessageStopwatch.Restart();
                    await Program.Server.WriteToAllClients(
                        $"{{ \"channel\": \"{dataPair.Key}\", \"data\": {dataPair.Value.ToString().Replace(',', '.')} }}", dataPair.Key);
                }
            }

            foreach (ICalculatedChannel calculatedChannel in calculatedChannelsToUpdate)
            {
                await Program.Server.WriteToAllClients(
                    $"{{ \"channel\": \"{calculatedChannel.Name}\", \"data\": {calculatedChannel.CalculatedValue.ToString().Replace(',', '.')} }}", calculatedChannel.Name);
            }
        }

        /// <summary>
        /// Reads and parses a forwarder message from a network stream
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        /// <returns>A new ForwarderMessage containing the read data, or null if an error occurred</returns>
        private async Task<ForwarderMessage> ReadMessageAsync(NetworkStream stream)
        {
            // TODO: Could possibly be moved to StreamExtensions, but it's only used in this class
            ForwarderMessage message;
            try
            {
                byte[] timestampBytes = await stream.ReadFixedAmountAsync(sizeof(long));
                if (timestampBytes == null)
                {
                    return null;
                }

                long timestamp = BitConverter.ToInt64(timestampBytes, 0);
                message = new ForwarderMessage(timestamp);

                byte[] valuesCountBytes = await stream.ReadFixedAmountAsync(1);
                if (valuesCountBytes == null)
                {
                    return null;
                }

                for (int i = 0; i < valuesCountBytes[0]; i++)
                {
                    byte[] channelNameLengthBytes = await stream.ReadFixedAmountAsync(1);
                    if (channelNameLengthBytes == null)
                    {
                        return null;
                    }

                    byte[] channelNameBytes = await stream.ReadFixedAmountAsync(channelNameLengthBytes[0]);
                    if (channelNameBytes == null)
                    {
                        return null;
                    }


                    byte[] dataBytes = await stream.ReadFixedAmountAsync(sizeof(double));
                    if (dataBytes == null)
                    {
                        return null;
                    }

                    string channelName = Encoding.UTF8.GetString(channelNameBytes);
                    double data = BitConverter.ToDouble(dataBytes, 0);

                    if (!message.DataValues.TryAdd(channelName, data))
                    {
                        _logger.Warn("Received a message with 2 data points from the same channel");
                    }
                }
            }
            catch (IOException e) when (e.InnerException is SocketException socketException)
            {
                // These exceptions usually gets called when the client disconnects
                _logger.Trace("Lost connection: SocketError.{0}", socketException.SocketErrorCode);
                return null;
            }
            catch (Exception e)
            {
                _logger.Error(e);
                return null;
            }

            return message;
        }
    }
}

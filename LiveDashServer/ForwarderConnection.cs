using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LiveDashServer
{
    class ForwarderConnection
    {
        public async Task ListenAsync()
        {
            try
            {
                TcpListener listener = new TcpListener(IPAddress.Any, 1221);
                listener.Start();

                while (true)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    Console.WriteLine("Got connection from pit!");
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
                    if(channelNameLengthBytes == null)
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

                    Program.Server.WriteToAllClients($"{{ \"channel\": \"{channelName}\", \"data\": {data.ToString().Replace(',', '.')} }}");
                }
            }
            catch (Exception e)
            {

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
                    return null;
                }

                totalBytes += bytesReceived;
            }

            return bytes;
        }
    }
}

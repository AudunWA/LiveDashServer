using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace LiveDashServer
{
    public static class StreamExtensions
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static async Task<byte[]> ReadFixedAmountAsync(this Stream stream, int count)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

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

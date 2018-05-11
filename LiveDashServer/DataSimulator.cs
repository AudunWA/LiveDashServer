using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace LiveDashServer
{
    /// <summary>
    /// Generates fake data for sending to client when there is no Analyze instance connected
    /// </summary>
    public class DataSimulator
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The main loop of the data simulator. Both generates and sends the generated data to all clients
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task GenerateAndSendData(CancellationToken token = default)
        {
            Random random = new Random();
            int counter = 60;
            int counter2 = 60;
            string messageFormat = "{{ \"canId\": \"{0}\", \"data\": {1} }}";
            byte[] messageBytes = new byte[4];
            try
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    counter = GenerateNewNumber(counter, random);
                    counter2 = GenerateNewNumber(counter2, random);

                    foreach (Client client in Program.Server.Clients.Values)
                    {
                        foreach (string channel in client.SubscribedChannels.ToList())
                        {
                            client.SendMessage(string.Format(messageFormat, channel, counter), channel).Forget();
                        }
                    }
                    counter = counter % 120;
                    if (counter < 0) counter = 120;

                    await Task.Delay(100, token);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Trace("DataSimulator task canceled");
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        /// <summary>
        /// Generates a random number which gets added to the input counter
        /// </summary>
        /// <param name="counter">The input counter</param>
        /// <param name="random">The Random instance that provides random numbers </param>
        /// <returns></returns>
        private static int GenerateNewNumber(int counter, Random random)
        {
            const double delta = 120 * 0.1;
            return counter + (int)(random.NextDouble() * delta * 2 - delta);
        }
    }
}

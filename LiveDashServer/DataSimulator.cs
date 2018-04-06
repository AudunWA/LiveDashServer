using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace LiveDashServer
{
    public class DataSimulator
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private const int TIMESTAMP_ID = 32;
        private const int VIDEO_DELAY_ID = 31;
        public async Task GenerateAndSendData(CancellationToken token = default)
        {
            Random random = new Random();
            int counter = 60;
            int counter2 = 60;
            string messageFormat = "{{ \"canId\": {0}, \"data\": {1} }}";
            byte[] messageBytes = new byte[4];
            try
            {
                while (!token.IsCancellationRequested)
                {
                    counter = GenerateNewNumber(counter, random);
                    counter2 = GenerateNewNumber(counter2, random);
                    //BitConverter.GetBytes((short)1).CopyTo(messageBytes, 0);
                    //BitConverter.GetBytes((short)counter).CopyTo(messageBytes, 2);
                    //await Program.Server.WriteToAllClients(messageBytes);
                    //BitConverter.GetBytes((short)50).CopyTo(messageBytes, 0);
                    //BitConverter.GetBytes((short)(120-counter)).CopyTo(messageBytes, 2);
                    //await Program.Server.WriteToAllClients(messageBytes);
                    string message = string.Format(messageFormat, 1, counter);
                    string message2 = string.Format(messageFormat, 50, 120 - counter);
                    string message3 = string.Format(messageFormat, 2, counter2);
                    Program.Server.WriteToAllClients(message);
                    Program.Server.WriteToAllClients(message2);
                    Program.Server.WriteToAllClients(message3);
                    Program.Server.WriteToAllClients(string.Format(messageFormat, TIMESTAMP_ID,
                        DateTimeOffset.Now.ToUnixTimeSeconds()));
                    Program.Server.WriteToAllClients(string.Format(messageFormat, VIDEO_DELAY_ID, 3000));
                    //counter++;
                    counter = counter % 120;
                    counter2 = counter2 % 120;
                    if (counter < 0) counter = 120;
                    if (counter2 < 0) counter2 = 120;
                    //if (counter == 0) counter = 1;
                    //if (counter2 == 0) counter2 = 1;
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

        private static int GenerateNewNumber(int counter, Random random)
        {
            double delta = 120 * 0.1;
            counter += (int) (random.NextDouble() * delta * 2 - delta);
            return counter;
        }
    }
}

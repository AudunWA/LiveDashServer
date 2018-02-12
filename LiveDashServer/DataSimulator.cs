using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LiveDashServer
{
    class DataSimulator
    {
        public async Task GenerateAndSendData()
        {
            int counter = 0;
            string messageFormat = "{{ \"canId\": {0}, \"data\": {1} }}";
            byte[] messageBytes = new byte[4];
            try
            {
                while (true)
                {
                    //BitConverter.GetBytes((short)1).CopyTo(messageBytes, 0);
                    //BitConverter.GetBytes((short)counter).CopyTo(messageBytes, 2);
                    //await Program.Server.WriteToAllClients(messageBytes);
                    //BitConverter.GetBytes((short)50).CopyTo(messageBytes, 0);
                    //BitConverter.GetBytes((short)(120-counter)).CopyTo(messageBytes, 2);
                    //await Program.Server.WriteToAllClients(messageBytes);
                    string message = string.Format(messageFormat, 1, counter);
                    string message2 = string.Format(messageFormat, 50, 120 - counter);
                    await Program.Server.WriteToAllClients(message);
                    await Program.Server.WriteToAllClients(message2);
                    counter++;
                    counter = counter % 120;
                    await Task.Delay(10);
                }
            }
            catch (Exception e)
            {

            }
        }
    }
}

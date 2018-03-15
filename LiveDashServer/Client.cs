using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using vtortola.WebSockets;

namespace LiveDashServer
{
    public class Client
    {
        public Task MainTask { get; }
        public WebSocket Socket { get; }

        public Client(WebSocket socket)
        {
            Socket = socket;
            MainTask = ProcessConnection();
            // Task.Run(ProcessConnection);
        }

        private async Task ProcessConnection()
        {
            try
            {
                while (Socket.IsConnected)
                {
                    WebSocketMessageReadStream messageReadStream =
                        await Socket.ReadMessageAsync(CancellationToken.None);
                    if (messageReadStream.MessageType == WebSocketMessageType.Text)
                    {
                        string msgContent;
                        using (var sr = new StreamReader(messageReadStream, Encoding.UTF8))
                            msgContent = await sr.ReadToEndAsync();

                        Program.Server.WriteToAllClients(msgContent);
                    }
                }
            }
            finally
            {
                Socket.Dispose();
            }
        }
    }
}
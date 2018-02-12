using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using vtortola.WebSockets;

namespace LiveDashServer
{
    class Server
    {
        public bool IsRunning { get; private set; } = true;
        private readonly ConcurrentBag<Client> _clients = new ConcurrentBag<Client>();

        public async Task Run()
        {
            var listener = new WebSocketListener(new IPEndPoint(IPAddress.Any, 8080));
            listener.Standards.RegisterStandard(new WebSocketFactoryRfc6455());
            listener.StartAsync();

            DataSimulator simulator = new DataSimulator();
            Task.Factory.StartNew(() => simulator.GenerateAndSendData().ConfigureAwait(false));
            while (IsRunning)
            {
                var client = await listener.AcceptWebSocketAsync(CancellationToken.None);
                _clients.Add(new Client(client));

            }
        }

        public async Task WriteToAllClients(string message)
        {
            foreach (var client in _clients)
            {
                if (!client.Socket.IsConnected)
                    continue;
                using (var writer = client.Socket.CreateMessageWriter(WebSocketMessageType.Text))
                {
                    using (var sw = new StreamWriter(writer, Encoding.UTF8))
                    {
                        await sw.WriteAsync(message);
                    }
                }
            }
        }
        public async Task WriteToAllClients(byte[] message)
        {
            foreach (var client in _clients)
            {
                if (!client.Socket.IsConnected)
                    continue;

                using (var messageWriter = client.Socket.CreateMessageWriter(WebSocketMessageType.Binary))
                    await messageWriter.WriteAsync(message, 0, message.Length);
            }
        }
    }
}
using System;
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
        private const int WS_PORT = 8080;

        public bool IsRunning { get; private set; } = true;
        private readonly ConcurrentBag<Client> _clients = new ConcurrentBag<Client>();

        public async Task Run()
        {
            var listener = new WebSocketListener(new IPEndPoint(IPAddress.Any, WS_PORT));
            listener.Standards.RegisterStandard(new WebSocketFactoryRfc6455());
            _ = listener.StartAsync().ConfigureAwait(false);
            Console.WriteLine($"Listening on port {WS_PORT}");

            DataSimulator simulator = new DataSimulator();
            _ = simulator.GenerateAndSendData().ConfigureAwait(false);

            while (IsRunning)
            {
                var clientSocket = await listener.AcceptWebSocketAsync(CancellationToken.None);
                Console.WriteLine("Accepted a new client!");
                Client client = new Client(clientSocket);
                _clients.Add(client);

            }
        }

        public void WriteToAllClients(string message)
        {
            foreach (var client in _clients)
            {
                _ = client.SendMessage(message);
            }
        }
        public void WriteToAllClients(byte[] message)
        {
            foreach (var client in _clients)
            {
                _ = client.SendMessage(message);
            }
        }
    }
}
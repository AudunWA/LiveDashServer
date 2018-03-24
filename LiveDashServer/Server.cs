﻿using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using vtortola.WebSockets;

namespace LiveDashServer
{
    class Server
    {
        private const int WS_PORT = 8080;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();


        private volatile int _nextClientID = 1;

        public bool IsRunning { get; private set; } = true;
        private readonly ConcurrentDictionary<int, Client> _clients = new ConcurrentDictionary<int, Client>();

        public async Task Run()
        {
            var listener = new WebSocketListener(new IPEndPoint(IPAddress.Any, WS_PORT));
            listener.Standards.RegisterStandard(new WebSocketFactoryRfc6455());
            _ = listener.StartAsync().ConfigureAwait(false);
            _logger.Info("Listening on port {0}", WS_PORT);

         /*   ForwarderConnection connection = new ForwarderConnection();
            _ = connection.ListenAsync().ConfigureAwait(false);*/

            DataSimulator simulator = new DataSimulator();
            _ = simulator.GenerateAndSendData().ConfigureAwait(false);

            while (IsRunning)
            {
                try
                {
                    var clientSocket = await listener.AcceptWebSocketAsync(CancellationToken.None);
                    Client client = new Client(_nextClientID++, clientSocket);
                    _ = HandleClient(client);
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                }

            }
        }

        private async Task HandleClient(Client client)
        {
            _logger.Info("Accepted a new client with ID {0}!", client.ID);
            _clients.TryAdd(client.ID, client);
            UpdateConsoleTitle();

            await client.ProcessConnection().ConfigureAwait(false);
            _clients.TryRemove(client.ID, out _);
            _logger.Info("Client {0} disconnected", client.ID);
            UpdateConsoleTitle();
        }

        private void UpdateConsoleTitle()
        {
            Console.Title = $"LiveDashServer - {_clients.Count} client(s)";
        }

        public void WriteToAllClients(string message)
        {
            foreach (var client in _clients.Values)
            {
                _ = client.SendMessage(message);
            }
        }
        public void WriteToAllClients(byte[] message)
        {
            foreach (var client in _clients.Values)
            {
                _ = client.SendMessage(message);
            }
        }
    }
}
using System;
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

        private readonly ForwarderConnection _forwarderConnection = new ForwarderConnection();
        private readonly DataSimulator _simulator = new DataSimulator();
        private CancellationTokenSource _simulatorTokenSource;

        public void StartSimulator()
        {
            if (_simulatorTokenSource != null && !_simulatorTokenSource.IsCancellationRequested)
                return;

            _simulatorTokenSource = new CancellationTokenSource();
            _simulator.GenerateAndSendData(_simulatorTokenSource.Token).Forget();

        }
        public void StopSimulator()
        {
            _simulatorTokenSource?.Cancel();
        }

        public async Task Run()
        {
            UpdateConsoleTitle();
            WebSocketListenerOptions options = new WebSocketListenerOptions()
            {
                //PingMode = PingModes.LatencyControl,
                //PingTimeout = TimeSpan.FromSeconds(1)
            };
            var listener = new WebSocketListener(new IPEndPoint(IPAddress.Any, WS_PORT), options);
            listener.Standards.RegisterStandard(new WebSocketFactoryRfc6455());
            listener.StartAsync().Forget();
            _logger.Info("Listening on port {0}", WS_PORT);

           _forwarderConnection.ListenAsync().Forget();

            while (IsRunning)
            {
                try
                {
                    var clientSocket = await listener.AcceptWebSocketAsync(CancellationToken.None);

                    if (clientSocket != null)
                    {
                        // TODO: Log the exception which caused the socket to become null
                        Client client = new Client(_nextClientID++, clientSocket);
                        HandleClient(client).Forget();
                    }
                    else
                    {
                        _logger.Warn("A client socket was null");
                    }
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

            await client.ProcessConnection();
            _clients.TryRemove(client.ID, out _);
            _logger.Info("Client {0} disconnected", client.ID);
            UpdateConsoleTitle();
        }

        internal void UpdateConsoleTitle()
        {
            Console.Title = $"LiveDashServer - {_clients.Count} client(s), forwarder {(_forwarderConnection.IsConnected ? "" : "NOT")} connected";
        }

        public async Task WriteToAllClients(string message)
        {
            foreach (var client in _clients.Values)
            {
                await client.SendMessage(message);
            }
        }
        public async Task WriteToAllClients(byte[] message)
        {
            foreach (var client in _clients.Values)
            {
                await client.SendMessage(message);
            }
        }
    }
}
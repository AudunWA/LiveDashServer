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
    /// <summary>
    /// A wrapper around the WebSocket server which keeps track of all clients
    /// </summary>
    public class Server
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The port that the server should listen on
        /// </summary>
        private const int WS_PORT = 8080;

        /// <summary>
        /// An integer used to generate client IDs
        /// </summary>
        private volatile int _nextClientID = 1;

        /// <summary>
        /// Indicates if the server is running or not
        /// </summary>
        public bool IsRunning { get; private set; } = true;

        /// <summary>
        /// A dictionary containing all the connected web application users
        /// </summary>
        public ConcurrentDictionary<int, Client> Clients { get; } = new ConcurrentDictionary<int, Client>();

        /// <summary>
        /// A reference to the forwarder connection
        /// </summary>
        private readonly ForwarderConnection _forwarderConnection = new ForwarderConnection();

        /// <summary>
        /// A reference to the data simulator
        /// </summary>
        private readonly DataSimulator _simulator = new DataSimulator();

        /// <summary>
        /// A cancellation token source used to cancel the data simulator
        /// </summary>
        private CancellationTokenSource _simulatorTokenSource;


        /// <summary>
        /// Starts the data simulator if it isn't started already
        /// </summary>
        public void StartSimulator()
        {
            if (_simulatorTokenSource != null && !_simulatorTokenSource.IsCancellationRequested)
                return;

            _simulatorTokenSource = new CancellationTokenSource();
            _simulator.GenerateAndSendData(_simulatorTokenSource.Token).Forget();

        }

        /// <summary>
        /// Requests to stop the data simulator
        /// </summary>
        public void StopSimulator()
        {
            _simulatorTokenSource?.Cancel();
        }

        /// <summary>
        /// Starts listening and processing WebSocket clients. This method also starts the forwarder connection
        /// </summary>
        public async Task RunAsync()
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

        /// <summary>
        /// Manages the life cycle of a client, and ensures that it gets added and removed from the client dictionary
        /// </summary>
        /// <param name="client">The client to manage</param>
        private async Task HandleClient(Client client)
        {
            _logger.Info("Accepted a new client with ID {0}!", client.ID);
            Clients.TryAdd(client.ID, client);
            UpdateConsoleTitle();

            try
            {
                await client.ProcessConnection();
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
            finally
            {
                Clients.TryRemove(client.ID, out _);
                _logger.Info("Client {0} disconnected", client.ID);
                UpdateConsoleTitle();
            }
        }

        /// <summary>
        /// Updates the console title with the latest stats. Windows only
        /// </summary>
        internal void UpdateConsoleTitle()
        {
            Console.Title = $"LiveDashServer - {Clients.Count} client(s), forwarder {(_forwarderConnection.IsConnected ? "" : "NOT")} connected";
        }

        /// <summary>
        /// Writes a string to all connected clients
        /// </summary>
        /// <param name="message">The string to write</param>
        /// <param name="dataChannel">The name of the data channel the message belongs to</param>
        /// <returns></returns>
        public async Task WriteToAllClients(string message, string dataChannel)
        {
            foreach (var client in Clients.Values)
            {
                await client.SendMessage(message, dataChannel);
            }
        }


        /// <summary>
        /// Writes a byte array to all connected clients
        /// </summary>
        /// <param name="message">The byte array to write</param>
        /// <returns></returns>
        public async Task WriteToAllClients(byte[] message)
        {
            foreach (var client in Clients.Values)
            {
                await client.SendMessage(message);
            }
        }
    }
}
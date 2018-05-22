using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using vtortola.WebSockets;

namespace LiveDashServer
{
    /// <summary>
    /// Represents a web application user, and wraps around a WebSocket client
    /// </summary>
    public class Client
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// A cancellation token source used to stop the client
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// A task completion source used to signal that the connection has been closed
        /// </summary>
        private readonly TaskCompletionSource<bool> _isClosed = new TaskCompletionSource<bool>();

        /// <summary>
        /// The WebSocket connection to the web application
        /// </summary>
        private WebSocket Socket { get; }

        /// <summary>
        /// THe ID of this client
        /// </summary>
        public int ID { get; }

        /// <summary>
        /// Indicates if the client is connected or not
        /// </summary>
        public bool IsConnected => Socket.IsConnected;

        /// <summary>
        /// An event which gets called every time an message gets received
        /// </summary>
        public EventHandler<string> MessageReceived { get; set; }

        /// <summary>
        /// A list of all the channel that the client has subscribed to
        /// </summary>
        public HashSet<string> SubscribedChannels { get; } = new HashSet<string>();


        /// <summary>
        /// Initializes a new client
        /// </summary>
        /// <param name="id">The ID of the client</param>
        /// <param name="socket">The WebSocket connection which the client should use</param>
        public Client(int id, WebSocket socket)
        {
            ID = id;
            Socket = socket;

            MessageReceived += ProcessMessage;
        }

        /// <summary>
        /// Processes messages from the web application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="dataChannel"></param>
        private void ProcessMessage(object sender, string dataChannel)
        {
            if (SubscribedChannels.Add(dataChannel))
                _logger.Trace($"Client {this.ID} subscribed to channel \"{dataChannel}\"");
        }

        /// <summary>
        /// The main loop of the client. Listens for messages, and detects client disconnection
        /// </summary>
        /// <returns></returns>
        public async Task ProcessConnection()
        {
            CancellationToken token = _cancellationTokenSource.Token;
            // This fixes "deadlock"-ish issue with unit tests, I don't understand why yet
            // It probably has to do with the synchronization context that the unit tests run in
            await Task.Yield();
            try
            {
                do
                {
                    WebSocketMessageReadStream messageReadStream =
                        await Socket.ReadMessageAsync(token);

                    // Client disconnected
                    if (messageReadStream == null)
                    {
                        _logger.Trace("Client {0}'s stream closed", ID);
                        return;
                    }

                    if (messageReadStream.MessageType == WebSocketMessageType.Text)
                    {
                        string msgContent;
                        using (StreamReader sr = new StreamReader(messageReadStream, Encoding.UTF8))
                            msgContent = await sr.ReadToEndAsync();
                    
                        MessageReceived?.Invoke(this, msgContent);
                    }
                } while (Socket.IsConnected && !token.IsCancellationRequested);
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
            finally
            {
                Socket.Dispose();
                _isClosed.SetResult(true);
            }
        }

        /// <summary>
        /// Closes the coonection to the client
        /// </summary>
        /// <returns></returns>
        public async Task CloseAsync()
        {
            _cancellationTokenSource.Cancel();
            await _isClosed.Task;
        }

        /// <summary>
        /// Sends a message to the client, but only if the client has subscribed to the given channel
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="dataChannel">The data channel that the message belongs to</param>
        /// <returns></returns>
        public async Task SendMessage(string message, string dataChannel)
        {
            if (!Socket.IsConnected)
                return;

            if (!SubscribedChannels.Contains(dataChannel))
                return;

            using (WebSocketMessageWriteStream writer = Socket.CreateMessageWriter(WebSocketMessageType.Text))
            {
                using (StreamWriter sw = new StreamWriter(writer, Encoding.UTF8))
                {
                    await sw.WriteAsync(message);
                }
            }
        }

        /// <summary>
        /// Sends a byte array to the client
        /// </summary>
        /// <param name="message">The byte array to send</param>
        /// <returns></returns>
        public async Task SendMessage(byte[] message)
        {
            if (!Socket.IsConnected)
                return;

            using (WebSocketMessageWriteStream messageWriter = Socket.CreateMessageWriter(WebSocketMessageType.Binary))
                await messageWriter.WriteAsync(message, 0, message.Length);
        }
    }
}
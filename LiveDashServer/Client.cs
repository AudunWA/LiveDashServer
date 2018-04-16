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
    public class Client
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly TaskCompletionSource<bool> _isClosed = new TaskCompletionSource<bool>();
        private WebSocket Socket { get; }

        public int ID { get; }

        public bool IsConnected => Socket.IsConnected;

        public EventHandler<string> MessageReceived { get; set; }

        private readonly HashSet<string> subscribedChannels = new HashSet<string>();


        public Client(int id, WebSocket socket)
        {
            ID = id;
            Socket = socket;

            MessageReceived += ProcessMessage;
        }

        private void ProcessMessage(object sender, string dataChannel)
        {
            if (subscribedChannels.Add(dataChannel))
                _logger.Trace($"Client {this.ID} subscribed to channel \"{dataChannel}\"");
        }

        public async Task ProcessConnection()
        {
            CancellationToken token = _cancellationTokenSource.Token;
            // This fixes "deadlock"-ish issue with unit tests, I don't understand why yet
            // It might be because the loop always captures the context, thus not allowing the outer code to run
            // This is probably not a good solution, and should be replaced when I undertstand async better
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

        public async Task Close()
        {
            _cancellationTokenSource.Cancel();
            await _isClosed.Task;
        }

        public async Task SendMessage(string message, string dataChannel)
        {
            if (!Socket.IsConnected)
                return;

            if (!subscribedChannels.Contains(dataChannel))
                return;

            using (WebSocketMessageWriteStream writer = Socket.CreateMessageWriter(WebSocketMessageType.Text))
            {
                using (StreamWriter sw = new StreamWriter(writer, Encoding.UTF8))
                {
                    await sw.WriteAsync(message);
                }
            }
        }
        public async Task SendMessage(byte[] message)
        {
            if (!Socket.IsConnected)
                return;

            using (WebSocketMessageWriteStream messageWriter = Socket.CreateMessageWriter(WebSocketMessageType.Binary))
                await messageWriter.WriteAsync(message, 0, message.Length);
        }
    }
}
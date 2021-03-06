using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using NUnit.Framework;
using vtortola.WebSockets;

namespace LiveDashServer.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task CloseClientConnection()
        {
            WebSocket socket = A.Fake<WebSocket>();
            A.CallTo(() => socket.IsConnected).Returns(true);
            Assert.IsTrue(socket.IsConnected);
            Client client = new Client(1, socket);
            client.ProcessConnection().Forget();

            await client.CloseAsync();
            // Try to close after it has been closed
            await client.CloseAsync();
            A.CallTo(() => socket.Dispose()).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase("someData")]
        public async Task ReceiveData(string testData)
        {
            WebSocket socket = A.Fake<WebSocket>();
            var readStream = new WebSocketMessageReadStreamStub(new MemoryStream(Encoding.UTF8.GetBytes(testData)), WebSocketMessageType.Text, WebSocketExtensionFlags.None);
            A.CallTo(() => socket.IsConnected).Returns(true);
            A.CallTo(socket).WithReturnType<Task<WebSocketMessageReadStream>>().Returns(SimulateRead(readStream));
            Client client = new Client(1, socket);
            client.ProcessConnection().Forget();

            object delegateSender = null;
            string delegateData = null;
            client.MessageReceived += (sender, data) =>
            {
                delegateSender = sender;
                delegateData = data;
            };
            await client.CloseAsync();
            Assert.AreSame(client, delegateSender);
            Assert.AreEqual(testData, delegateData);
        }

        private async Task<WebSocketMessageReadStream> SimulateRead(WebSocketMessageReadStreamStub stub)
        {
            await Task.Yield();
            await Task.Delay(500).ConfigureAwait(false);
            return stub;
        }

        public class WebSocketMessageReadStreamStub : WebSocketMessageReadStream
        {
            private readonly MemoryStream _stream;

            public WebSocketMessageReadStreamStub(MemoryStream stream, WebSocketMessageType messageType, WebSocketExtensionFlags flags)
            {
                _stream = stream;
                MessageType = messageType;
                Flags = flags;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _stream.Read(buffer, offset, count);
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancel)
            {
                return _stream.ReadAsync(buffer, offset, count, cancel);
            }

            public override WebSocketMessageType MessageType { get; }

            public override WebSocketExtensionFlags Flags { get; }
        }
    }
}
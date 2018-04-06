using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace LiveDashServer.Tests
{
    class StreamExtensionsTests
    {
        [Test]
        public async Task ReadFixedAmountAsyncShouldReturnNullIfEndOfStreamReached()
        {
            MemoryStream stream = new MemoryStream(new byte[] { 0 });
            byte[] result = await stream.ReadFixedAmountAsync(2);
            Assert.Null(result);
        }

        [Test]
        public void ReadFixedAmountAsyncShouldNotAcceptNullStream()
        {
            Stream stream = null;
            Assert.That(async () => await stream.ReadFixedAmountAsync(0), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void ReadFixedAmountAsyncShouldNotAcceptNegativeCount()
        {
            MemoryStream stream = new MemoryStream(new byte[0]);
            Assert.That(async () => await stream.ReadFixedAmountAsync(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        [TestCase(new byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9})]
        [TestCase(new byte[] { 195, 134, 195, 152, 195, 133, 195, 166, 195, 184, 195, 165 })]
        [TestCase(new byte[0])]
        public async Task ReadFixedAmountAsyncShouldReturnTheSameAsTheInput(byte[] input)
        {
            MemoryStream stream = new MemoryStream(input);
            byte[] result = await stream.ReadFixedAmountAsync(input.Length);
            Assert.AreEqual(input, result);
        }
    }
}

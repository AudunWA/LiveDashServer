using System;
using System.Reflection;
using System.Threading.Tasks;

namespace LiveDashServer
{
    class Program
    {
        public static Server Server { get; private set; }
        static async Task Main(string[] args)
        {
            Console.WriteLine($"Starting LiveDashServer {Assembly.GetExecutingAssembly().GetName().Version}");
            Server = new Server();
            await Server.Run().ConfigureAwait(false);
        }
    }
}

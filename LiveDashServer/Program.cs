using System;
using System.Reflection;
using System.Threading.Tasks;
using NLog;

namespace LiveDashServer
{
    class Program
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static Server Server { get; private set; }

        static async Task Main(string[] args)
        {
            _logger.Info("Starting LiveDashServer {0}", Assembly.GetExecutingAssembly().GetName().Version);
            Server = new Server();
            await Server.Run().ConfigureAwait(false);

            // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
            LogManager.Shutdown();
        }
    }
}

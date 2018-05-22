using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace LiveDashServer
{
    /// <summary>
    /// The main class of the application
    /// </summary>
    static class Program
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The application's instance of the Server class
        /// </summary>
        public static Server Server { get; private set; }

        /// <summary>
        /// The entry point of the application
        /// </summary>
        /// <param name="args">Arguments sent in when starting the application. Currently not used</param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
            //DelayTest().Forget();
            _logger.Info("Starting LiveDashServer {0}", Assembly.GetExecutingAssembly().GetName().Version);
            try
            {
                Server = new Server();
                await Server.RunAsync();
            }
            catch (Exception e)
            {
                _logger.Error(e, "Main task error");
            }
            finally
            {
                _logger.Info("Server task awaited, shutting down LiveDashServer");
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                LogManager.Shutdown();
            }
        }

        /// <summary>
        /// A test task to measure the delay of Task.Delay and its connection to the utilization of the thread pool
        /// </summary>
        static async Task DelayTest()
        {
            ThreadPool.SetMinThreads(16, 16);
            while (true)
            {
                ThreadPool.GetAvailableThreads(out int workerThreads, out int completionPortThreads);
                _logger.Info($"Thread pool: workerThreads is {workerThreads}, completionPortThreads is {completionPortThreads}");
                ThreadPool.GetMinThreads(out int minWorkerThreads, out int minCompletionPortThreads);
                _logger.Info($"Thread pool: minWorkerThreads is {minWorkerThreads}, minCompletionPortThreads is {minCompletionPortThreads}");
                ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);
                _logger.Info($"Thread pool: maxWorkerThreads is {maxWorkerThreads}, maxCompletionPortThreads is {maxCompletionPortThreads}");
                _logger.Info($"Thread pool: worker is {maxWorkerThreads - workerThreads}, port is {maxCompletionPortThreads - completionPortThreads}");
                Stopwatch sw = new Stopwatch();
                sw.Start();
                await Task.Delay(1000);
                sw.Stop();
                _logger.Trace("Delay: " + sw.Elapsed.TotalMilliseconds);
            }
        }
    }
}

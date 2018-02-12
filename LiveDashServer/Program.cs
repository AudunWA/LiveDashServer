using System;
using System.Threading.Tasks;

namespace LiveDashServer
{
    class Program
    {
        public static Server Server { get; private set; }
        static async Task Main(string[] args)
        {
            Server = new Server();
            await Server.Run();
        }
    }
}

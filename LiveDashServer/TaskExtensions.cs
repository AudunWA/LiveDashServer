using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace LiveDashServer
{
    public static class TaskExtensions
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static async void Forget(this Task task)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }
    }
}

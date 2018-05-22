using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace LiveDashServer
{
    /// <summary>
    /// Contains extensions methods for the Task class
    /// </summary>
    public static class TaskExtensions
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Allows a caller to run an async task without awaiting it (fire-and-forget). This method awaits the async task, and logs any exceptions
        /// </summary>
        /// <param name="task">The task to fire-and-forget</param>
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

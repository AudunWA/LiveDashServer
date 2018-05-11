using System.Collections.Generic;

namespace LiveDashServer
{
    /// <summary>
    /// Represents a message sent from Analyze, which contains a set of data values
    /// </summary>
    class ForwarderMessage
    {
        /// <summary>
        /// The timetsamp of these values (relative time from car start)
        /// </summary>
        public long Timestamp { get; }

        /// <summary>
        /// The dictionary which contains the channel name and data value pairs
        /// </summary>
        public Dictionary<string, double> DataValues { get; } = new Dictionary<string, double>();

        /// <summary>
        /// Initializes a new forwarder message
        /// </summary>
        /// <param name="timestamp"></param>
        public ForwarderMessage(long timestamp)
        {
            Timestamp = timestamp;
        }
    }
}

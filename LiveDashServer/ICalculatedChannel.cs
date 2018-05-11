namespace LiveDashServer
{
    /// <summary>
    /// Defines a data channel where its data values are calculated dynamically from a set of other data channels
    /// </summary>
    public interface ICalculatedChannel
    {
        /// <summary>
        /// The name of the calculated channel
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The latest value that was calculated
        /// </summary>
        double CalculatedValue { get; }

        /// <summary>
        /// Notifies the calculated channel that one of its input channels has a new value.
        /// A new value should be calculated here
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="value"></param>
        void UpdateValue(string channelName, double value);
    }
}
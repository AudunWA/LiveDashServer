using System;
using System.Collections.Generic;
using System.Text;

namespace LiveDashServer
{
    /// <summary>
    /// A calculated data channel, which takes the euclidian distance (vector length) of 2 input channels.
    /// It's mainly used for channel pairs like velocity in the x and y direction
    /// </summary>
    class CalculatedChannelEuclidian : ICalculatedChannel
    {
        public string Name { get; }
        public string ChannelName1 { get; }
        public string ChannelName2 { get; }
        public double CalculatedValue { get; private set; }
        public double Multiplier { get; }


        private double _value1;
        private double _value2;

        /// <summary>
        /// Initializes a new custom euclidian channel
        /// </summary>
        /// <param name="name">The name of the calculated channel</param>
        /// <param name="channelName1">The name of the first input channel</param>
        /// <param name="channelName2">The name of the second input channel</param>
        /// <param name="multiplier">A constant which gets multiplied which each calculated value</param>
        public CalculatedChannelEuclidian(string name, string channelName1, string channelName2, double multiplier)
        {
            Name = name;
            ChannelName1 = channelName1;
            ChannelName2 = channelName2;
            Multiplier = multiplier;
        }


        /// <inheritdoc />
        public void UpdateValue(string channelName, double value)
        {
            if (channelName == ChannelName1)
            {
                _value1 = value;
            }
            else if (channelName == ChannelName2)
            {
                _value2 = value;
            }
            else
            {
                throw new ArgumentException("Channel name not used by this computed channel", nameof(channelName));
            }

            CalculatedValue = CalculateValue();
        }

        /// <summary>
        /// Calculates the euclidian distance of the two channel values, and multiplies the result with an multiplier
        /// </summary>
        /// <returns></returns>
        private double CalculateValue()
        {
            return Math.Sqrt(Math.Pow(_value1, 2) + Math.Pow(_value2, 2)) * Multiplier;
        }
    }
}

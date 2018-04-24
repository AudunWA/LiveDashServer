using System;
using System.Collections.Generic;
using System.Text;

namespace LiveDashServer
{
    class CalculatedChannelEuclidian : ICalculatedChannel
    {
        public string Name { get; }
        public string ChannelName1 { get; }
        public string ChannelName2 { get; }
        public double CalculatedValue { get; private set; }
        public double Multiplier { get; }


        private double _value1;
        private double _value2;

        public CalculatedChannelEuclidian(string name, string channelName1, string channelName2, double multiplier)
        {
            Name = name;
            ChannelName1 = channelName1;
            ChannelName2 = channelName2;
            Multiplier = multiplier;
        }


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

        private double CalculateValue()
        {
            return Math.Sqrt(Math.Pow(_value1, 2) + Math.Pow(_value2, 2)) * Multiplier;
        }
    }
}

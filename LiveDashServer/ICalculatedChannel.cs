namespace LiveDashServer
{
    public interface ICalculatedChannel
    {
        string Name { get; }

        double CalculatedValue { get; }

        void UpdateValue(string channelName, double value);
    }
}
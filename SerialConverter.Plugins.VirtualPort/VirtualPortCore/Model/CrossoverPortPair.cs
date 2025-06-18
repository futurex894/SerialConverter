namespace SerialConverter.Plugins.VirtualSerial.VirtualPortCore.Model
{
    public class CrossoverPortPair
    {
        public CrossoverPortPair(int number, string portNameA, string portNameB)
        {
            PairNumber = number;
            PortNameA = portNameA;
            PortNameB = portNameB;
        }
        public int PairNumber { get; }
        public string PortNameA { get; }
        public string PortNameB { get; }
    }
}


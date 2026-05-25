namespace ScadaNet.Core;

public class ScadaNetException : Exception
{
    public ScadaNetException(string message)
        : base(message)
    {
    }

    public ScadaNetException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

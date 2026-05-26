namespace ScadaNet.AspNetCore;

public sealed class ScadaNetOptionsValidationException : Exception
{
    public ScadaNetOptionsValidationException(IReadOnlyList<string> errors)
        : base($"ScadaNet configuration is invalid: {string.Join("; ", errors)}")
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }
}

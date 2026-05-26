namespace ScadaNet.Logix;

public interface ILogixClient : IAsyncDisposable
{
    ValueTask<object?> ReadAsync(
        string tagName,
        CancellationToken cancellationToken = default);

    ValueTask<T> ReadAsync<T>(
        string tagName,
        CancellationToken cancellationToken = default);

    ValueTask WriteAsync(
        string tagName,
        LogixDataTypeCode dataType,
        object? value,
        CancellationToken cancellationToken = default);
}

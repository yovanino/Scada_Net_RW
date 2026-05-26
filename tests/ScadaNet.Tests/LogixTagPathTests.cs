using ScadaNet.Logix;

namespace ScadaNet.Tests;

public class LogixTagPathTests
{
    [Fact]
    public void Encode_encodes_single_symbol_segment()
    {
        var encoded = LogixTagPath.Encode("Counter");

        Assert.Equal(
            [0x91, 0x07, (byte)'C', (byte)'o', (byte)'u', (byte)'n', (byte)'t', (byte)'e', (byte)'r', 0x00],
            encoded);
    }

    [Fact]
    public void Encode_encodes_udt_member_segments()
    {
        var encoded = LogixTagPath.Encode("Motor.Speed");

        Assert.Equal(
            [
                0x91, 0x05, (byte)'M', (byte)'o', (byte)'t', (byte)'o', (byte)'r', 0x00,
                0x91, 0x05, (byte)'S', (byte)'p', (byte)'e', (byte)'e', (byte)'d', 0x00
            ],
            encoded);
    }

    [Fact]
    public void Encode_encodes_array_index_segments()
    {
        var encoded = LogixTagPath.Encode("Counters[3]");

        Assert.Equal(
            [
                0x91, 0x08, (byte)'C', (byte)'o', (byte)'u', (byte)'n',
                (byte)'t', (byte)'e', (byte)'r', (byte)'s',
                0x28, 0x03
            ],
            encoded);
    }

    [Fact]
    public void Encode_encodes_udt_member_array_index_segments()
    {
        var encoded = LogixTagPath.Encode("Recipe.Steps[260].Target");

        Assert.Equal(
            [
                0x91, 0x06, (byte)'R', (byte)'e', (byte)'c', (byte)'i', (byte)'p', (byte)'e',
                0x91, 0x05, (byte)'S', (byte)'t', (byte)'e', (byte)'p', (byte)'s', 0x00,
                0x29, 0x00, 0x04, 0x01,
                0x91, 0x06, (byte)'T', (byte)'a', (byte)'r', (byte)'g', (byte)'e', (byte)'t'
            ],
            encoded);
    }

    [Fact]
    public void Encode_encodes_multi_dimensional_indexes()
    {
        var encoded = LogixTagPath.Encode("Matrix[1,2]");

        Assert.Equal(
            [
                0x91, 0x06, (byte)'M', (byte)'a', (byte)'t', (byte)'r', (byte)'i', (byte)'x',
                0x28, 0x01,
                0x28, 0x02
            ],
            encoded);
    }

    [Fact]
    public void Encode_rejects_non_ascii_segments()
    {
        var error = Assert.Throws<ArgumentException>(() => LogixTagPath.Encode("Linea.ProduccionÑ"));

        Assert.Contains("non-ASCII", error.Message);
    }

    [Fact]
    public void Encode_rejects_invalid_index_segments()
    {
        Assert.Throws<ArgumentException>(() => LogixTagPath.Encode("Counters[-1]"));
        Assert.Throws<ArgumentException>(() => LogixTagPath.Encode("Counters[]"));
        Assert.Throws<ArgumentException>(() => LogixTagPath.Encode("Counters[1"));
        Assert.Throws<ArgumentException>(() => LogixTagPath.Encode("[1]"));
    }
}

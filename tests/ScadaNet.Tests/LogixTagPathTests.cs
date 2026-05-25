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
    public void Encode_rejects_non_ascii_segments()
    {
        var error = Assert.Throws<ArgumentException>(() => LogixTagPath.Encode("Linea.ProduccionÑ"));

        Assert.Contains("non-ASCII", error.Message);
    }
}

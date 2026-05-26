using ScadaNet.Cip;

namespace ScadaNet.Tests;

public class CipRoutePathTests
{
    [Fact]
    public void Encode_returns_port_link_pairs()
    {
        var encoded = CipRoutePath.Encode("1,0");

        Assert.Equal([0x01, 0x00], encoded);
    }

    [Fact]
    public void Encode_rejects_odd_number_of_segments()
    {
        Assert.Throws<ArgumentException>(() => CipRoutePath.Encode("1,0,2"));
    }
}

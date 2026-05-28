using ScadaNet.Protocols;

namespace ScadaNet.Tests;

public class KnownProtocolMetadataTests
{
    [Fact]
    public void Known_protocol_families_include_planned_ethernet_protocols()
    {
        Assert.Equal("EtherNet/IP", KnownProtocolFamilies.EtherNetIp);
        Assert.Equal("Modbus TCP", KnownProtocolFamilies.ModbusTcp);
        Assert.Equal("OPC UA", KnownProtocolFamilies.OpcUa);
        Assert.Equal("MQTT", KnownProtocolFamilies.Mqtt);
        Assert.Equal("PROFINET", KnownProtocolFamilies.Profinet);
        Assert.Equal("Siemens S7", KnownProtocolFamilies.SiemensS7);
    }

    [Fact]
    public void Known_protocol_ports_include_default_tcp_ports()
    {
        Assert.Equal(44818, KnownProtocolPorts.EtherNetIpExplicitMessaging);
        Assert.Equal(2222, KnownProtocolPorts.EtherNetIpImplicitMessaging);
        Assert.Equal(502, KnownProtocolPorts.ModbusTcp);
        Assert.Equal(4840, KnownProtocolPorts.OpcUa);
        Assert.Equal(1883, KnownProtocolPorts.Mqtt);
        Assert.Equal(8883, KnownProtocolPorts.MqttTls);
        Assert.Equal(102, KnownProtocolPorts.SiemensS7);
    }
}

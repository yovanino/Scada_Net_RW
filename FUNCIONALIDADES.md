# Plataforma .NET De Comunicacion Industrial

## Objetivo

Crear una libreria/plataforma .NET nativa para comunicacion industrial multi-protocolo, con un runtime comun para dispositivos, pooling, cache, snapshots y telemetria.

El primer driver fuerte sera EtherNet/IP + CIP, con una capa de alto nivel fuertemente optimizada para Allen-Bradley Logix, especialmente lectura, escritura y modelado de UDTs.

La comunicacion directa sera por Ethernet. No se implementara comunicacion serial directa en el alcance inicial. Si un equipo solo soporta serial, debera integrarse mediante un gateway Ethernet.

La arquitectura debe quedar preparada para sumar otros protocolos industriales sobre Ethernet sin redisenar el runtime:

- EtherNet/IP + CIP.
- Omron FINS Ethernet.
- Siemens S7 ISO-on-TCP.
- Mitsubishi SLMP/MC Protocol.
- Panasonic MEWTOCOL.
- Beckhoff ADS.
- Modbus TCP.
- PROFINET.
- OPC UA.
- MQTT.

La libreria estara orientada principalmente a uso desde APIs ASP.NET Core, MVC, Blazor Server y servicios backend, con foco en performance, pooling de dispositivos, cache de metadata y operaciones asincronicas.

## Aplicacion Objetivo

La primera aplicacion concreta sera una API industrial que formara parte de un sistema mayor tipo SCADA/MES liviano para control de produccion en tiempo real.

Casos de uso esperados:

- Monitoreo de produccion en tiempo real.
- Calculo de OEE.
- Indicadores de disponibilidad, rendimiento y calidad.
- Estado de maquinas, lineas, robots y estaciones.
- Integracion con pantallas HMI.
- Integracion con ladder del PLC.
- Lectura de datos de proceso.
- Escritura de comandos controlados.
- Captura de eventos productivos.
- Publicacion de datos a dashboards Blazor.
- Exposicion de endpoints API para otros modulos del sistema.

El diseno debe contemplar que la libreria no sera solo un driver, sino una base de comunicacion para una plataforma industrial con multiples dispositivos y alta frecuencia de datos.

## Principios

- Implementacion .NET nativa, sin dependencias C/C++.
- Diseno clean-room: no copiar codigo, documentacion ni estructura interna de librerias existentes.
- API async-first con `CancellationToken`.
- Integracion natural con Dependency Injection de ASP.NET Core.
- Runtime comun independiente del protocolo.
- Drivers separados por protocolo.
- Nucleo CIP generico, no limitado a Allen-Bradley.
- Capa Logix especializada para tags, UDTs, arrays y strings.
- Performance como requisito central: pooling, batching, cache y backpressure.
- Observabilidad desde el inicio: logs, health checks, metricas y diagnostico.

## Arquitectura Por Capas

La plataforma se organizara en capas estrictamente separadas, desde lo mas generico hasta lo mas especifico por dispositivo. Cada capa solo debe conocer a la capa inmediatamente inferior mediante contratos publicos.

Objetivos de esta separacion:

- Evitar que un protocolo contamine el runtime comun.
- Evitar que Allen-Bradley Logix condicione drivers de otras marcas.
- Permitir agregar dispositivos sin redisenar la API.
- Permitir que discovery, health checks, snapshots y polling funcionen igual para todos los drivers.
- Mantener testeable cada capa de forma aislada.

### Capa 0 - Infraestructura Binaria Y Utilidades

Responsabilidad:

- Tipos comunes.
- Errores base.
- Lectura/escritura binaria.
- Buffers.
- Endianess.
- Timeouts.
- Cancelacion.
- Reintentos.
- Logging y metricas base.

No debe conocer:

- Protocolos industriales.
- IPs de dispositivos.
- Tags.
- PLCs.
- UDTs.

Proyectos candidatos:

- `PlcNet.Core`
- `PlcNet.Buffers`
- `PlcNet.Diagnostics`

### Capa 1 - Transporte

Responsabilidad:

- TCP.
- UDP.
- TLS cuando aplique.
- Sockets.
- Conexiones.
- Reconexiones.
- Envio/recepcion de bytes.
- Control de timeouts de red.

Ejemplos:

- TCP para EtherNet/IP explicito.
- UDP para EtherNet/IP implicito.
- TCP para Modbus TCP.
- TCP para OPC UA.
- TCP para MQTT.
- TCP para S7 ISO-on-TCP.

No debe conocer:

- Tags Logix.
- Registros Modbus.
- UDTs.
- Objetos especificos de PLC.

Proyectos candidatos:

- `PlcNet.Transport`
- `PlcNet.Transport.Tcp`
- `PlcNet.Transport.Udp`

### Capa 2 - Protocolo Industrial

Responsabilidad:

- Implementar el protocolo en si.
- Codificar requests.
- Decodificar responses.
- Validar status.
- Manejar sesiones propias del protocolo.
- Exponer primitivas genericas del protocolo.

Ejemplos:

- EtherNet/IP encapsulation.
- CIP services.
- Modbus TCP function codes.
- OPC UA services.
- MQTT packets.
- Mitsubishi SLMP/MC.
- Omron FINS.
- Panasonic MEWTOCOL.
- Siemens S7.
- Beckhoff ADS.

No debe conocer:

- La aplicacion SCADA/OEE.
- Grupos de polling.
- Snapshots.
- HMI.
- Ladder.

Proyectos candidatos:

- `PlcNet.EtherNetIp`
- `PlcNet.Cip`
- `PlcNet.ModbusTcp`
- `PlcNet.OpcUa`
- `PlcNet.Mqtt`
- `PlcNet.MitsubishiMelsec`
- `PlcNet.Omron`
- `PlcNet.PanasonicFp`
- `PlcNet.SiemensS7`
- `PlcNet.BeckhoffAds`

### Capa 3 - Driver De Familia O Marca

Responsabilidad:

- Convertir primitivas de protocolo en operaciones utiles para una familia de dispositivos.
- Definir direccionamiento de datos.
- Definir capacidades de lectura/escritura.
- Definir metadata propia de familia.
- Implementar optimizaciones de lectura/escritura del fabricante.

Ejemplos:

- `LogixDriver`: tags por nombre, UDTs, arrays, strings.
- `MitsubishiFxDriver`: dispositivos `D`, `M`, `X`, `Y`.
- `OmronFinsDriver`: areas de memoria Omron.
- `PanasonicFpDriver`: contactos/registros FP.
- `SiemensS7Driver`: DBs, marcas, entradas y salidas.
- `BeckhoffAdsDriver`: variables TwinCAT por nombre/handle.
- `ModbusDeviceDriver`: coils y registers.
- `OpcUaDeviceDriver`: NodeIds y subscriptions.
- `MqttDeviceDriver`: topics y payloads.

No debe conocer:

- Controllers MVC.
- Componentes Blazor.
- Base de datos historica.
- Calculo de OEE.

Proyectos candidatos:

- `PlcNet.Logix`
- `PlcNet.MitsubishiMelsec.Fx`
- `PlcNet.Omron.Fins`
- `PlcNet.PanasonicFp`
- `PlcNet.SiemensS7`
- `PlcNet.BeckhoffAds`

### Capa 4 - Perfil De Dispositivo

Responsabilidad:

- Modelar un dispositivo concreto o tipo concreto de equipo.
- Definir su configuracion.
- Definir sus signals conocidas.
- Definir permisos de escritura.
- Definir health check propio.
- Definir mapeos entre direcciones fisicas y signals normalizadas.

Ejemplos:

- `ControlLogixDevice`.
- `CompactLogixDevice`.
- `Micro800Device`.
- `YaskawaMotomanDevice`.
- `IfmIoLinkMasterDevice`.
- `BalluffIoLinkMasterDevice`.
- `MitsubishiFx5Device`.
- `PanasonicFpxDevice`.
- `OmronNjDevice`.
- `SiemensS71200Device`.
- `BeckhoffTwinCatDevice`.

Esta capa puede conocer:

- Driver usado.
- Modelo/familia.
- Opciones concretas de conexion.
- Tags/direcciones esperadas.
- Reglas de seguridad por dispositivo.

No debe conocer:

- Como se renderiza una pantalla.
- Como se calcula un KPI.
- Como se persiste historico.

Proyecto candidato:

- `PlcNet.Devices`

### Capa 5 - Modelo Comun De Datos

Responsabilidad:

- Normalizar cualquier dato industrial como signal.
- Definir nombres logicos.
- Definir tipo de dato.
- Definir calidad.
- Definir timestamp.
- Definir permisos.
- Definir unidades.
- Definir metadata.

Conceptos:

- `SignalRef`.
- `SignalValue`.
- `SignalQuality`.
- `DeviceIdentity`.
- `DeviceCapability`.
- `ReadRequest`.
- `WriteRequest`.
- `DeviceSnapshot`.

Ejemplo:

```csharp
public sealed record SignalRef(string DeviceName, string Address);

public sealed record SignalValue(
    SignalRef Ref,
    object? Value,
    SignalQuality Quality,
    DateTimeOffset Timestamp);
```

Esta capa permite que la API lea datos de Logix, Modbus, OPC UA, MQTT o S7 con la misma forma.

Proyecto candidato:

- `PlcNet.Model`

### Capa 6 - Runtime Industrial

Responsabilidad:

- Administrar dispositivos.
- Administrar pools.
- Administrar conexiones.
- Ejecutar polling.
- Aplicar backpressure.
- Ordenar requests.
- Agrupar lecturas.
- Mantener snapshots.
- Cachear metadata.
- Exponer metricas y health checks.

Esta es la capa central para performance.

Componentes:

- `DeviceRegistry`.
- `DevicePool`.
- `ConnectionPool`.
- `RequestScheduler`.
- `PollingEngine`.
- `SnapshotStore`.
- `MetadataCache`.
- `UdtMetadataCache`.
- `DiscoveryService`.
- `HealthMonitor`.
- `MetricsCollector`.

No debe conocer:

- Controllers MVC concretos.
- Paginas Blazor concretas.
- Pantallas HMI concretas.

Proyecto candidato:

- `PlcNet.Runtime`

### Capa 7 - Integracion ASP.NET Core

Responsabilidad:

- Dependency Injection.
- Configuracion desde `appsettings.json`.
- Hosted services.
- Health checks.
- Logging.
- Metrics.
- Factories.
- APIs listas para MVC, Minimal APIs y Blazor Server.

Ejemplos:

- `AddPlcNet`.
- `IPlcRuntime`.
- `IPlcDeviceRegistry`.
- `IPlcSnapshotStore`.
- `IPlcDiscoveryService`.
- `PlcHealthCheck`.

Proyecto candidato:

- `PlcNet.AspNetCore`

### Capa 8 - Aplicacion Industrial

Responsabilidad:

- API de produccion.
- SCADA/MES/OEE.
- Dashboards.
- HMI.
- Reglas de negocio.
- Historico.
- Alarmas.
- Reportes.
- Handshake de produccion.
- Coordinacion con ladder PLC y robot.

Esta capa puede usar todo lo anterior, pero no debe implementar protocolos industriales directamente.

Ejemplos:

- `ProductionApi`.
- `OeeService`.
- `LineDashboard`.
- `RobotCellMonitor`.
- `HmiBackend`.

## Flujo De Dependencias

```text
Aplicacion Industrial
    -> PlcNet.AspNetCore
        -> PlcNet.Runtime
            -> PlcNet.Model
            -> PlcNet.Devices
                -> Drivers de familia/marca
                    -> Protocolos industriales
                        -> Transportes
                            -> Core/Buffers/Diagnostics
```

Regla:

- Una capa superior puede usar una capa inferior.
- Una capa inferior nunca debe depender de una capa superior.
- Los drivers no deben depender de ASP.NET Core.
- Los protocolos no deben depender del runtime.
- Los perfiles de dispositivo no deben implementar sockets ni codificacion binaria.

## Separacion Por Nivel De Especificidad

```text
Generico:
  Core, Transport, Protocols, Model, Runtime

Protocolo:
  EtherNet/IP, CIP, Modbus TCP, OPC UA, MQTT, FINS, SLMP, MEWTOCOL, S7, ADS

Familia/Marca:
  Logix, Omron, Mitsubishi, Panasonic, Siemens, Beckhoff

Dispositivo:
  ControlLogix, CompactLogix, YaskawaMotoman, FX5, FP-X, NJ, S7-1200

Aplicacion:
  OEE, SCADA, HMI, produccion, robot cell, dashboards
```

## Contratos Entre Capas

Los contratos principales viviran en capas genericas para que los drivers se puedan intercambiar.

```csharp
public interface IDeviceDriver
{
    string DriverName { get; }
    ValueTask<IDeviceConnection> ConnectAsync(DeviceConnectionOptions options, CancellationToken cancellationToken);
    ValueTask<DeviceDetectionResult> ProbeAsync(ProbeRequest request, CancellationToken cancellationToken);
}

public interface IDeviceConnection : IAsyncDisposable
{
    DeviceIdentity Identity { get; }
    DeviceCapabilities Capabilities { get; }
    ValueTask<SignalValue> ReadAsync(SignalRef signal, CancellationToken cancellationToken);
    ValueTask<IReadOnlyList<SignalValue>> ReadManyAsync(IReadOnlyList<SignalRef> signals, CancellationToken cancellationToken);
    ValueTask WriteAsync(SignalRef signal, object? value, CancellationToken cancellationToken);
}
```

Las capacidades permitiran distinguir dispositivos read-only, write-enabled, subscribable o pollable.

```csharp
[Flags]
public enum DeviceCapabilities
{
    None = 0,
    Read = 1,
    Write = 2,
    ReadMany = 4,
    Subscribe = 8,
    Poll = 16,
    Discover = 32,
    Metadata = 64,
    UdtMetadata = 128,
    ImplicitIo = 256
}
```

## Fuera De Alcance Inicial

- PCCC.
- Comunicacion serial directa.
- Modbus RTU directo.
- RS-232/RS-485 directo.
- PLC-5.
- SLC 500.
- MicroLogix clasico por direccionamiento PCCC.
- Direcciones tipo `N7:0`, `B3:4/2`, `F8:0`.
- DH+ bridges.
- Port directo o traduccion de codigo de otras librerias.

## Modulos Propuestos

### PlcNet.Core

Funcionalidades base compartidas:

- Errores y excepciones comunes.
- Tipos de resultado.
- Abstracciones comunes de dispositivo.
- Abstracciones comunes de lectura/escritura.
- Modelo comun de tags, puntos, variables o signals.
- Utilidades binarias con `Span<byte>` y `Memory<byte>`.
- Manejo de endianess.
- Abstracciones de transporte.
- Timeouts.
- Cancelacion.
- Reintentos configurables.
- Backpressure.
- Telemetria base.

### PlcNet.Protocols

Contratos comunes para drivers:

- `IIndustrialDevice`.
- `IDeviceDriver`.
- `IDeviceConnection`.
- `IReadOnlyDevice`.
- `IWritableDevice`.
- `ISubscribableDevice`.
- `IPollableDevice`.
- `IDeviceMetadataProvider`.
- `IDeviceHealthProvider`.

Objetivo:

- Permitir que EtherNet/IP, Modbus TCP, PROFINET, OPC UA y MQTT compartan runtime.
- Evitar que la API principal dependa de detalles internos de un protocolo.
- Permitir lecturas cruzadas entre dispositivos de diferentes protocolos.
- Normalizar snapshots, metricas, health checks y errores.

### PlcNet.Discovery

Modulo de autodeteccion de dispositivos y capacidades.

Objetivo:

- Dada una IP y opcionalmente un puerto, detectar si el dispositivo responde a alguno de los protocolos soportados.
- Identificar el driver mas probable.
- Determinar capacidades de lectura/escritura sin ejecutar escrituras durante la deteccion.
- Facilitar configuracion inicial de plantas con muchos dispositivos.

Principios:

- La deteccion debe ser read-only.
- Nunca escribir salidas, coils, registros, tags ni assemblies durante discovery.
- Usar timeouts cortos y configurables.
- Limitar concurrencia para no saturar redes industriales.
- Registrar evidencia de cada probe.
- Devolver resultado con nivel de confianza, no asumir compatibilidad total por un solo puerto abierto.

Probes esperados:

- EtherNet/IP:
  - TCP 44818.
  - `RegisterSession`.
  - `ListIdentity` o lectura de Identity Object.
  - Deteccion de vendor id, device type, product code, revision y product name.
- Modbus TCP:
  - TCP 502.
  - Prueba de conexion.
  - Lectura opcional controlada de un rango configurado por el usuario.
  - No asumir mapa de registros sin configuracion.
- Mitsubishi SLMP/MC:
  - Puerto configurable, comunmente 5000 o el definido en PLC.
  - Probe de protocolo con comando read-only seguro.
  - Deteccion por respuesta valida.
- Panasonic MEWTOCOL Ethernet:
  - Puerto configurable segun modulo/configuracion.
  - Probe read-only seguro.
  - Deteccion por respuesta valida.
- OPC UA:
  - TCP 4840 o endpoint configurado.
  - GetEndpoints.
  - lectura de server status si corresponde.
- MQTT:
  - TCP 1883 o 8883.
  - Conexion al broker con credenciales si aplica.
  - No detectar dispositivos detras del broker sin convenciones de topics.
- PROFINET:
  - Deteccion limitada en alcance inicial.
  - Preferir integracion por configuracion, gateway o inventario externo.

Resultado esperado:

```csharp
public sealed class DeviceDetectionResult
{
    public string Address { get; init; }
    public int? Port { get; init; }
    public IReadOnlyList<ProtocolProbeResult> Probes { get; init; }
    public string? RecommendedDriver { get; init; }
    public double Confidence { get; init; }
    public DeviceIdentity? Identity { get; init; }
    public IReadOnlyList<string> Capabilities { get; init; }
}
```

Ejemplo de uso:

```csharp
var result = await discovery.DetectAsync(
    address: "192.168.0.10",
    ports: new[] { 44818, 502, 4840 },
    cancellationToken);

if (result.RecommendedDriver == "EtherNetIp")
{
    Console.WriteLine(result.Identity?.ProductName);
}
```

### PlcNet.EtherNetIp

Capa de transporte EtherNet/IP:

- Encapsulacion EtherNet/IP.
- Sesiones TCP.
- Registro de sesion.
- Cierre de sesion.
- Envio de paquetes encapsulados.
- Manejo de responses.
- Reconexion.
- Soporte para TCP explicito.
- Base para UDP implicito.

### PlcNet.Cip

Capa CIP generica:

- Paths CIP.
- Servicios CIP genericos.
- `GetAttributeSingle`.
- `SetAttributeSingle`.
- Envio de servicios custom.
- Manejo de class / instance / attribute.
- Decodificacion de status CIP.
- Mensajeria no conectada.
- Mensajeria conectada.
- Soporte para Connection Manager.

### PlcNet.Logix

Capa Allen-Bradley Logix:

- Cliente de alto nivel `LogixClient`.
- Lectura de tags por nombre.
- Escritura de tags por nombre.
- Lectura multiple.
- Escritura multiple.
- Tags globales.
- Tags de programa.
- Fragmentacion de lectura.
- Fragmentacion de escritura.
- Multi-service packet cuando corresponda.
- Descubrimiento/listado de tags.
- Cache de metadata de tags.

### PlcNet.Logix.Types

Sistema de tipos Logix:

- `BOOL`.
- `SINT`.
- `INT`.
- `DINT`.
- `LINT`.
- `REAL`.
- `LREAL`.
- `STRING`.
- Arrays 1D.
- Arrays 2D.
- Arrays 3D.
- Structs.
- Tipos anidados.
- Codificacion y decodificacion binaria.

### PlcNet.Logix.Udt

Soporte fuerte para UDTs:

- Lectura de metadata de UDT.
- Modelo de UDT con nombre, campos, offsets y tamanos.
- Resolucion de tipos de campos.
- UDTs anidados.
- Arrays dentro de UDTs.
- Strings dentro de UDTs.
- Cache de layout de UDT.
- Decodificacion a estructura dinamica.
- Decodificacion a tipos C#.
- Escritura de UDT completo.
- Escritura parcial de campos.
- Validacion de layout.
- Generacion opcional de mappers compilados para evitar reflection repetida.

### PlcNet.ImplicitIo

Mensajeria implicita EtherNet/IP:

- Soporte de assemblies de entrada.
- Soporte de assemblies de salida.
- Soporte de assembly de configuracion.
- Conexion I/O como scanner.
- Recepcion UDP.
- Envio UDP.
- Requested Packet Interval.
- Watchdog.
- Control de estado de conexion.
- Buffers de entrada/salida.
- Eventos de datos recibidos.
- Base futura para modo adapter.

### PlcNet.Eds

Modulo opcional para dispositivos de terceros:

- Parser de archivos EDS.
- Lectura de assemblies declarados.
- Lectura de parametros.
- Ayuda para configurar implicit messaging.
- Mapeo de datos de dispositivos IFM, Balluff, Turck, Festo, Sick, Keyence u otros.

### PlcNet.ModbusTcp

Driver Modbus TCP.

Objetivo:

- Leer y escribir dispositivos Modbus TCP desde el mismo runtime.
- Integrar medidores, variadores, balanzas, sensores, gateways y PLCs que expongan Modbus TCP.
- Mapear registros Modbus a signals normalizadas.

Funcionalidades esperadas:

- Cliente TCP Modbus.
- Read Coils.
- Read Discrete Inputs.
- Read Holding Registers.
- Read Input Registers.
- Write Single Coil.
- Write Single Register.
- Write Multiple Coils.
- Write Multiple Registers.
- Unit ID configurable.
- Endianess configurable por dispositivo o signal.
- Conversion de registros a tipos .NET.
- Agrupamiento de lecturas contiguas.
- Polling optimizado por rangos.
- Snapshot store compartido con el runtime.

### PlcNet.MitsubishiMelsec

Driver para PLCs Mitsubishi MELSEC, con foco inicial en familia FX moderna.

Objetivo:

- Leer y escribir dispositivos Mitsubishi desde el mismo runtime.
- Soportar FX5/iQ-F por Ethernet usando SLMP/MC Protocol cuando este disponible.
- Permitir integracion de FX3/FX3U si cuenta con modulo Ethernet compatible.
- Exponer datos Mitsubishi como signals normalizadas para API, dashboards y OEE.

Funcionalidades esperadas:

- Cliente SLMP/MC Protocol sobre TCP.
- Lectura de dispositivos Mitsubishi:
  - `D` data registers.
  - `M` internal relays.
  - `X` inputs.
  - `Y` outputs.
  - `R` file registers, si aplica.
  - otros dispositivos segun familia.
- Escritura controlada de dispositivos.
- Lectura por bloques.
- Agrupamiento de direcciones contiguas.
- Conversion a tipos .NET.
- Configuracion por estacion/red cuando aplique.
- Health check por PLC.
- Snapshot store compartido.

Notas:

- En FX5/iQ-F, Mitsubishi documenta comunicacion Ethernet mediante SLMP/MC Protocol y tambien funciones de MODBUS/TCP, segun configuracion y firmware.
- En FX3/FX3U normalmente se requiere modulo/adaptador Ethernet, por ejemplo familia FX3U-ENET/ENET-ADP, y la disponibilidad exacta depende del modulo y configuracion.
- Para integracion rapida, Modbus TCP puede ser opcion en FX5 si se configura; para acceso mas nativo a dispositivos Mitsubishi conviene SLMP/MC.

### PlcNet.Omron

Driver para PLCs Omron sobre Ethernet.

Objetivo:

- Leer y escribir PLCs Omron desde el mismo runtime.
- Soportar familias NJ/NX/CP/CJ/CS segun protocolo disponible.
- Integrar equipos Omron en dashboards, OEE y API industrial.

Rutas de comunicacion esperadas:

- EtherNet/IP + CIP para equipos Omron que lo soporten.
- FINS Ethernet para acceso nativo Omron.
- OPC UA en controladores que lo expongan.
- Modbus TCP si el equipo o gateway lo permite.

Funcionalidades esperadas:

- Cliente FINS por UDP/TCP.
- Lectura de areas de memoria Omron.
- Escritura controlada de areas permitidas.
- Lectura por bloques.
- Agrupamiento de direcciones contiguas.
- Conversion a tipos .NET.
- Configuracion de network/node/unit.
- Health check por PLC.
- Snapshot store compartido.

Notas:

- En NJ/NX puede existir integracion por EtherNet/IP/CIP, pero el modelo de datos no debe asumirse igual a Allen-Bradley Logix.
- FINS sigue siendo una ruta importante para PLCs Omron.
- OPC UA puede ser preferible si el controlador ya lo tiene configurado y se busca interoperabilidad.

### PlcNet.PanasonicFp

Driver para PLCs Panasonic FP, con foco en FP-X/FP-XH.

Objetivo:

- Leer y escribir PLCs Panasonic FP desde el mismo runtime.
- Soportar MEWTOCOL/Computer Link sobre Ethernet cuando este disponible.
- Integrar FP-X/FP-XH en dashboards, OEE y API industrial.

Funcionalidades esperadas:

- Cliente MEWTOCOL por TCP/UDP segun hardware disponible.
- Lectura de contactos y registros Panasonic.
- Escritura controlada de contactos y registros.
- Lectura por bloques.
- Agrupamiento de direcciones contiguas.
- Conversion a tipos .NET.
- Configuracion de station number.
- Health check por PLC.
- Snapshot store compartido.

Notas:

- FP-X/FP-XH puede requerir cassette o modulo de comunicacion para Ethernet.
- Panasonic documenta soporte de Modbus-RTU en FP-XH, pero no se implementara RTU directo en el alcance inicial.
- Si se necesita RTU, se integrara mediante gateway Ethernet a Modbus TCP, OPC UA, MQTT u otro protocolo soportado.
- Modbus TCP dependera del equipo, modulo, gateway o arquitectura concreta.
- MEWTOCOL/Computer Link es la ruta mas especifica de Panasonic para acceso a datos FP.
- Si se usa gateway externo a Modbus TCP, el dispositivo puede entrar por `PlcNet.ModbusTcp` sin driver Panasonic dedicado.

### PlcNet.SiemensS7

Driver para PLCs Siemens S7 sobre Ethernet.

Objetivo:

- Leer y escribir PLCs Siemens desde el mismo runtime cuando no se use OPC UA.
- Soportar integracion con S7-300, S7-400, S7-1200 y S7-1500 segun configuracion.
- Exponer datos Siemens como signals normalizadas.

Rutas de comunicacion esperadas:

- S7 ISO-on-TCP para lectura/escritura de datos.
- OPC UA para controladores que lo expongan.
- PROFINET para integracion I/O o via gateway/runtime especializado.

Funcionalidades esperadas:

- Cliente S7 por TCP.
- Lectura de DBs.
- Escritura controlada de DBs.
- Lectura de marcas, entradas y salidas cuando aplique.
- Agrupamiento de lecturas.
- Conversion a tipos .NET.
- Configuracion de rack/slot.
- Health check por PLC.
- Snapshot store compartido.

Notas:

- En S7-1200/S7-1500 puede requerirse habilitar acceso PUT/GET o usar OPC UA segun politica de seguridad.
- S7-1500 tiene restricciones adicionales segun configuracion de proteccion, bloques optimizados y permisos.
- Para integraciones nuevas y seguras, OPC UA puede ser la ruta preferida si esta disponible.

### PlcNet.BeckhoffAds

Driver para Beckhoff TwinCAT por ADS sobre Ethernet.

Objetivo:

- Leer y escribir variables TwinCAT desde el mismo runtime.
- Integrar Beckhoff en APIs, dashboards, OEE y captura de eventos.

Funcionalidades esperadas:

- Cliente ADS/TCP.
- Lectura de variables por nombre o handle.
- Escritura controlada de variables.
- Lectura multiple.
- Suscripciones/notificaciones si aplica.
- Cache de handles.
- Conversion a tipos .NET.
- Health check por runtime TwinCAT.
- Snapshot store compartido.

Notas:

- Beckhoff tambien puede exponer OPC UA, MQTT o Modbus TCP segun configuracion.
- ADS es la ruta mas nativa para TwinCAT.

### PlcNet.Profinet

Driver o integracion PROFINET.

Objetivo:

- Dejar preparada la arquitectura para dispositivos PROFINET.
- Evaluar el alcance real segun el tipo de comunicacion requerida.

Notas:

- PROFINET I/O en tiempo real suele requerir comportamiento de controlador/dispositivo y acceso a bajo nivel de red.
- Para una libreria .NET pura, puede ser necesario limitar el alcance inicial o integrarse con un runtime/gateway externo.
- El primer objetivo podria ser integracion via gateways industriales o stacks especializados.

Funcionalidades candidatas:

- Modelo comun de dispositivo PROFINET.
- Configuracion de slots/subslots.
- Mapeo de datos de entrada/salida.
- Health y diagnostico.
- Integracion futura con GSDML.
- Integracion mediante gateway PROFINET a OPC UA, MQTT, Modbus TCP o EtherNet/IP cuando convenga.

### PlcNet.OpcUa

Driver OPC UA.

Objetivo:

- Conectar la API a servidores OPC UA existentes.
- Integrar PLCs, SCADAs, gateways y servidores industriales que expongan variables por OPC UA.

Funcionalidades esperadas:

- Cliente OPC UA.
- Conexion segura configurable.
- Lectura de nodos.
- Escritura de nodos.
- Browsing de namespace.
- Suscripciones.
- Monitored items.
- Reconexion.
- Mapeo de NodeId a signals normalizadas.
- Snapshot store compartido.
- Health checks por servidor.

### PlcNet.Mqtt

Driver MQTT.

Objetivo:

- Integrar datos publicados por dispositivos, gateways, edge services o sistemas externos.
- Publicar eventos, snapshots o comandos hacia otros sistemas.

Funcionalidades esperadas:

- Cliente MQTT.
- Conexion a broker.
- TLS y autenticacion.
- Subscribe.
- Publish.
- Retained messages.
- QoS configurable.
- Mapeo topic -> signal.
- Mapeo signal -> topic.
- Payload JSON.
- Payload binario opcional.
- Integracion con Sparkplug B como extension futura.
- Snapshot store compartido.
- Buffering ante desconexion.
- Reintentos y backoff.

### Marcas Cubiertas Por Protocolos Existentes

Algunas marcas no necesitan driver especifico inicial si se integran por protocolos ya contemplados:

- Schneider Electric: Modbus TCP, EtherNet/IP, OPC UA segun familia.
- WAGO: Modbus TCP, OPC UA, MQTT segun controlador.
- Phoenix Contact: Modbus TCP, OPC UA, PROFINET segun equipo.
- Turck: EtherNet/IP, Modbus TCP, PROFINET, IO-Link masters via Ethernet.
- Balluff: EtherNet/IP, PROFINET, IO-Link masters via Ethernet.
- IFM: EtherNet/IP, PROFINET, IO-Link masters/gateways, MQTT en algunos equipos/gateways.
- Festo: EtherNet/IP, PROFINET, Modbus TCP, OPC UA segun dispositivo.
- Keyence: EtherNet/IP, PROFINET, TCP/UDP propio o gateways segun equipo.
- Sick: EtherNet/IP, PROFINET, OPC UA, MQTT/gateways segun equipo.
- Delta: Modbus TCP, Ethernet/IP u OPC UA segun familia.
- LS Electric: Modbus TCP, EtherNet/IP, OPC UA segun familia.

Si una marca requiere acceso nativo que no encaje bien en esos protocolos, se agregara como driver dedicado.

### PlcNet.Robotics.YaskawaMotoman

Modulo especifico para robots Yaskawa/Motoman sobre EtherNet/IP.

Objetivo:

- Leer y escribir datos del robot desde la API.
- Integrar robots Motoman dentro del mismo runtime de dispositivos.
- Soportar comunicacion por assemblies usando implicit messaging.
- Soportar explicit messaging para datos o funciones disponibles por CIP.
- Modelar el robot como un dispositivo de produccion dentro del sistema SCADA/MES.
- Facilitar el diseno conjunto de API, ladder PLC y pantalla HMI.

Funcionalidades esperadas:

- Perfil de dispositivo `YaskawaMotomanDevice`.
- Configuracion de controlador, por ejemplo YRC1000 o YRC1000micro.
- Configuracion de rol EtherNet/IP: adapter o scanner, segun el caso.
- Mapeo de entradas/salidas de robot por assemblies.
- Lectura de estado de robot desde datos I/O.
- Escritura de comandos permitidos hacia el robot.
- Modelo de senales estandar:
  - servo on/off.
  - teach/play/remote.
  - running.
  - alarm/fault.
  - hold.
  - cycle start.
  - job select/request.
  - job running/completed.
  - program number.
  - step/state.
- Mapeo configurable por proyecto, porque las asignaciones I/O pueden variar por celda.
- Plantillas para handshake PLC-robot.
- Validaciones para evitar escrituras inseguras desde API.
- Snapshot de estado para dashboards.
- Eventos de cambio de estado.

Notas de integracion:

- En controladores YRC1000/YRC1000micro, Yaskawa documenta que pueden configurarse como EtherNet/IP Adapter o Scanner cuando la opcion esta habilitada.
- Para integracion con PLC Allen-Bradley, el escenario comun sera configurar el robot como Adapter y el PLC o la API/runtime como Scanner, segun la arquitectura elegida.
- En YRC1000, Yaskawa indica que LAN2 es el puerto destinado a EtherNet/IP cuando la opcion esta habilitada.
- La API no debe asumir un mapa fijo universal de I/O; debe permitir perfiles por robot/celda.

### PlcNet.Runtime

Runtime multi-dispositivo:

- Registro de dispositivos.
- Pool de dispositivos.
- Pool de conexiones por dispositivo.
- Drivers por protocolo.
- Lecturas cruzadas multi-protocolo.
- Scheduler de requests.
- Colas por dispositivo.
- Priorizacion de requests.
- Limites de concurrencia.
- Batching de lecturas.
- Circuit breaker por dispositivo.
- Rate limit por dispositivo.
- Snapshot store.
- Polling engine.
- Cache de metadata.
- Metricas de latencia, errores, throughput y reconexiones.

### PlcNet.AspNetCore

Integracion web:

- Registro por Dependency Injection.
- Configuracion desde `appsettings.json`.
- Factories de clientes.
- Health checks.
- Hosted services para polling.
- Integracion con logging de ASP.NET Core.
- Integracion con metricas de .NET.
- APIs preparadas para MVC, Minimal APIs y Blazor Server.

## API Publica Esperada

### Registro En ASP.NET Core

```csharp
builder.Services.AddPlcNet(options =>
{
    options.AddLogixDevice("Line1", device =>
    {
        device.Address = "192.168.0.10";
        device.Path = "1,0";
        device.MaxConnections = 4;
        device.MaxConcurrentRequests = 16;
        device.RequestTimeout = TimeSpan.FromSeconds(2);
        device.EnableBatching = true;
        device.EnableMetadataCache = true;
    });
});
```

### Lectura Simple

```csharp
var value = await plcRuntime.ReadAsync<int>(
    new PlcTagRef("Line1", "Motor_01.Speed"),
    cancellationToken);
```

### Lectura Multiple

```csharp
var values = await plcRuntime.ReadManyAsync(new[]
{
    new PlcTagRef("Line1", "Motor_01.Speed"),
    new PlcTagRef("Line1", "Motor_01.Running"),
    new PlcTagRef("Line2", "Tank.Level")
}, cancellationToken);
```

### Lectura Multi-Protocolo

```csharp
var values = await runtime.ReadManyAsync(new SignalRef[]
{
    new("Line1Plc", "Motor_01.Speed"),
    new("OmronNj", "D100"),
    new("MitsubishiFx", "D100"),
    new("PanasonicFpx", "DT100"),
    new("SiemensS7", "DB10.DBW0"),
    new("Beckhoff", "MAIN.Speed"),
    new("EnergyMeter", "HoldingRegister:40001"),
    new("OpcServer", "ns=2;s=Line1.Tank.Level"),
    new("MqttBroker", "plant/line1/oee/state")
}, cancellationToken);
```

### Escritura

```csharp
await plcRuntime.WriteAsync(
    new PlcTagRef("Line1", "Motor_01.Command"),
    true,
    cancellationToken);
```

### UDT Dinamico

```csharp
var motor = await logixClient.ReadUdtAsync("Motor_01", cancellationToken);

var speed = motor["Speed"];
var running = motor["Running"];
```

### UDT Tipado

```csharp
public sealed class MotorStatus
{
    public float Speed { get; set; }
    public bool Running { get; set; }
    public int FaultCode { get; set; }
}

var motor = await logixClient.ReadTagAsync<MotorStatus>(
    "Motor_01",
    cancellationToken);
```

### CIP Generico

```csharp
var identity = await cipClient.GetIdentityAsync(cancellationToken);

var data = await cipClient.GetAttributeSingleAsync(
    classId: 0x01,
    instanceId: 1,
    attributeId: 1,
    cancellationToken);
```

### Implicit Messaging

```csharp
var connection = await scanner.OpenIoConnectionAsync(new IoConnectionOptions
{
    Address = "192.168.0.50",
    InputAssembly = 101,
    OutputAssembly = 100,
    ConfigurationAssembly = 102,
    InputSize = 32,
    OutputSize = 32,
    RequestedPacketInterval = TimeSpan.FromMilliseconds(20)
}, cancellationToken);

connection.InputReceived += (_, packet) =>
{
    var input = packet.Data.Span;
};
```

## Performance

Requisitos de performance:

- No abrir/cerrar conexion por request HTTP.
- Reusar sesiones TCP.
- Mantener pools por dispositivo.
- Agrupar lecturas compatibles.
- Cachear metadata de tags.
- Cachear layouts de UDT.
- Evitar reflection repetida.
- Usar mappers compilados cuando sea posible.
- Minimizar allocations con `ArrayPool<byte>`.
- Usar `Span<byte>` y `Memory<byte>` para codificacion.
- Soportar backpressure cuando un dispositivo responde lento.
- Evitar que un dispositivo lento degrade todo el runtime.
- Permitir limites por dispositivo.
- Exponer metricas de cola, latencia y errores.

## Uso Recomendado En Blazor/MVC

Para pantallas o dashboards no conviene hacer lecturas directas desde cada componente UI. El flujo recomendado es:

```text
PLC -> PollingEngine -> SnapshotStore -> API/SignalR/Blazor
```

Para la aplicacion objetivo, el flujo general sera:

```text
PLCs / Robots / Dispositivos EIP -> PlcNet.Runtime -> API Industrial -> SCADA/MES/HMI/Blazor
```

Funcionalidades necesarias:

- Polling de grupos de tags.
- Snapshots de ultimo valor conocido.
- Notificaciones de cambio.
- Integracion con SignalR.
- Endpoints API para lectura bajo demanda.
- Health checks por dispositivo.

## Configuracion Esperada

```json
{
  "PlcNet": {
    "Devices": {
      "Line1": {
        "Driver": "Logix",
        "Address": "192.168.0.10",
        "Path": "1,0",
        "MaxConnections": 4,
        "MaxConcurrentRequests": 16,
        "RequestTimeoutMs": 2000,
        "EnableBatching": true,
        "EnableMetadataCache": true
      },
      "EnergyMeter": {
        "Driver": "ModbusTcp",
        "Address": "192.168.0.30",
        "Port": 502,
        "UnitId": 1,
        "RequestTimeoutMs": 1000
      },
      "MitsubishiFx": {
        "Driver": "MitsubishiMelsec",
        "Protocol": "SLMP",
        "Address": "192.168.0.60",
        "Port": 5000,
        "RequestTimeoutMs": 1000
      },
      "OmronNj": {
        "Driver": "Omron",
        "Protocol": "FINS",
        "Address": "192.168.0.62",
        "Port": 9600,
        "Network": 0,
        "Node": 62,
        "Unit": 0,
        "RequestTimeoutMs": 1000
      },
      "PanasonicFpx": {
        "Driver": "PanasonicFp",
        "Protocol": "MEWTOCOL",
        "Address": "192.168.0.61",
        "Port": 9094,
        "Station": 1,
        "RequestTimeoutMs": 1000
      },
      "SiemensS7": {
        "Driver": "SiemensS7",
        "Address": "192.168.0.63",
        "Port": 102,
        "Rack": 0,
        "Slot": 1,
        "RequestTimeoutMs": 1000
      },
      "Beckhoff": {
        "Driver": "BeckhoffAds",
        "Address": "192.168.0.64",
        "AmsNetId": "192.168.0.64.1.1",
        "AmsPort": 851,
        "RequestTimeoutMs": 1000
      },
      "OpcServer": {
        "Driver": "OpcUa",
        "EndpointUrl": "opc.tcp://192.168.0.40:4840",
        "SecurityMode": "None"
      },
      "MqttBroker": {
        "Driver": "Mqtt",
        "Host": "192.168.0.50",
        "Port": 1883
      }
    }
  }
}
```

## Roadmap Inicial

### Fase 1 - Nucleo Explicito

- Crear solucion .NET.
- Crear proyectos base.
- Implementar transporte EtherNet/IP TCP.
- Implementar registro/cierre de sesion.
- Implementar CIP generico minimo.
- Implementar `GetIdentity`.

### Fase 2 - Discovery Inicial

- Crear `PlcNet.Discovery`.
- Implementar probes read-only.
- Detectar EtherNet/IP por puerto 44818.
- Detectar Modbus TCP por puerto 502.
- Detectar OPC UA por endpoint.
- Devolver resultado con evidencia y confianza.
- Integrar discovery con configuracion inicial de dispositivos.

### Fase 3 - Logix Tags Basicos

- Leer tags simples.
- Escribir tags simples.
- Soportar tipos primitivos.
- Soportar strings.
- Soportar arrays.
- Implementar timeouts y cancelacion.

### Fase 4 - Performance Runtime

- Device registry.
- Connection pools.
- Request scheduler.
- Read batching.
- Metadata cache.
- Health checks.
- Metricas.

### Fase 5 - UDTs

- Leer metadata de tags.
- Leer metadata de UDTs.
- Construir layouts.
- Decodificar UDT dinamico.
- Decodificar UDT tipado.
- Escribir UDT.
- Cachear mappers.

### Fase 6 - Dispositivos Genericos

- Mejorar API CIP class/instance/attribute.
- Parser EDS inicial.
- Pruebas con dispositivos IFM/Balluff u otros.

### Fase 7 - Runtime Multi-Protocolo

- Definir contratos comunes de drivers.
- Definir `SignalRef` comun.
- Definir modelo comun de snapshot.
- Permitir registrar dispositivos de distintos protocolos.
- Permitir lecturas cruzadas multi-protocolo.
- Normalizar errores y health checks.

### Fase 8 - Modbus TCP

- Implementar cliente Modbus TCP.
- Leer coils e inputs.
- Leer holding/input registers.
- Escribir coils/registers.
- Agrupar rangos contiguos.
- Mapear registros a signals.

### Fase 9 - Mitsubishi MELSEC FX

- Implementar cliente SLMP/MC Protocol basico.
- Leer dispositivos `D`, `M`, `X`, `Y`.
- Escribir dispositivos permitidos.
- Agrupar lecturas contiguas.
- Configurar perfiles FX5 y FX3 con Ethernet.

### Fase 10 - Omron

- Implementar cliente FINS Ethernet basico.
- Leer areas de memoria.
- Escribir areas permitidas.
- Agrupar lecturas contiguas.
- Configurar network/node/unit.
- Evaluar integracion Omron por EtherNet/IP/CIP y OPC UA.

### Fase 11 - Panasonic FP

- Implementar cliente MEWTOCOL basico.
- Leer contactos/registros.
- Escribir contactos/registros permitidos.
- Agrupar lecturas contiguas.
- Configurar perfiles FP-X/FP-XH.

### Fase 12 - Siemens S7

- Implementar cliente S7 ISO-on-TCP basico.
- Leer DBs.
- Escribir DBs permitidos.
- Agrupar lecturas.
- Configurar rack/slot.
- Documentar restricciones S7-1200/S7-1500.

### Fase 13 - Beckhoff ADS

- Implementar cliente ADS/TCP basico.
- Leer variables por nombre.
- Escribir variables permitidas.
- Cachear handles.
- Evaluar suscripciones ADS.

### Fase 14 - OPC UA

- Implementar cliente OPC UA.
- Lectura/escritura de nodos.
- Browsing.
- Suscripciones.
- Snapshot store.

### Fase 15 - MQTT

- Implementar cliente MQTT.
- Subscribe/publish.
- Mapeo topic/signal.
- Buffering ante desconexion.
- Integracion con snapshots/eventos.

### Fase 16 - Robotica Yaskawa/Motoman

- Definir perfil `YaskawaMotomanDevice`.
- Definir modelo de senales estandar robot/API/PLC/HMI.
- Definir handshake base PLC-robot.
- Soportar assemblies de entrada/salida para robot.
- Agregar snapshots de estado de robot.
- Agregar eventos de robot.
- Documentar configuracion tipica YRC1000/YRC1000micro.

### Fase 17 - Implicit Messaging

- Scanner I/O basico.
- Assemblies input/output/config.
- UDP I/O.
- Watchdog.
- Eventos de datos.
- Integracion con runtime.

## Decisiones Pendientes

- Nombre final de la libreria.
- Version minima de .NET.
- Licencia del proyecto.
- Si se publicara como NuGet publico o privado.
- Nivel inicial de soporte para Micro800.
- Prioridad entre UDTs y implicit messaging.
- Prioridad de soporte para Yaskawa/Motoman.
- Prioridad entre Modbus TCP, OPC UA, MQTT y PROFINET.
- Prioridad de soporte para Mitsubishi FX.
- Prioridad de soporte para Omron.
- Prioridad de soporte para Panasonic FP-X/FP-XH.
- Prioridad de soporte para Siemens S7.
- Prioridad de soporte para Beckhoff ADS.
- Alcance realista de PROFINET en .NET puro.
- Nivel de agresividad permitido para discovery en redes industriales.
- Lista de puertos por defecto para autodeteccion.
- Si discovery debe ejecutarse manualmente o como tarea programada.
- Estrategia de pruebas con hardware real.
- Formato de configuracion de grupos de polling.
- Modelo exacto de snapshots y eventos.
- Si se generaran clases C# desde UDTs o solo mappers runtime.
- Definicion del handshake estandar PLC-robot.
- Definicion de responsabilidades entre API, ladder PLC y HMI.

## MVP v0.1

El MVP debe ser suficientemente pequeno para construirlo rapido, pero suficientemente representativo para validar la arquitectura real de la plataforma.

Objetivo del MVP:

- Probar la arquitectura por capas.
- Validar comunicacion Ethernet real.
- Tener una API usable desde ASP.NET Core.
- Leer datos Allen-Bradley Logix.
- Preparar el camino para UDTs sin implementar todo el ecosistema.
- Tener base de runtime multi-dispositivo.
- Evitar sobrecargar la primera version con todos los protocolos.

### Incluido En MVP v0.1

#### Proyectos Iniciales

- `PlcNet.Core`
- `PlcNet.Model`
- `PlcNet.Transport`
- `PlcNet.Protocols`
- `PlcNet.EtherNetIp`
- `PlcNet.Cip`
- `PlcNet.Logix`
- `PlcNet.Runtime`
- `PlcNet.AspNetCore`
- `PlcNet.Tests`

#### Funcionalidades Core

- Tipos base de errores.
- `SignalRef`.
- `SignalValue`.
- `SignalQuality`.
- `DeviceIdentity`.
- `DeviceCapabilities`.
- `IDeviceDriver`.
- `IDeviceConnection`.
- `IPlcRuntime`.
- `CancellationToken` en operaciones publicas.
- Timeouts configurables.

#### Transporte

- Cliente TCP asincronico.
- Conexion por IP/puerto.
- Envio/recepcion de bytes.
- Timeout de conexion.
- Timeout de operacion.
- Cierre ordenado.

#### EtherNet/IP + CIP

- `RegisterSession`.
- `UnregisterSession`.
- Encapsulacion basica.
- `ListIdentity` o lectura equivalente de identidad.
- Resultado de identidad con vendor, product, revision y nombre.
- Manejo basico de errores.

#### Discovery Basico

- Detectar EtherNet/IP en puerto 44818.
- Detectar si responde identidad.
- Devolver `DeviceDetectionResult`.
- No escribir nada durante discovery.

#### Logix Basico

- Conectar a ControlLogix/CompactLogix por IP y path.
- Leer tags simples.
- Escribir tags simples controlados.
- Tipos iniciales:
  - `BOOL`.
  - `DINT`.
  - `REAL`.
  - `STRING` si no complica demasiado la primera prueba.
- Lectura multiple simple sin batching avanzado.
- API publica `LogixClient`.

#### Runtime Basico

- Registro de dispositivos por nombre.
- Obtener cliente por nombre.
- Lectura de un signal.
- Escritura de un signal.
- Estado de salud simple por dispositivo.
- Sin pooling avanzado todavia; una conexion persistente por dispositivo alcanza para validar.

#### ASP.NET Core

- `AddPlcNet`.
- Configuracion desde `appsettings.json`.
- Registro de dispositivos Logix.
- Inyeccion de `IPlcRuntime`.
- Health check basico.

### Fuera Del MVP v0.1

- UDT completo.
- Escritura parcial de UDT.
- Multi-service packet.
- Fragmentacion avanzada.
- Pool de multiples conexiones por dispositivo.
- Scheduler avanzado.
- Snapshot store avanzado.
- Polling engine.
- SignalR.
- Modbus TCP.
- OPC UA.
- MQTT.
- PROFINET.
- Mitsubishi.
- Omron.
- Panasonic.
- Siemens.
- Beckhoff.
- Yaskawa/Motoman.
- Implicit messaging.

### Criterios De Exito

- La solucion compila.
- Hay tests unitarios para codificacion binaria y modelos base.
- Se puede registrar un dispositivo Logix por configuracion.
- Se puede hacer discovery de una IP EtherNet/IP.
- Se puede leer un tag `DINT`.
- Se puede escribir un tag `DINT` en modo controlado.
- Se puede consumir desde una API ASP.NET Core de ejemplo.
- El diseno no bloquea agregar Modbus TCP como segundo driver.

### MVP v0.2 Sugerido

- Metadata de tags Logix.
- Lectura de arrays.
- Lectura de strings robusta.
- Primer soporte de UDT dinamico read-only.
- Snapshot store.
- Polling engine simple.
- Modbus TCP como segundo driver.

### MVP v0.3 Sugerido

- UDTs tipados.
- Cache de metadata UDT.
- Batching de lecturas Logix.
- Health checks mas completos.
- Metricas.
- Ejemplo Blazor dashboard.

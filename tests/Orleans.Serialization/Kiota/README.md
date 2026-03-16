# Orleans.Serialization.Kiota.Tests

xUnit v3 coverage for the Orleans Kiota serialization codecs.

## Overview

This project validates the three supported codecs:

- `KiotaJsonCodec`
- `KiotaMessagePackCodec`
- `KiotaMemoryPackCodec`

Each codec is exercised with compression disabled and enabled.

## Coverage

The test suite currently covers:

- Orleans object serializer round-trips for representative Microsoft Graph payloads
- deep-copy behavior for Kiota `IParsable` models
- `KiotaTestModel` round-trip and deep-copy coverage
- `Microsoft.Orleans.Serialization.TestKit` copier smoke tests where it adds value

## Graph sample set

The Graph-based tests use nested, deterministic samples defined in `KiotaCodecTestInfrastructure.cs`, including:

- `User`
- `Message`
- `Event`
- `Group`
- `Contact`
- `Team`

The samples intentionally favor deeper but stable relations such as managers, owners, reply-to recipients, additional locations, contact addresses, and team settings.

## Run tests

From the repository root:

```powershell
dotnet test .\tests\Orleans.Serialization\Kiota\Orleans.Serialization.Kiota.Tests.csproj -c Release
```

## Notable helpers

| Helper | Purpose |
|---|---|
| `KiotaCodecHarnessFactory` | Builds Orleans serializer service providers for each codec and compression combination. |
| `GraphEntitySamples` | Creates representative Graph payloads for round-trip and deep-copy tests. |
| `GraphEntityAssert` | Performs entity-aware equality checks for the supported Graph payload set. |
| `XunitV3OutputAdapter` | Bridges xUnit v3 output to `Microsoft.Orleans.Serialization.TestKit`. |

## Related documentation

- [Repository overview](../../../README.md)
- [SystemTextJson package](../../../src/Orleans.Serialization/Kiota/SystemTextJson/README.md)
- [MessagePack package](../../../src/Orleans.Serialization/Kiota/MessagePack/README.md)
- [MemoryPack package](../../../src/Orleans.Serialization/Kiota/MemoryPack/README.md)
- [Kiota benchmarks](../../../benchmarks/Orleans.Serialization/Kiota/README.md)

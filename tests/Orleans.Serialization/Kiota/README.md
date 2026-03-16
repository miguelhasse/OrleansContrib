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
- `KiotaCollectionTestModel` coverage for primitive, object, enum, and empty collections
- `Microsoft.Orleans.Serialization.TestKit` copier smoke tests where it adds value

## Graph sample set

The Graph-based tests use nested, deterministic samples defined in `KiotaCodecTestInfrastructure.cs`, including:

- `User`
- `Message`
- `Event`
- `Group`
- `Contact`
- `DriveItem`
- `Team`

The samples intentionally favor deeper but stable relations such as managers, owners, reply-to recipients, additional locations, nested drive items, contact addresses, and team settings.

Collection-focused tests concentrate on entity shapes that naturally stress list/object collections, such as:

- `User`
- `Message`
- `Group`
- `Contact`

## Run tests

From the repository root:

```powershell
dotnet test .\tests\Orleans.Serialization\Kiota\Orleans.Serialization.Kiota.Tests.csproj -c Release
```

## Related documentation

- [Repository overview](../../../README.md)
- [SystemTextJson package](../../../src/Orleans.Serialization/Kiota/SystemTextJson/README.md)
- [MessagePack package](../../../src/Orleans.Serialization/Kiota/MessagePack/README.md)
- [MemoryPack package](../../../src/Orleans.Serialization/Kiota/MemoryPack/README.md)
- [Kiota benchmarks](../../../benchmarks/Orleans.Serialization/Kiota/README.md)

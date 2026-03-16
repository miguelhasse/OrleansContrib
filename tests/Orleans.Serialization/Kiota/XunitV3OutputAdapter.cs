namespace Kiota.Serialization.MemoryPack.Orleans.Tests;

/// <summary>
/// Adapts xunit.v3's <see cref="Xunit.ITestOutputHelper"/> to the
/// <see cref="Xunit.Abstractions.ITestOutputHelper"/> interface expected by
/// <c>Microsoft.Orleans.Serialization.TestKit</c> (built against xunit v2).
/// </summary>
internal sealed class XunitV3OutputAdapter : Xunit.Abstractions.ITestOutputHelper
{
    private readonly Xunit.ITestOutputHelper _inner;

    public XunitV3OutputAdapter(Xunit.ITestOutputHelper inner) => _inner = inner;

    public void WriteLine(string message) => _inner.WriteLine(message);

    public void WriteLine(string format, params object[] args) => _inner.WriteLine(format, args);
}

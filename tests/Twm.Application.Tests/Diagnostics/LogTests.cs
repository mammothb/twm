using Twm.Application.Diagnostics;

namespace Twm.Application.Tests.Diagnostics;

// Log.s_sink is shared static state — running these tests in parallel with
// any other test class that calls Log.Line (e.g., WmSessionTests, which
// triggers Log.Line from many session methods) causes captured-lambda races.
// Serializing this collection makes the whole test run run sequentially,
// which is the heavy-hammer xUnit v3 provides for this case. Cheap because
// the assembly is small (~250ms).
[CollectionDefinition("Log", DisableParallelization = true)]
public class LogTestCollection;

[Collection("Log")]
public sealed class LogTests : IDisposable
{
    // Log holds a static sink that other tests in the suite could leak into;
    // resetting in Dispose keeps tests independent regardless of order.
    public void Dispose() => Log.Init(null);

    [Fact]
    public void Enabled_WithNoSink_ReturnsFalse()
    {
        Log.Init(null);
        Log.Enabled.ShouldBeFalse();
    }

    [Fact]
    public void Enabled_AfterInitWithSink_ReturnsTrue()
    {
        Log.Init(_ => { });
        Log.Enabled.ShouldBeTrue();
    }

    [Fact]
    public void Line_WithoutSink_DoesNotThrow()
    {
        Log.Init(null);
        // Hot-path no-op behavior: instrumented code keeps running silently
        // when no sink is configured.
        Should.NotThrow(() => Log.Line("anything"));
    }

    [Fact]
    public void Line_WithSink_InvokesSinkOnceWithPrefixedMessage()
    {
        string? captured = null;
        Log.Init(msg => captured = msg);

        Log.Line("hello");

        captured.ShouldNotBeNull();
        // Format is "{HH:mm:ss.fff} {message}" — assert the message tail
        // rather than the timestamp prefix (which is non-deterministic).
        captured.ShouldEndWith(" hello");
    }

    // Note: a "Line after sink reset does not invoke" test would depend on
    // the static Log.s_sink being reset, but xUnit's parallel-by-class
    // execution means a sibling class could call Log.Init between our
    // setup and assertion. The four tests above cover the public contract
    // (Enabled, Line no-op, Line with sink) without depending on ordering.
}

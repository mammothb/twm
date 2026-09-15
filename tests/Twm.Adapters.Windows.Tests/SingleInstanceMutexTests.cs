using System.Threading;

namespace Twm.Adapters.Windows.Tests;

public sealed class SingleInstanceMutexTests
{
    [Fact]
    public void Mutex_TwoInstances_SecondCannotAcquire()
    {
        // Unique per-test name so other tests / real Twm instances can't collide.
        string name = "Twm.MutexTest." + Guid.NewGuid().ToString("N");

        using var first = new Mutex(initiallyOwned: true, name, out bool firstOnly);
        firstOnly.ShouldBeTrue();

        using var second = new Mutex(initiallyOwned: true, name, out bool secondOnly);
        secondOnly.ShouldBeFalse();
    }

    [Fact]
    public void Mutex_AfterFirstReleases_SecondCanAcquire()
    {
        string name = "Twm.MutexTest." + Guid.NewGuid().ToString("N");

        var first = new Mutex(initiallyOwned: true, name, out bool firstOnly);
        firstOnly.ShouldBeTrue();
        first.ReleaseMutex();
        first.Dispose();

        using var second = new Mutex(initiallyOwned: true, name, out bool secondOnly);
        secondOnly.ShouldBeTrue();
    }
}

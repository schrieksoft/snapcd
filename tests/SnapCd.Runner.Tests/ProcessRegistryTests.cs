using SnapCd.Contracts;
using SnapCd.Runner.Services;

namespace SnapCd.Runner.Tests;

public class ProcessRegistryTests
{
    [Fact]
    public void Register_ShouldAddProcessToRegistry()
    {
        // Arrange
        var registry = new ProcessRegistry();
        var requestId = Guid.NewGuid();
        var cts = new CancellationTokenSource();

        // Act
        registry.Register(requestId, cts, CancellationType.ImmediateKill);

        // Assert
        Assert.True(registry.IsActive(requestId, CancellationType.ImmediateKill));
    }

    [Fact]
    public void Register_ShouldSupportMultipleCancellationTypes()
    {
        // Arrange
        var registry = new ProcessRegistry();
        var requestId = Guid.NewGuid();
        var cts1 = new CancellationTokenSource();
        var cts2 = new CancellationTokenSource();

        // Act
        registry.Register(requestId, cts1, CancellationType.ImmediateKill);
        registry.Register(requestId, cts2, CancellationType.ImmediateGraceful);

        // Assert
        Assert.True(registry.IsActive(requestId, CancellationType.ImmediateKill));
        Assert.True(registry.IsActive(requestId, CancellationType.ImmediateGraceful));
    }

    [Fact]
    public void TryCancel_ShouldCancelAndRemoveProcess()
    {
        // Arrange
        var registry = new ProcessRegistry();
        var requestId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        registry.Register(requestId, cts, CancellationType.ImmediateKill);

        // Act
        var result = registry.TryCancel(requestId, CancellationType.ImmediateKill);

        // Assert
        Assert.True(result);
        Assert.True(cts.Token.IsCancellationRequested);
        Assert.False(registry.IsActive(requestId, CancellationType.ImmediateKill));
    }

    [Fact]
    public void TryCancel_ShouldReturnFalse_WhenProcessNotFound()
    {
        // Arrange
        var registry = new ProcessRegistry();
        var requestId = Guid.NewGuid();

        // Act
        var result = registry.TryCancel(requestId, CancellationType.ImmediateKill);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryCancel_ShouldOnlyCancelSpecificCancellationType()
    {
        // Arrange
        var registry = new ProcessRegistry();
        var requestId = Guid.NewGuid();
        var cts1 = new CancellationTokenSource();
        var cts2 = new CancellationTokenSource();

        registry.Register(requestId, cts1, CancellationType.ImmediateKill);
        registry.Register(requestId, cts2, CancellationType.ImmediateGraceful);

        // Act
        var result = registry.TryCancel(requestId, CancellationType.ImmediateKill);

        // Assert
        Assert.True(result);
        Assert.True(cts1.Token.IsCancellationRequested);
        Assert.False(cts2.Token.IsCancellationRequested);
        Assert.False(registry.IsActive(requestId, CancellationType.ImmediateKill));
        Assert.True(registry.IsActive(requestId, CancellationType.ImmediateGraceful));
    }

    [Fact]
    public void Remove_ShouldRemoveProcessFromRegistry()
    {
        // Arrange
        var registry = new ProcessRegistry();
        var requestId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        registry.Register(requestId, cts, CancellationType.ImmediateKill);

        // Act
        registry.Remove(requestId, CancellationType.ImmediateKill);

        // Assert
        Assert.False(registry.IsActive(requestId, CancellationType.ImmediateKill));
    }

    [Fact]
    public void Remove_ShouldNotThrow_WhenProcessNotFound()
    {
        // Arrange
        var registry = new ProcessRegistry();
        var requestId = Guid.NewGuid();

        // Act & Assert - should not throw
        registry.Remove(requestId, CancellationType.ImmediateKill);
    }

    [Fact]
    public void IsActive_ShouldReturnFalse_WhenProcessNotRegistered()
    {
        // Arrange
        var registry = new ProcessRegistry();
        var requestId = Guid.NewGuid();

        // Act
        var result = registry.IsActive(requestId, CancellationType.ImmediateKill);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsActive_ShouldReturnTrue_WhenProcessIsRegistered()
    {
        // Arrange
        var registry = new ProcessRegistry();
        var requestId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        registry.Register(requestId, cts, CancellationType.ImmediateKill);

        // Act
        var result = registry.IsActive(requestId, CancellationType.ImmediateKill);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Register_ShouldOverwritePreviousRegistration_ForSameRequestIdAndType()
    {
        // Arrange
        var registry = new ProcessRegistry();
        var requestId = Guid.NewGuid();
        var cts1 = new CancellationTokenSource();
        var cts2 = new CancellationTokenSource();

        // Act
        registry.Register(requestId, cts1, CancellationType.ImmediateKill);
        registry.Register(requestId, cts2, CancellationType.ImmediateKill);

        // Try to cancel - should cancel cts2 (the latest one)
        registry.TryCancel(requestId, CancellationType.ImmediateKill);

        // Assert
        Assert.False(cts1.Token.IsCancellationRequested);
        Assert.True(cts2.Token.IsCancellationRequested);
    }

    [Fact]
    public async Task Registry_ShouldBeThreadSafe()
    {
        // Arrange
        var registry = new ProcessRegistry();
        var tasks = new List<Task>();
        var requestIds = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();

        // Act - Register processes concurrently
        foreach (var requestId in requestIds)
        {
            tasks.Add(Task.Run(() =>
            {
                var cts = new CancellationTokenSource();
                registry.Register(requestId, cts, CancellationType.ImmediateKill);
            }));
        }

        await Task.WhenAll(tasks);
        tasks.Clear();

        // Cancel processes concurrently
        foreach (var requestId in requestIds)
        {
            tasks.Add(Task.Run(() =>
            {
                registry.TryCancel(requestId, CancellationType.ImmediateKill);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - all processes should be removed
        foreach (var requestId in requestIds)
        {
            Assert.False(registry.IsActive(requestId, CancellationType.ImmediateKill));
        }
    }

    [Fact]
    public void Registry_ShouldHandleMultipleRequestIds()
    {
        // Arrange
        var registry = new ProcessRegistry();
        var request1 = Guid.NewGuid();
        var request2 = Guid.NewGuid();
        var request3 = Guid.NewGuid();

        var cts1 = new CancellationTokenSource();
        var cts2 = new CancellationTokenSource();
        var cts3 = new CancellationTokenSource();

        // Act
        registry.Register(request1, cts1, CancellationType.ImmediateKill);
        registry.Register(request2, cts2, CancellationType.ImmediateKill);
        registry.Register(request3, cts3, CancellationType.ImmediateGraceful);

        // Assert
        Assert.True(registry.IsActive(request1, CancellationType.ImmediateKill));
        Assert.True(registry.IsActive(request2, CancellationType.ImmediateKill));
        Assert.True(registry.IsActive(request3, CancellationType.ImmediateGraceful));

        // Cancel one
        registry.TryCancel(request2, CancellationType.ImmediateKill);

        // Assert others are still active
        Assert.True(registry.IsActive(request1, CancellationType.ImmediateKill));
        Assert.False(registry.IsActive(request2, CancellationType.ImmediateKill));
        Assert.True(registry.IsActive(request3, CancellationType.ImmediateGraceful));
    }
}

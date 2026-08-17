using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Services.StoreHub;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Services;

public class SyncWorkerTests
{
    private static OutboxEvent CreateTestEvent(string type = "InvoiceCompleted", string status = "Pending")
    {
        return new OutboxEvent
        {
            Id = Guid.NewGuid(),
            StoreId = "STORE-001",
            Type = type,
            PayloadJson = "{}",
            CreatedUtc = DateTime.UtcNow,
            Status = status,
            Attempts = 0
        };
    }

    private static (SyncWorker Worker, Mock<IOutboxRepository> OutboxRepo, Mock<ICloudSyncClient> SyncClient) CreateSut(
        SyncWorkerOptions? options = null)
    {
        var outboxRepo = new Mock<IOutboxRepository>();
        var syncClient = new Mock<ICloudSyncClient>();
        var logger = Moq.Mock.Of<ILogger<SyncWorker>>();

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IOutboxRepository))).Returns(outboxRepo.Object);
        serviceProvider.Setup(x => x.GetService(typeof(ICloudSyncClient))).Returns(syncClient.Object);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var workerOptions = Options.Create(options ?? new SyncWorkerOptions
        {
            BatchSize = 10,
            PollingIntervalSeconds = 1,
            MaxRetries = 3,
            BaseRetryDelaySeconds = 30,
            Enabled = true
        });

        var worker = new SyncWorker(scopeFactory.Object, workerOptions, logger);

        return (worker, outboxRepo, syncClient);
    }

    [Fact]
    public async Task SyncWorker_ShouldProcessPendingEvents_WhenEventsExist()
    {
        // Arrange
        var (worker, outboxRepo, syncClient) = CreateSut();
        var testEvent = CreateTestEvent();
        var markAsSentCalled = new TaskCompletionSource<bool>();

        outboxRepo.SetupSequence(x => x.GetPendingEventsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OutboxEvent> { testEvent })
            .ReturnsAsync(new List<OutboxEvent>()); // Empty on second call

        syncClient.Setup(x => x.SendEventAsync(testEvent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        outboxRepo.Setup(x => x.MarkAsSentAsync(testEvent.Id, It.IsAny<CancellationToken>()))
            .Callback(() => markAsSentCalled.TrySetResult(true))
            .Returns(Task.CompletedTask);

        using var cts = new CancellationTokenSource();

        // Act - Start the worker and wait for processing
        var workerTask = worker.StartAsync(cts.Token);

        // Wait for MarkAsSentAsync to be called (with timeout)
        var completedTask = await Task.WhenAny(markAsSentCalled.Task, Task.Delay(5000));

        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        // Assert
        completedTask.Should().Be(markAsSentCalled.Task, "MarkAsSentAsync should have been called within timeout");
        syncClient.Verify(x => x.SendEventAsync(testEvent, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        outboxRepo.Verify(x => x.MarkAsSentAsync(testEvent.Id, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task SyncWorker_ShouldMarkAsFailed_WhenSyncFails()
    {
        // Arrange
        var (worker, outboxRepo, syncClient) = CreateSut();
        var testEvent = CreateTestEvent();
        var markAsFailedCalled = new TaskCompletionSource<bool>();

        outboxRepo.SetupSequence(x => x.GetPendingEventsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OutboxEvent> { testEvent })
            .ReturnsAsync(new List<OutboxEvent>());

        syncClient.Setup(x => x.SendEventAsync(testEvent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Simulate failure

        outboxRepo.Setup(x => x.MarkAsFailedAsync(testEvent.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback(() => markAsFailedCalled.TrySetResult(true))
            .Returns(Task.CompletedTask);

        using var cts = new CancellationTokenSource();

        // Act
        var workerTask = worker.StartAsync(cts.Token);

        // Wait for MarkAsFailedAsync to be called (with timeout)
        var completedTask = await Task.WhenAny(markAsFailedCalled.Task, Task.Delay(5000));

        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        // Assert
        completedTask.Should().Be(markAsFailedCalled.Task, "MarkAsFailedAsync should have been called within timeout");
        outboxRepo.Verify(x => x.MarkAsFailedAsync(
            testEvent.Id,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task SyncWorker_ShouldNotProcess_WhenDisabled()
    {
        // Arrange
        var (worker, outboxRepo, syncClient) = CreateSut(new SyncWorkerOptions { Enabled = false });

        using var cts = new CancellationTokenSource();

        // Act
        var workerTask = worker.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        // Assert
        outboxRepo.Verify(x => x.GetPendingEventsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncWorker_ShouldHandleException_WithoutCrashing()
    {
        // Waits for the condition rather than for a fixed 100 ms. This test used to sleep 100 ms and
        // then assert the worker had polled at least once, which made it FLAKY: under the CPU load of
        // a full-solution run the background worker is not always scheduled inside that window, so
        // the count was 0 and the run failed with nothing wrong. It surfaced twice on 2026-08-17 and
        // passed every time in isolation.
        //
        // Polling a SECOND time is also the stronger assertion, and the one that matches the test's
        // name: surviving the exception means it kept going, not merely that it started.
        // SyncWorker_ShouldMarkAsFailed_WhenSyncFails in this file already used this shape.
        var (worker, outboxRepo, _) = CreateSut();

        var polledAfterThrowing = new TaskCompletionSource();
        var calls = 0;

        outboxRepo.Setup(x => x.GetPendingEventsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                if (Interlocked.Increment(ref calls) == 1)
                {
                    throw new InvalidOperationException("Database error");
                }

                polledAfterThrowing.TrySetResult();
                return Task.FromResult<IReadOnlyList<OutboxEvent>>([]);
            });

        using var cts = new CancellationTokenSource();

        // Act - should not throw
        await worker.StartAsync(cts.Token);

        var finished = await Task.WhenAny(polledAfterThrowing.Task, Task.Delay(TimeSpan.FromSeconds(10)));

        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        // Assert
        finished.Should().Be(polledAfterThrowing.Task,
            "the worker must keep polling after GetPendingEventsAsync throws, not die on it");
        outboxRepo.Verify(
            x => x.GetPendingEventsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(2));
    }
}

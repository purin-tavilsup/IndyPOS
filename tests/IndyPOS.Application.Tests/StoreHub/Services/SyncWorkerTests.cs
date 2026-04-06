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

        outboxRepo.SetupSequence(x => x.GetPendingEventsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OutboxEvent> { testEvent })
            .ReturnsAsync(new List<OutboxEvent>()); // Empty on second call

        syncClient.Setup(x => x.SendEventAsync(testEvent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        using var cts = new CancellationTokenSource();

        // Act - Start the worker and cancel after a short delay
        var workerTask = worker.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        // Assert
        syncClient.Verify(x => x.SendEventAsync(testEvent, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        outboxRepo.Verify(x => x.MarkAsSentAsync(testEvent.Id, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task SyncWorker_ShouldMarkAsFailed_WhenSyncFails()
    {
        // Arrange
        var (worker, outboxRepo, syncClient) = CreateSut();
        var testEvent = CreateTestEvent();

        outboxRepo.SetupSequence(x => x.GetPendingEventsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OutboxEvent> { testEvent })
            .ReturnsAsync(new List<OutboxEvent>());

        syncClient.Setup(x => x.SendEventAsync(testEvent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Simulate failure

        using var cts = new CancellationTokenSource();

        // Act
        var workerTask = worker.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        // Assert
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
        // Arrange
        var (worker, outboxRepo, syncClient) = CreateSut();

        outboxRepo.SetupSequence(x => x.GetPendingEventsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"))
            .ReturnsAsync(new List<OutboxEvent>());

        using var cts = new CancellationTokenSource();

        // Act - Should not throw
        var workerTask = worker.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        // Assert - Worker should have survived the exception
        outboxRepo.Verify(x => x.GetPendingEventsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.AtLeast(1));
    }
}

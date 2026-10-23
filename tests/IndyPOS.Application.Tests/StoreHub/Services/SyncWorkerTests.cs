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
            BaseRetryDelaySeconds = 30,
            Enabled = true
        });

        var worker = new SyncWorker(scopeFactory.Object, workerOptions, logger);

        return (worker, outboxRepo, syncClient);
    }

    private static SyncWorker CreateWorker(IOutboxRepository outbox, ICloudSyncClient client, SyncWorkerOptions options)
    {
        var services = new ServiceCollection();
        services.AddSingleton(outbox);
        services.AddSingleton(client);

        return new SyncWorker(
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            Moq.Mock.Of<ILogger<SyncWorker>>());
    }

    /// <summary>
    /// The stall itself. An event that reached the old retry limit was logged and left untouched: still
    /// Pending, already due, oldest first. Ten of them filled every batch of ten, so nothing newer was
    /// ever sent again. Refused events must now step aside as Failed.
    /// </summary>
    [Fact]
    public async Task SyncWorker_WithTenRefusedEventsAheadOfANewOne_StillSendsTheNewOne()
    {
        var outbox = new InMemoryOutboxRepository();
        var refusedIds = new List<Guid>();
        for (var i = 0; i < 10; i++)
        {
            var refused = CreateTestEvent();
            refused.CreatedUtc = DateTime.UtcNow.AddHours(-1).AddSeconds(i);
            refused.Attempts = 4;
            outbox.Add(refused);
            refusedIds.Add(refused.Id);
        }
        var newer = CreateTestEvent();
        outbox.Add(newer);

        var client = new Mock<ICloudSyncClient>();
        client.Setup(c => c.SendEventAsync(It.IsAny<OutboxEvent>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((OutboxEvent e, CancellationToken _) =>
                  e.Id == newer.Id ? SyncSendResult.Sent : SyncSendResult.Refused("403 store-mismatch"));

        var worker = CreateWorker(outbox, client.Object, new SyncWorkerOptions { BatchSize = 10, PollingIntervalSeconds = 1 });

        await worker.StartAsync(CancellationToken.None);
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (outbox.Get(newer.Id).Status != "Sent" && DateTime.UtcNow < deadline)
            await Task.Delay(50);
        await worker.StopAsync(CancellationToken.None);

        outbox.Get(newer.Id).Status.Should()
                                   .Be("Sent");
        refusedIds.Select(id => outbox.Get(id).Status).Should()
                                                      .OnlyContain(status => status == "Failed");
    }

    /// <summary>Runs one outbox event through a started worker and returns it once the client was asked once.</summary>
    private static async Task<OutboxEvent> SendOnceAsync(OutboxEvent outboxEvent, Func<SyncSendResult> answer)
    {
        var outbox = new InMemoryOutboxRepository();
        outbox.Add(outboxEvent);
        var asked = new TaskCompletionSource();

        var client = new Mock<ICloudSyncClient>();
        client.Setup(c => c.SendEventAsync(It.IsAny<OutboxEvent>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(() =>
              {
                  try { return answer(); }
                  finally { asked.TrySetResult(); }
              });

        var worker = CreateWorker(outbox, client.Object, new SyncWorkerOptions { PollingIntervalSeconds = 1 });

        await worker.StartAsync(CancellationToken.None);
        await asked.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await Task.Delay(200); // let the worker write the outcome
        await worker.StopAsync(CancellationToken.None);

        return outbox.Get(outboxEvent.Id);
    }

    // The old limit gave up after five attempts, about eight minutes. A cloud that is down for a day
    // must never turn a good sale into a failed one.
    [Fact]
    public async Task SyncWorker_WhenTheCloudIsUnreachableAfterManyAttempts_KeepsTheEventPending()
    {
        var outboxEvent = CreateTestEvent();
        outboxEvent.Attempts = 50;

        var stored = await SendOnceAsync(outboxEvent, () => SyncSendResult.RetryLater("Cloud unreachable"));

        stored.Status.Should()
                     .Be("Pending");
        stored.NextRetryUtc.Should()
                           .BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task SyncWorker_WhenTheCloudRefusesAnEvent_MarksItFailedWithTheReason()
    {
        var stored = await SendOnceAsync(CreateTestEvent(), () => SyncSendResult.Refused("403 store-mismatch: not this store"));

        stored.Status.Should()
                     .Be("Failed");
        stored.LastError.Should()
                        .Be("403 store-mismatch: not this store");
    }

    [Fact]
    public async Task SyncWorker_WhenSendingThrows_SchedulesARetry()
    {
        var stored = await SendOnceAsync(CreateTestEvent(), () => throw new InvalidOperationException("serializer blew up"));

        stored.Status.Should()
                     .Be("Pending");
        stored.Attempts.Should()
                       .Be(1);
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
            .ReturnsAsync(SyncSendResult.Sent);

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
    public async Task SyncWorker_WhenTheCloudIsDown_SchedulesARetry()
    {
        // Arrange
        var (worker, outboxRepo, syncClient) = CreateSut();
        var testEvent = CreateTestEvent();
        var scheduleRetryCalled = new TaskCompletionSource<bool>();

        outboxRepo.SetupSequence(x => x.GetPendingEventsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OutboxEvent> { testEvent })
            .ReturnsAsync(new List<OutboxEvent>());

        syncClient.Setup(x => x.SendEventAsync(testEvent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SyncSendResult.RetryLater("simulated outage"));

        outboxRepo.Setup(x => x.ScheduleRetryAsync(testEvent.Id, It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback(() => scheduleRetryCalled.TrySetResult(true))
            .Returns(Task.CompletedTask);

        using var cts = new CancellationTokenSource();

        // Act
        var workerTask = worker.StartAsync(cts.Token);

        // Wait for ScheduleRetryAsync to be called (with timeout)
        var completedTask = await Task.WhenAny(scheduleRetryCalled.Task, Task.Delay(5000));

        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        // Assert
        completedTask.Should().Be(scheduleRetryCalled.Task, "ScheduleRetryAsync should have been called within timeout");
        outboxRepo.Verify(x => x.ScheduleRetryAsync(
            testEvent.Id,
            It.IsAny<DateTime>(),
            It.IsAny<string?>(),
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
        // SyncWorker_WhenTheCloudIsDown_SchedulesARetry in this file already used this shape.
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

using System.Collections.Concurrent;
using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using IndyPOS.Application.UseCases.Cloud.Sync.Heartbeat;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Services.StoreHub;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Services;

/// <summary>
/// Every two minutes StoreHub tells the cloud it is up and how its outbox stands, kept light, and carries out the
/// resend requests in the reply. It must never stop, whatever the cloud does.
/// </summary>
public class HeartbeatWorkerTests
{
    private readonly InMemoryOutboxRepository _outbox = new();
    private readonly ConcurrentQueue<HeartbeatRequest> _sent = new();
    private readonly Mock<ICloudHeartbeatClient> _client = new();

    private static OutboxEvent Event(string status, DateTime? createdUtc = null, string? lastError = null) => new()
    {
        Id = Guid.NewGuid(), StoreId = "STORE-001", Type = "InvoiceCompleted", PayloadJson = "{}",
        CreatedUtc = createdUtc ?? DateTime.UtcNow, Status = status, LastError = lastError, LastAttemptUtc = DateTime.UtcNow
    };

    private void CloudAnswers(params Func<HeartbeatResponse?>[] replies)
    {
        var turns = new Queue<Func<HeartbeatResponse?>>(replies);
        _client.Setup(c => c.SendAsync(It.IsAny<HeartbeatRequest>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((HeartbeatRequest request, CancellationToken _) =>
               {
                   _sent.Enqueue(request);
                   return turns.Count > 1 ? turns.Dequeue()() : turns.Peek()();
               });
    }

    private static HeartbeatResponse Nothing() => new([]);

    /// <summary>Runs the worker until it has sent <paramref name="beats"/> heartbeats, and returns them.</summary>
    private async Task<List<HeartbeatRequest>> RunAsync(int beats)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOutboxRepository>(_outbox);
        services.AddSingleton(_client.Object);
        var worker = new HeartbeatWorker(
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new HeartbeatOptions { IntervalSeconds = 1 }),
            Moq.Mock.Of<ILogger<HeartbeatWorker>>());

        await worker.StartAsync(CancellationToken.None);
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (_sent.Count < beats && DateTime.UtcNow < deadline)
            await Task.Delay(50);
        await worker.StopAsync(CancellationToken.None);

        return _sent.Take(beats).ToList();
    }

    [Fact]
    public async Task Heartbeat_ReportsTheOutboxState()
    {
        var oldest = DateTime.UtcNow.AddMinutes(-40);
        _outbox.Add(Event("Pending", oldest));
        _outbox.Add(Event("Pending"));
        var failed = Event("Failed", lastError: "403 store-mismatch");
        _outbox.Add(failed);
        CloudAnswers(Nothing);

        var beat = (await RunAsync(1)).Single();

        beat.PendingCount.Should().Be(2);
        beat.FailedCount.Should().Be(1);
        beat.OldestPendingCreatedAtUtc.Should().Be(oldest);
        beat.FailedEvents.Should().ContainSingle(e => e.EventId == failed.Id && e.Reason == "403 store-mismatch");
    }

    [Fact]
    public async Task Heartbeat_WithNothingFailed_SendsNoFailedList()
    {
        _outbox.Add(Event("Pending"));
        CloudAnswers(Nothing);

        var beat = (await RunAsync(1)).Single();

        beat.FailedEvents.Should().BeNull();
        beat.CompletedResends.Should().BeNull();
    }

    [Fact]
    public async Task Heartbeat_WithALongReason_SendsItCutTo200Characters()
    {
        _outbox.Add(Event("Failed", lastError: new string('x', 900)));
        CloudAnswers(Nothing);

        var beat = (await RunAsync(1)).Single();

        beat.FailedEvents!.Single().Reason!.Length.Should().Be(HeartbeatLimits.MaxReasonLength);
    }

    [Fact]
    public async Task Heartbeat_WithAResendForAll_MovesFailedEventsBackToPending()
    {
        var failed = Event("Failed");
        _outbox.Add(failed);
        CloudAnswers(() => new HeartbeatResponse([new ResendInstruction(Guid.NewGuid(), null)]), Nothing);

        await RunAsync(2);

        _outbox.Get(failed.Id).Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Heartbeat_AfterAResend_ReportsItCompletedOnTheNextBeat()
    {
        var failed = Event("Failed");
        _outbox.Add(failed);
        var requestId = Guid.NewGuid();
        CloudAnswers(() => new HeartbeatResponse([new ResendInstruction(requestId, null)]), Nothing);

        var beats = await RunAsync(2);

        beats[1].CompletedResends.Should().ContainSingle(c => c.RequestId == requestId)
                .Which.MovedEventIds.Should().Equal(failed.Id);
    }

    [Fact]
    public async Task Heartbeat_WithAResendForAnEventThatIsNotFailed_ReportsItIgnored()
    {
        var pending = Event("Pending");
        _outbox.Add(pending);
        var requestId = Guid.NewGuid();
        CloudAnswers(() => new HeartbeatResponse([new ResendInstruction(requestId, pending.Id)]), Nothing);

        var beats = await RunAsync(2);

        beats[1].CompletedResends!.Single().IgnoredEventIds.Should().Equal(pending.Id);
    }

    [Fact]
    public async Task Heartbeat_WhenTheCloudIsDown_KeepsTheCompletedResendForTheNextBeat()
    {
        _outbox.Add(Event("Failed"));
        var requestId = Guid.NewGuid();
        CloudAnswers(() => new HeartbeatResponse([new ResendInstruction(requestId, null)]), () => null, Nothing);

        var beats = await RunAsync(3);

        beats[2].CompletedResends.Should().ContainSingle(c => c.RequestId == requestId);
    }

    [Fact]
    public async Task Heartbeat_WhenSendingThrows_KeepsRunning()
    {
        CloudAnswers(() => throw new InvalidOperationException("boom"), Nothing);

        var beats = await RunAsync(2);

        beats.Should().HaveCount(2);
    }
}

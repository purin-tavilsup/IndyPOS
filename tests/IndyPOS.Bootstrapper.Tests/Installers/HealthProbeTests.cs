using System.Net;
using FluentAssertions;
using IndyPOS.Bootstrapper.Installers;

namespace IndyPOS.Bootstrapper.Tests.Installers;

public class HealthProbeTests
{
    private static readonly Uri ReadyUrl = new("http://localhost:5000/health/ready");
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task IsReadyAsync_WithACancelledToken_ThrowsOperationCanceled()
    {
        var clock = new ManualClock();
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        var act = () => HealthProbe.IsReadyAsync(ReadyUrl, new Responder(HttpStatusCode.OK), clock, clock.Advance, cancelled.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task IsReadyAsync_WhenNeverReady_GivesUpAtTheDeadline()
    {
        var clock = new ManualClock();
        var responder = new Responder(HttpStatusCode.ServiceUnavailable);

        var ready = await HealthProbe.IsReadyAsync(ReadyUrl, responder, clock, clock.Advance, CancellationToken.None);

        ready.Should().BeFalse();
        clock.Elapsed.Should().BeCloseTo(Deadline, TimeSpan.FromSeconds(2));
    }

    // PostgreSQL can take most of a minute after a reboot; failing fast must not end the wait early.
    [Fact]
    public async Task IsReadyAsync_WhenNotReadyAtFirst_KeepsPollingUntilReady()
    {
        var clock = new ManualClock();
        var responder = new Responder(HttpStatusCode.ServiceUnavailable, readyAfter: 20);

        var ready = await HealthProbe.IsReadyAsync(ReadyUrl, responder, clock, clock.Advance, CancellationToken.None);

        ready.Should().BeTrue();
    }

    [Fact]
    public async Task IsReadyAsync_WhenReady_ReturnsOnTheFirstTry()
    {
        var clock = new ManualClock();
        var responder = new Responder(HttpStatusCode.OK);

        var ready = await HealthProbe.IsReadyAsync(ReadyUrl, responder, clock, clock.Advance, CancellationToken.None);

        ready.Should().BeTrue();
        responder.Calls.Should().Be(1);
    }

    private sealed class ManualClock : TimeProvider
    {
        private readonly DateTimeOffset _start = new(2026, 10, 7, 0, 0, 0, TimeSpan.Zero);
        private DateTimeOffset _now = new(2026, 10, 7, 0, 0, 0, TimeSpan.Zero);

        public TimeSpan Elapsed => _now - _start;

        public override DateTimeOffset GetUtcNow() => _now;

        public Task Advance(TimeSpan by, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _now += by;
            return Task.CompletedTask;
        }
    }

    private sealed class Responder(HttpStatusCode status, int readyAfter = int.MaxValue) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            var code = Calls > readyAfter ? HttpStatusCode.OK : status;
            return Task.FromResult(new HttpResponseMessage(code));
        }
    }
}

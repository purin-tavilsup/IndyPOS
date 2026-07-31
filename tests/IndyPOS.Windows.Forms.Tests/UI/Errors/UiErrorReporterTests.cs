using System.Text.RegularExpressions;
using FluentAssertions;
using IndyPOS.Windows.Forms.UI.Errors;
using Serilog;
using Serilog.Events;
using Xunit;

namespace IndyPOS.Windows.Forms.Tests.UI.Errors;

public class UiErrorReporterTests
{
    private readonly CapturingSink _sink = new();

    // Pulls the generated ERR-XXXX code out of a shown message so a test can pin the
    // exact full string around it instead of only asserting Contain(...).
    private static string ExtractReference(string message) =>
        Regex.Match(message, "ERR-[0-9A-F]{4}").Value;

    private UiErrorReporter CreateSut(FakeErrorDialog dialog)
    {
        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(_sink)
            .CreateLogger();

        return new UiErrorReporter(dialog, logger);
    }

    [Fact]
    public void Record_ShouldReturnAReferenceCodeInTheExpectedShape()
    {
        var sut = CreateSut(new FakeErrorDialog());

        var reference = sut.Record(new InvalidOperationException("boom"), "save product", UiErrorSeverity.Recoverable);

        reference.Should().MatchRegex("^ERR-[0-9A-F]{4}$");
    }

    [Fact]
    public void Record_ShouldNotShowAnything()
    {
        // The fatal path logs, flushes, then shows. That ordering is only possible
        // if recording never presents anything by itself.
        var dialog = new FakeErrorDialog();

        CreateSut(dialog).Record(new Exception("boom"), "save product", UiErrorSeverity.Fatal);

        dialog.Shown.Should().BeEmpty();
    }

    [Fact]
    public void ReportToUser_ShouldShowTheSameCodeItLogged()
    {
        // The whole support workflow depends on this: the code on screen must be
        // findable in the log.
        var dialog = new FakeErrorDialog();

        CreateSut(dialog).ReportToUser(new Exception("boom"), "save product", UiErrorSeverity.Recoverable);

        var logged = _sink.PropertyValue("ErrorReference");

        logged.Should().NotBeNull();
        dialog.Shown.Should().ContainSingle();
        dialog.Shown[0].Message.Should().Contain(logged!);
    }

    [Fact]
    public void Record_ForSeparateOccurrences_ShouldReturnDifferentCodes()
    {
        var sut = CreateSut(new FakeErrorDialog());

        var first = sut.Record(new Exception("a"), "op", UiErrorSeverity.Recoverable);
        var second = sut.Record(new Exception("b"), "op", UiErrorSeverity.Recoverable);

        second.Should().NotBe(first);
    }

    [Fact]
    public void ReportToUser_ShouldNotShowTheOperationNameToTheUser()
    {
        // Staff should not read internal method names off a till screen.
        var dialog = new FakeErrorDialog();

        CreateSut(dialog).ReportToUser(new Exception("boom"), "GenerateProductBarcodeAsync", UiErrorSeverity.Recoverable);

        dialog.Shown[0].Message.Should().NotContain("GenerateProductBarcodeAsync");
        dialog.Shown[0].Caption.Should().NotContain("GenerateProductBarcodeAsync");
    }

    [Fact]
    public void Record_ShouldLogTheOperationNameForDiagnosis()
    {
        CreateSut(new FakeErrorDialog())
            .Record(new Exception("boom"), "GenerateProductBarcodeAsync", UiErrorSeverity.Recoverable);

        _sink.PropertyValue("Operation").Should().Be("GenerateProductBarcodeAsync");
    }

    [Theory]
    [InlineData(UiErrorSeverity.Recoverable, LogEventLevel.Error)]
    [InlineData(UiErrorSeverity.Fatal, LogEventLevel.Fatal)]
    [InlineData(UiErrorSeverity.Background, LogEventLevel.Warning)]
    public void Record_ShouldLogAtTheLevelForTheSeverity(UiErrorSeverity severity, LogEventLevel expected)
    {
        CreateSut(new FakeErrorDialog()).Record(new Exception("boom"), "op", severity);

        _sink.Events.Should().ContainSingle();
        _sink.Events[0].Level.Should().Be(expected);
    }

    [Fact]
    public void Record_ShouldAttachTheException()
    {
        var exception = new InvalidOperationException("boom");

        CreateSut(new FakeErrorDialog()).Record(exception, "op", UiErrorSeverity.Recoverable);

        _sink.Events[0].Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public void ReportToUser_WhenFatal_ShouldUseTheFatalCaption()
    {
        var dialog = new FakeErrorDialog();

        CreateSut(dialog).ReportToUser(new Exception("boom"), "op", UiErrorSeverity.Fatal);

        var reference = ExtractReference(dialog.Shown[0].Message);

        dialog.Shown[0].Caption.Should().Be("เกิดข้อผิดพลาดร้ายแรง");
        dialog.Shown[0].Message.Should().Be($"โปรแกรมต้องปิดตัวลง\n\nแจ้งรหัส {reference}");
    }

    [Fact]
    public void ReportToUser_WhenRecoverable_ShouldUseTheRecoverableCaption()
    {
        var dialog = new FakeErrorDialog();

        CreateSut(dialog).ReportToUser(new Exception("boom"), "op", UiErrorSeverity.Recoverable);

        var reference = ExtractReference(dialog.Shown[0].Message);

        dialog.Shown[0].Caption.Should().Be("เกิดข้อผิดพลาด");
        dialog.Shown[0].Message.Should().Be($"เกิดข้อผิดพลาดที่ไม่คาดคิด\n\nกรุณาลองอีกครั้ง หากยังเกิดปัญหา แจ้งรหัส {reference}");
    }

    [Fact]
    public void ReportToUser_WhenBackground_ShouldNotShowAnything()
    {
        // Background means either nobody was waiting on the result, or the caller
        // presents its own dialog. Either way the reporter stays silent.
        var dialog = new FakeErrorDialog();

        CreateSut(dialog).ReportToUser(new Exception("boom"), "op", UiErrorSeverity.Background);

        dialog.Shown.Should().BeEmpty();
        _sink.Events.Should().ContainSingle("the failure is still recorded");
    }

    [Fact]
    public void ReportToUser_WhenTheDialogItselfThrows_ShouldNotPropagate()
    {
        // The reporter is the last line of defence. If it threw, the exception
        // would re-enter the handler that called it.
        var sut = CreateSut(new FakeErrorDialog(throwOnShow: true));

        var act = () => sut.ReportToUser(new Exception("boom"), "op", UiErrorSeverity.Recoverable);

        act.Should().NotThrow();
    }

    [Fact]
    public void ReportToUser_WhenTheDialogThrows_ShouldStillHaveLogged()
    {
        var sut = CreateSut(new FakeErrorDialog(throwOnShow: true));

        sut.ReportToUser(new Exception("boom"), "op", UiErrorSeverity.Recoverable);

        _sink.Events.Should().ContainSingle("the log entry must survive a broken dialog");
    }

    [Fact]
    public void Show_WithABackgroundSeverity_ShouldNotShowAnything()
    {
        var dialog = new FakeErrorDialog();

        CreateSut(dialog).Show("ERR-1234", UiErrorSeverity.Background);

        dialog.Shown.Should().BeEmpty();
    }
}

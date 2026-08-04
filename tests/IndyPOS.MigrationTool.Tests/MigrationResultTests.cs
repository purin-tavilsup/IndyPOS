namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// Pure unit tests. No Docker, no database, no real store data.
/// </summary>
public class MigrationResultTests
{
    [Fact]
    public void Outcome_WithAPhaseFailure_IsAbortedAndIsSuccessIsFalse()
    {
        // THE load-bearing test of defect 12's fix. Program.cs derives its exit code from IsSuccess.
        // Before this fix a phase failure threw, so the operator got a red message and exit 1. Now it
        // returns normally -- so if IsSuccess ignored PhaseFailures, a migration that wrote NOTHING
        // would exit 0 and announce success. That would be worse than the defect being fixed.
        var result = new MigrationResult();
        result.Users.Migrated = 12;

        result.AddPhaseFailure("PayLater", "SQL logic error: no such column: PaidAmount");

        result.Outcome.Should().Be(MigrationOutcome.Aborted);
        result.IsSuccess.Should().BeFalse("nothing was persisted, so the exit code must be non-zero");
        result.PhaseFailures.Should().ContainSingle()
              .Which.Should().BeEquivalentTo(
                  new MigrationPhaseFailure("PayLater", "SQL logic error: no such column: PaidAmount"));
    }

    [Fact]
    public void Outcome_WithOnlyRowFailures_IsCompletedWithErrors()
    {
        // Distinct from Aborted: these rows were refused but the run still committed, so data WAS
        // written. Conflating the two would tell an operator their store is mostly migrated when
        // nothing at all was written.
        var result = new MigrationResult();
        result.Invoices.Migrated = 500;
        result.Payments.Failed = 1;

        result.Outcome.Should().Be(MigrationOutcome.CompletedWithErrors);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Outcome_WithNothingFailed_IsSuccess()
    {
        var result = new MigrationResult();
        result.Users.Migrated = 3;
        result.Products.Skipped = 2;

        result.Outcome.Should().Be(MigrationOutcome.Success);
        result.IsSuccess.Should().BeTrue("skipped rows are not failures");
    }

    [Fact]
    public void AddError_PastTheCap_BoundsTheStringsAndSaysHowManyWereDropped()
    {
        // An upstream phase failure makes every downstream row miss its lookup: up to 325,780
        // near-identical strings on real GeneralHardware data, burying the one line that explains
        // the cause.
        var result = new MigrationResult();

        for (var i = 1; i <= 250; i++)
        {
            result.AddError("Invoices", $"Invoice {i}: product not found");
        }

        result.Errors.Should().HaveCount(101, "100 real errors plus one suppression note");
        result.Errors.Last().Should().Be("Invoices: 150 further error(s) suppressed.");
    }

    [Fact]
    public void AddError_CapsEachPhaseIndependently()
    {
        var result = new MigrationResult();

        for (var i = 1; i <= 150; i++)
        {
            result.AddError("Invoices", $"Invoice {i}: product not found");
            result.AddError("PayLater", $"PayLater {i}: no migrated payment");
        }

        result.Errors.Should().HaveCount(202, "each phase gets its own 100 plus its own note");
        result.Errors.Should().Contain("Invoices: 50 further error(s) suppressed.");
        result.Errors.Should().Contain("PayLater: 50 further error(s) suppressed.");
    }

    [Fact]
    public void AddError_UnderTheCap_KeepsEveryMessageVerbatim()
    {
        var result = new MigrationResult();

        result.AddError("PayLater", "PayLater 999: no migrated payment for legacy PaymentId 999");

        result.Errors.Should().ContainSingle()
              .Which.Should().Be("PayLater 999: no migrated payment for legacy PaymentId 999");
    }
}

namespace IndyPOS.MigrationTool;

/// <summary>
/// A phase that failed as a whole -- its query threw, so none of its rows were examined. Distinct
/// from a per-row failure: a phase failure means NOTHING is persisted for the entire run.
/// </summary>
public sealed record MigrationPhaseFailure(string Phase, string Message);

/// <summary>
/// The three outcomes an operator must be able to tell apart. Collapsing <see cref="Aborted"/> into
/// <see cref="CompletedWithErrors"/> would report a run that wrote nothing as a partial success.
/// </summary>
public enum MigrationOutcome
{
    /// <summary>Everything migrated, and it was committed.</summary>
    Success,

    /// <summary>Committed, but individual rows were refused. Data WAS written.</summary>
    CompletedWithErrors,

    /// <summary>A phase failed wholesale, so NOTHING was written.</summary>
    Aborted
}

/// <summary>
/// A product whose legacy <c>QuantityInStock</c> was negative and was migrated as zero. Unrecorded
/// restocks, not shelf state -- 1,535 products across the two large stores, down to -3,882.
/// </summary>
public sealed record ClampedStock(string Barcode, string ProductName, int LegacyQuantity);

public class MigrationResult
{
    public EntityMigrationResult Users { get; set; } = new();
    public EntityMigrationResult Products { get; set; } = new();
    public EntityMigrationResult Invoices { get; set; } = new();
    public EntityMigrationResult Payments { get; set; } = new();
    public EntityMigrationResult PayLater { get; set; } = new();

    /// <summary>Per phase, because a cascade from one failure must not bury every other phase.</summary>
    private const int MaxErrorsPerPhase = 100;

    private readonly List<string> _errors = [];
    private readonly Dictionary<string, int> _errorCountByPhase = [];
    private readonly Dictionary<string, int> _suppressionNoteIndexByPhase = [];
    private readonly List<ClampedStock> _clampedStocks = [];

    /// <summary>Read-only so every write goes through <see cref="AddError"/> and stays bounded.</summary>
    public IReadOnlyList<string> Errors => _errors;

    /// <summary>
    /// How many errors were actually recorded, across every phase and ignoring the cap.
    /// </summary>
    /// <remarks>
    /// <see cref="Errors"/> is capped, so its Count understates -- and it understates by an amount
    /// the operator could not discover: the note that carries the suppressed count is appended at
    /// index <see cref="MaxErrorsPerPhase"/>, while the console prints only the first ten, so
    /// whenever suppression happens the note announcing it can never be on screen. A store with 400
    /// unresolved categories reported "... and 91 more errors" and the figure 400 appeared nowhere.
    /// </remarks>
    public int TotalErrorsRecorded => _errorCountByPhase.Values.Sum();

    /// <summary>Phases that failed as a whole. Non-empty means nothing was persisted.</summary>
    public List<MigrationPhaseFailure> PhaseFailures { get; } = [];

    /// <summary>
    /// Products whose negative legacy stock was migrated as zero. Deliberately UNCAPPED, unlike
    /// <see cref="AddError"/>: error strings are capped because one phase failure can produce an
    /// error per invoice line (325,780 on real data), while clamps are bounded by the product count.
    /// This is not an error -- <see cref="Outcome"/> is unaffected and no row failed.
    /// </summary>
    public IReadOnlyList<ClampedStock> ClampedStocks => _clampedStocks;

    public void AddClampedStock(string barcode, string productName, int legacyQuantity) =>
        _clampedStocks.Add(new ClampedStock(barcode, productName, legacyQuantity));

    public MigrationOutcome Outcome =>
        PhaseFailures.Count > 0 ? MigrationOutcome.Aborted :
        AnyRowFailed ? MigrationOutcome.CompletedWithErrors :
        MigrationOutcome.Success;

    /// <remarks>
    /// Defect 12: this MUST count phase failures. Program.cs turns it into the process exit code, so
    /// a phase failure that left IsSuccess true would exit 0 on a run that wrote nothing.
    /// </remarks>
    public bool IsSuccess => Outcome == MigrationOutcome.Success;

    private bool AnyRowFailed => Users.Failed > 0 || Products.Failed > 0 ||
                                 Invoices.Failed > 0 || Payments.Failed > 0 ||
                                 PayLater.Failed > 0;

    /// <summary>
    /// Records a per-row error, capped per phase. The <see cref="EntityMigrationResult.Failed"/>
    /// counts stay exact -- only these strings are capped, because an upstream phase failure makes
    /// every downstream row miss its lookup (up to 325,780 on real data).
    /// </summary>
    public void AddError(string phase, string message)
    {
        var alreadyRecorded = _errorCountByPhase.GetValueOrDefault(phase);
        _errorCountByPhase[phase] = alreadyRecorded + 1;

        if (alreadyRecorded < MaxErrorsPerPhase)
        {
            _errors.Add(message);
            return;
        }

        var note = $"{phase}: {alreadyRecorded + 1 - MaxErrorsPerPhase} further error(s) suppressed.";

        if (_suppressionNoteIndexByPhase.TryGetValue(phase, out var index))
        {
            _errors[index] = note;
            return;
        }

        _errors.Add(note);
        _suppressionNoteIndexByPhase[phase] = _errors.Count - 1;
    }

    public void AddPhaseFailure(string phase, string message) =>
        PhaseFailures.Add(new MigrationPhaseFailure(phase, message));

    public int TotalMigrated => Users.Migrated + Products.Migrated +
                                Invoices.Migrated + Payments.Migrated +
                                PayLater.Migrated;

    // ID mappings for relationships
    public Dictionary<int, Guid> UserIdMap { get; } = [];
    public Dictionary<int, Guid> ProductIdMap { get; } = [];
    public Dictionary<int, Guid> InvoiceIdMap { get; } = [];

    /// <summary>
    /// Legacy <c>Payment.PaymentId</c> to the migrated payment's Guid. PayLater is a 1:1 extension
    /// of Payment, so its rows attach to an already-migrated payment through this map rather than
    /// creating one -- see defect 10.
    /// </summary>
    public Dictionary<int, Guid> PaymentIdMap { get; } = [];
}

public class EntityMigrationResult
{
    public int Migrated { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public int Total => Migrated + Skipped + Failed;
}

public class CloudSyncResult
{
    public bool IsSuccess { get; set; }
    public int EventsSent { get; set; }
    public string? Error { get; set; }
}

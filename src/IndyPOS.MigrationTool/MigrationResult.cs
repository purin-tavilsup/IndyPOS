namespace IndyPOS.MigrationTool;

public class MigrationResult
{
    public EntityMigrationResult Users { get; set; } = new();
    public EntityMigrationResult Products { get; set; } = new();
    public EntityMigrationResult Invoices { get; set; } = new();
    public EntityMigrationResult Payments { get; set; } = new();
    public EntityMigrationResult PayLater { get; set; } = new();

    public List<string> Errors { get; } = [];

    public bool IsSuccess => Users.Failed == 0 &&
                             Products.Failed == 0 &&
                             Invoices.Failed == 0 &&
                             Payments.Failed == 0 &&
                             PayLater.Failed == 0;

    public int TotalMigrated => Users.Migrated + Products.Migrated +
                                Invoices.Migrated + Payments.Migrated +
                                PayLater.Migrated;

    // ID mappings for relationships
    public Dictionary<int, Guid> UserIdMap { get; } = [];
    public Dictionary<int, Guid> ProductIdMap { get; } = [];
    public Dictionary<int, Guid> InvoiceIdMap { get; } = [];
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

using System.Security.Claims;
using System.Text;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.Auth;
using IndyPOS.Application.UseCases.StoreHub.Auth.ChangePassword;
using IndyPOS.Application.UseCases.StoreHub.Auth.Login;
using IndyPOS.Application.UseCases.StoreHub.Products;
using IndyPOS.Application.UseCases.StoreHub.Products.AdjustQuantity;
using IndyPOS.Application.UseCases.StoreHub.Products.Create;
using IndyPOS.Application.UseCases.StoreHub.Products.Delete;
using IndyPOS.Application.UseCases.StoreHub.Products.GenerateBarcode;
using IndyPOS.Application.UseCases.StoreHub.Products.Get;
using IndyPOS.Application.UseCases.StoreHub.Products.Update;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using IndyPOS.Application.UseCases.StoreHub.Reports;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetInvoiceDetail;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetInvoices;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetPayLaterReport;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetProductSales;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetSalesSummary;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetLegacySalesSummary;
using IndyPOS.Application.UseCases.StoreHub.Reports.GetLegacyPaymentsSummary;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.PayLater;
using IndyPOS.Application.UseCases.StoreHub.Sales;
using IndyPOS.Application.UseCases.StoreHub.Sales.Complete;
using IndyPOS.Infrastructure.QueryHandlers.Reports;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.Services.StoreHub;
using IndyPOS.ServiceDefaults;
using IndyPOS.StoreHub.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Nokpirab;
using Scalar.AspNetCore;

// Anchor the content root to the exe directory (not the current working
// directory) so appsettings.json — and its DPAPI-protected connection string —
// load no matter where the process is launched from. The Windows service and
// the installer's "migrate" step already run from the install dir, but the
// operator-facing "reset-admin" recovery command can be run from any CWD;
// without this it fails with "ConnectionString is missing".
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// Decrypt DPAPI-protected secrets before any consumer reads them. No-op in dev:
// Aspire injects an unmarked connection string. See IndyPOS.Vault.
builder.Configuration.UnprotectSecrets(
    "ConnectionStrings:storehub-db",
    "LocalToken:SecretKey");

// Enable Windows Service hosting so SCM's start callback is satisfied within
// 30s (otherwise sc start fails with error 1053). No-op when running as a
// console app (e.g. via Aspire/dotnet run in dev).
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "IndyPOS.StoreHub";
});

// Add Aspire service defaults (health checks, OpenTelemetry, service discovery)
builder.AddServiceDefaults();

// Add PostgreSQL with EF Core via Aspire
// Connection name must match AppHost: postgres.AddDatabase("storehub-db")
builder.AddNpgsqlDbContext<StoreHubDbContext>("storehub-db");

// Readiness check: surfaces DB connectivity at /health/ready. Liveness ("self"
// check tagged "live") comes from ServiceDefaults.AddDefaultHealthChecks.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<StoreHubDbContext>("storehub-db", tags: ["ready"]);

// Add StoreHub infrastructure services (repositories, store identity)
builder.Services.AddStoreHubServices(builder.Configuration);

// Add SyncWorker background service
builder.Services.AddSyncWorker();

// Add auth services
builder.Services.AddStoreHubAuthServices(builder.Configuration);

// Register StoreHub CQRS handlers manually
// Note: We don't use AddApplicationServices() as it registers ALL handlers including legacy ones
builder.Services.AddTransient<IQueryHandler<GetProductsQuery, IReadOnlyList<ProductDto>>, GetProductsQueryHandler>();
builder.Services.AddTransient<ICommandHandler<CompleteSaleCommand, CompleteSaleResponse>, CompleteSaleCommandHandler>();
builder.Services.AddTransient<ICommandHandler<LoginCommand, LoginResponse>, LoginCommandHandler>();
builder.Services.AddTransient<ICommandHandler<ChangePasswordCommand, ChangePasswordResponse>, ChangePasswordCommandHandler>();

// Product write handlers
builder.Services.AddTransient<ICommandHandler<CreateProductCommand, ProductDto>, CreateProductCommandHandler>();
builder.Services.AddTransient<ICommandHandler<UpdateProductCommand, ProductDto>, UpdateProductCommandHandler>();
builder.Services.AddTransient<ICommandHandler<DeleteProductCommand>, DeleteProductCommandHandler>();
builder.Services.AddTransient<ICommandHandler<AdjustProductQuantityCommand, int>, AdjustProductQuantityCommandHandler>();
builder.Services.AddTransient<IQueryHandler<GenerateBarcodeQuery, string>, GenerateBarcodeQueryHandler>();

// Payment methods handlers
builder.Services.AddTransient<IQueryHandler<GetOfferablePaymentMethodsQuery, IReadOnlyList<PaymentMethodDto>>, GetOfferablePaymentMethodsQueryHandler>();

// Register Report query handlers (in Infrastructure layer)
builder.Services.AddTransient<IQueryHandler<GetSalesSummaryQuery, SalesSummaryDto>, GetSalesSummaryQueryHandler>();
builder.Services.AddTransient<IQueryHandler<GetInvoicesQuery, PagedResult<InvoiceSummaryDto>>, GetInvoicesQueryHandler>();
builder.Services.AddTransient<IQueryHandler<GetInvoiceDetailQuery, InvoiceDetailDto?>, GetInvoiceDetailQueryHandler>();
builder.Services.AddTransient<IQueryHandler<GetPayLaterReportQuery, PayLaterReportDto>, GetPayLaterReportQueryHandler>();
builder.Services.AddTransient<IQueryHandler<GetProductSalesQuery, PagedResult<ProductSalesDto>>, GetProductSalesQueryHandler>();

// Legacy report handlers (for WinForms compatibility)
builder.Services.AddTransient<IQueryHandler<GetLegacySalesSummaryQuery, SalesSummary>, GetLegacySalesSummaryQueryHandler>();
builder.Services.AddTransient<IQueryHandler<GetLegacyPaymentsSummaryQuery, PaymentsSummary>, GetLegacyPaymentsSummaryQueryHandler>();

// PayLater handlers (for cashier pay-later management)
builder.Services.AddTransient<IQueryHandler<GetPayLaterQuery, GetPayLaterResponse>, GetPayLaterQueryHandler>();
builder.Services.AddTransient<IQueryHandler<GetPayLaterByIdQuery, PayLaterDto>, GetPayLaterByIdQueryHandler>();
builder.Services.AddTransient<ICommandHandler<RecordPayLaterPaymentCommand, PayLaterDto>, RecordPayLaterPaymentCommandHandler>();

// Add JWT authentication
var tokenOptions = builder.Configuration.GetSection(LocalTokenOptions.SectionName).Get<LocalTokenOptions>()
    ?? new LocalTokenOptions();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
       .AddJwtBearer(options =>
       {
           options.TokenValidationParameters = new TokenValidationParameters
           {
               ValidateIssuer = true,
               ValidateAudience = true,
               ValidateLifetime = true,
               ValidateIssuerSigningKey = true,
               ValidIssuer = tokenOptions.Issuer,
               ValidAudience = tokenOptions.Audience,
               IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenOptions.SecretKey)),
               ClockSkew = TimeSpan.FromMinutes(1)
           };
       });

builder.Services.AddAuthorization();

// Add capability-based authorization (S3: RBAC)
builder.Services.AddSingleton<IAuthorizationHandler, CapabilityAuthorizationHandler>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CanReadProducts", policy =>
        policy.RequireAuthenticatedUser()
              .AddRequirements(new CapabilityRequirement(Capability.ProductsRead)))
    .AddPolicy("CanCompleteSales", policy =>
        policy.RequireAuthenticatedUser()
              .AddRequirements(new CapabilityRequirement(Capability.SalesComplete)))
    .AddPolicy("CanViewSyncStatus", policy =>
        policy.RequireAuthenticatedUser()
              .AddRequirements(new CapabilityRequirement(Capability.SyncViewStatus)))
    .AddPolicy("CanViewReports", policy =>
        policy.RequireAuthenticatedUser()
              .AddRequirements(new CapabilityRequirement(Capability.ReportsView)))
    .AddPolicy("CanManageProducts", policy =>
        policy.RequireAuthenticatedUser()
              .AddRequirements(new CapabilityRequirement(Capability.ProductsManage)))
    .AddPolicy("CanAdjustInventory", policy =>
        policy.RequireAuthenticatedUser()
              .AddRequirements(new CapabilityRequirement(Capability.InventoryAdjust)));

// Add OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Provision the database. Dev uses EnsureCreated + test data for speed.
// Production provisioning (EF migrations + initial admin seed) runs ONLY when
// the installer invokes "IndyPOS.StoreHub.exe migrate", then exits before
// app.Run(). Doing schema work on the normal service-start path would block the
// host's "Running" signal past the 30s SCM start timeout on a fresh DB
// (error 1053); the installer runs this as a console step so the first real
// service start is immediate.
if (app.Environment.IsDevelopment())
{
    await app.EnsureStoreHubDatabaseCreatedAsync();
    await app.SeedDevelopmentDataAsync();
    await app.SeedPaymentMethodsAsync();
}
else if (Array.Exists(args, a => string.Equals(a, "migrate", StringComparison.OrdinalIgnoreCase)))
{
    await app.MigrateStoreHubDatabaseAsync();
    var seeded = await app.SeedInitialAdminAsync();
    await app.SeedPaymentMethodsAsync();
    // Marker consumed by the bootstrapper to decide the finish-screen credential text.
    Console.WriteLine($"ADMIN_SEEDED={(seeded ? "true" : "false")}");
    return;
}
else if (Array.Exists(args, a => string.Equals(a, "reset-admin", StringComparison.OrdinalIgnoreCase)))
{
    await app.MigrateStoreHubDatabaseAsync();
    var newPassword = await app.ResetAdminAsync();
    Console.WriteLine("ADMIN_RESET=true");
    Console.WriteLine($"New admin password (change it on next sign-in): {newPassword}");
    return;
}

// Map default endpoints (health, alive)
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline
app.UseAuthentication();
app.UseAuthorization();

// Force-rotation gate: a token carrying must_change may reach ONLY the
// change-password endpoint. This is the server-side teeth behind the WinForms
// first-login flow — a dismissed dialog or a rogue client cannot bypass it.
app.Use(async (context, next) =>
{
    var mustChange = context.User.FindFirst("must_change")?.Value == "true";
    if (mustChange && !context.Request.Path.StartsWithSegments("/auth/change-password"))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { error = "Password change required before continuing." });
        return;
    }

    await next();
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Minimal API endpoints
app.MapGet("/", () => "IndyPOS StoreHub API");

// Auth endpoints
app.MapPost("/auth/login", async (
    ICommandHandler<LoginCommand, LoginResponse> handler,
    LoginRequest request,
    CancellationToken cancellationToken) =>
{
    var command = new LoginCommand(request.Username, request.Password);
    var response = await handler.HandleAsync(command, cancellationToken);

    return response.Success
        ? Results.Ok(response)
        : Results.Unauthorized();
});

app.MapGet("/auth/me", (HttpContext context) =>
{
    var user = context.User;
    if (user.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value,
        username = user.FindFirst("unique_name")?.Value,
        roleId = user.FindFirst("role_id")?.Value,
        storeId = user.FindFirst("store_id")?.Value,
        firstName = user.FindFirst("first_name")?.Value,
        lastName = user.FindFirst("last_name")?.Value
    });
}).RequireAuthorization();

app.MapPost("/auth/change-password", async (
    ICommandHandler<ChangePasswordCommand, ChangePasswordResponse> handler,
    HttpContext context,
    ChangePasswordRequest request,
    CancellationToken cancellationToken) =>
{
    var idValue = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? context.User.FindFirst("sub")?.Value;

    if (!Guid.TryParse(idValue, out var userId))
    {
        return Results.Unauthorized();
    }

    var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword);
    var response = await handler.HandleAsync(command, cancellationToken);

    return response.Success
        ? Results.Ok(response)
        : Results.BadRequest(new { error = response.ErrorMessage });
}).RequireAuthorization();

// /health/ready is now served by ServiceDefaults.MapDefaultEndpoints (tag
// filter on "ready"), backed by the DbContextCheck registered above.

// Version endpoint (Velopack prep - used for update checks)
app.MapGet("/version", () =>
{
    var versionInfo = IndyPOS.Application.Common.AppVersion.GetVersionInfo(typeof(Program).Assembly);

    return Results.Ok(new
    {
        version = versionInfo.DisplayVersion,
        assemblyVersion = versionInfo.AssemblyVersion,
        fullVersion = versionInfo.InformationalVersion,
        name = "IndyPOS.StoreHub",
        environment = app.Environment.EnvironmentName
    });
});

// Products endpoint
app.MapGet("/products", async (
    IQueryHandler<GetProductsQuery, IReadOnlyList<ProductDto>> handler,
    bool? activeOnly,
    string? category,
    string? search,
    CancellationToken cancellationToken) =>
{
    var query = new GetProductsQuery(
        ActiveOnly: activeOnly ?? true,
        Category: category,
        SearchTerm: search);

    var products = await handler.HandleAsync(query, cancellationToken);
    return Results.Ok(products);
}).RequireAuthorization("CanReadProducts");

// Payment methods endpoint (offerable methods for this store)
app.MapGet("/payment-methods", async (
    IQueryHandler<GetOfferablePaymentMethodsQuery, IReadOnlyList<PaymentMethodDto>> handler,
    CancellationToken cancellationToken) =>
{
    var methods = await handler.HandleAsync(new GetOfferablePaymentMethodsQuery(), cancellationToken);
    return Results.Ok(methods);
}).RequireAuthorization("CanReadProducts");

// Create product
app.MapPost("/products", async (
    ICommandHandler<CreateProductCommand, ProductDto> handler,
    CreateProductCommand command,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(command, cancellationToken);
    return Results.Created($"/products/{result.Id}", result);
}).RequireAuthorization("CanManageProducts");

// Update product
app.MapPut("/products/{id:guid}", async (
    ICommandHandler<UpdateProductCommand, ProductDto> handler,
    Guid id,
    UpdateProductCommand command,
    CancellationToken cancellationToken) =>
{
    // Ensure ID matches
    if (id != command.Id)
    {
        return Results.BadRequest("Product ID in URL does not match body");
    }

    var result = await handler.HandleAsync(command, cancellationToken);
    return Results.Ok(result);
}).RequireAuthorization("CanManageProducts");

// Delete product (soft delete)
app.MapDelete("/products/{id:guid}", async (
    ICommandHandler<DeleteProductCommand> handler,
    Guid id,
    CancellationToken cancellationToken) =>
{
    await handler.HandleAsync(new DeleteProductCommand(id), cancellationToken);
    return Results.NoContent();
}).RequireAuthorization("CanManageProducts");

// Adjust product quantity
app.MapPost("/products/{id:guid}/adjust-quantity", async (
    ICommandHandler<AdjustProductQuantityCommand, int> handler,
    Guid id,
    AdjustQuantityRequest request,
    CancellationToken cancellationToken) =>
{
    var command = new AdjustProductQuantityCommand
    {
        ProductId = id,
        TargetQuantity = request.TargetQuantity,
        Reason = request.Reason
    };

    var newBalance = await handler.HandleAsync(command, cancellationToken);
    return Results.Ok(new { productId = id, quantity = newBalance });
}).RequireAuthorization("CanAdjustInventory");

// Generate next barcode
app.MapPost("/products/next-barcode", async (
    IQueryHandler<GenerateBarcodeQuery, string> handler,
    CancellationToken cancellationToken) =>
{
    var barcode = await handler.HandleAsync(new GenerateBarcodeQuery(), cancellationToken);
    return Results.Ok(new { barcode });
}).RequireAuthorization("CanManageProducts");

// Sales endpoint
app.MapPost("/sales/complete", async (
    ICommandHandler<CompleteSaleCommand, CompleteSaleResponse> handler,
    IStoreIdentityService storeIdentity,
    CompleteSaleRequest request,
    CancellationToken cancellationToken) =>
{
    var command = new CompleteSaleCommand(
        StoreId: storeIdentity.StoreId,
        UserId: request.UserId,
        Lines: request.Lines,
        Payments: request.Payments);

    var response = await handler.HandleAsync(command, cancellationToken);
    return Results.Ok(response);
}).RequireAuthorization("CanCompleteSales");

// Sync status endpoint (E4)
app.MapGet("/sync/status", async (
    IOutboxRepository outboxRepository,
    CancellationToken cancellationToken) =>
{
    var pendingCount = await outboxRepository.GetPendingCountAsync(cancellationToken);
    var failedCount = await outboxRepository.GetFailedCountAsync(cancellationToken);

    return Results.Ok(new
    {
        status = pendingCount == 0 ? "synced" : "pending",
        pending = pendingCount,
        failed = failedCount,
        timestamp = DateTime.UtcNow
    });
}).RequireAuthorization("CanViewSyncStatus");

// ========================
// Report endpoints
// ========================

// Sales summary (daily/weekly/monthly dashboard)
app.MapGet("/reports/sales-summary", async (
    IQueryHandler<GetSalesSummaryQuery, SalesSummaryDto> handler,
    DateOnly fromDate,
    DateOnly toDate,
    int? topProductsCount,
    CancellationToken cancellationToken) =>
{
    var query = new GetSalesSummaryQuery(
        FromDate: fromDate,
        ToDate: toDate,
        TopProductsCount: topProductsCount ?? 10);

    var result = await handler.HandleAsync(query, cancellationToken);
    return Results.Ok(result);
}).RequireAuthorization("CanViewReports");

// Invoice list (paginated)
app.MapGet("/reports/invoices", async (
    IQueryHandler<GetInvoicesQuery, PagedResult<InvoiceSummaryDto>> handler,
    DateOnly fromDate,
    DateOnly toDate,
    int? page,
    int? pageSize,
    CancellationToken cancellationToken) =>
{
    var query = new GetInvoicesQuery(
        FromDate: fromDate,
        ToDate: toDate,
        Page: page ?? 1,
        PageSize: pageSize ?? 50);

    var result = await handler.HandleAsync(query, cancellationToken);
    return Results.Ok(result);
}).RequireAuthorization("CanViewReports");

// Invoice detail
app.MapGet("/reports/invoices/{invoiceId:guid}", async (
    IQueryHandler<GetInvoiceDetailQuery, InvoiceDetailDto?> handler,
    Guid invoiceId,
    CancellationToken cancellationToken) =>
{
    var query = new GetInvoiceDetailQuery(invoiceId);
    var result = await handler.HandleAsync(query, cancellationToken);

    return result is null
        ? Results.NotFound()
        : Results.Ok(result);
}).RequireAuthorization("CanViewReports");

// PayLater (accounts receivable) report
app.MapGet("/reports/pay-later", async (
    IQueryHandler<GetPayLaterReportQuery, PayLaterReportDto> handler,
    bool? includeCompleted,
    int? page,
    int? pageSize,
    CancellationToken cancellationToken) =>
{
    var query = new GetPayLaterReportQuery(
        IncludeCompleted: includeCompleted ?? false,
        Page: page ?? 1,
        PageSize: pageSize ?? 50);

    var result = await handler.HandleAsync(query, cancellationToken);
    return Results.Ok(result);
}).RequireAuthorization("CanViewReports");

// Product sales report
app.MapGet("/reports/product-sales", async (
    IQueryHandler<GetProductSalesQuery, PagedResult<ProductSalesDto>> handler,
    DateOnly fromDate,
    DateOnly toDate,
    string? category,
    int? page,
    int? pageSize,
    CancellationToken cancellationToken) =>
{
    var query = new GetProductSalesQuery(
        FromDate: fromDate,
        ToDate: toDate,
        Category: category,
        Page: page ?? 1,
        PageSize: pageSize ?? 50);

    var result = await handler.HandleAsync(query, cancellationToken);
    return Results.Ok(result);
}).RequireAuthorization("CanViewReports");

// ========================
// Legacy Report Endpoints (for WinForms compatibility)
// ========================

// Legacy sales summary (returns SalesSummary model)
app.MapGet("/reports/legacy/sales-summary", async (
    IQueryHandler<GetLegacySalesSummaryQuery, SalesSummary> handler,
    DateOnly fromDate,
    DateOnly toDate,
    CancellationToken cancellationToken) =>
{
    var query = new GetLegacySalesSummaryQuery(fromDate, toDate);
    var result = await handler.HandleAsync(query, cancellationToken);
    return Results.Ok(result);
}).RequireAuthorization("CanViewReports");

// Legacy payments summary (returns PaymentsSummary model)
app.MapGet("/reports/legacy/payments-summary", async (
    IQueryHandler<GetLegacyPaymentsSummaryQuery, PaymentsSummary> handler,
    DateOnly fromDate,
    DateOnly toDate,
    CancellationToken cancellationToken) =>
{
    var query = new GetLegacyPaymentsSummaryQuery(fromDate, toDate);
    var result = await handler.HandleAsync(query, cancellationToken);
    return Results.Ok(result);
}).RequireAuthorization("CanViewReports");

// ========================
// PayLater endpoints (for cashiers to view and update pay-later accounts)
// ========================

// List pay-later records
app.MapGet("/pay-later", async (
    IQueryHandler<GetPayLaterQuery, GetPayLaterResponse> handler,
    bool? includeCompleted,
    string? search,
    CancellationToken cancellationToken) =>
{
    var query = new GetPayLaterQuery(
        IncludeCompleted: includeCompleted ?? false,
        SearchTerm: search);

    var result = await handler.HandleAsync(query, cancellationToken);
    return Results.Ok(result);
}).RequireAuthorization();

// Get single pay-later record
app.MapGet("/pay-later/{id:guid}", async (
    IQueryHandler<GetPayLaterByIdQuery, PayLaterDto> handler,
    Guid id,
    CancellationToken cancellationToken) =>
{
    try
    {
        var result = await handler.HandleAsync(new GetPayLaterByIdQuery(id), cancellationToken);
        return Results.Ok(result);
    }
    catch (PayLaterPaymentNotFoundException)
    {
        return Results.NotFound();
    }
}).RequireAuthorization();

// Record payment against pay-later
app.MapPost("/pay-later/{id:guid}/record-payment", async (
    ICommandHandler<RecordPayLaterPaymentCommand, PayLaterDto> handler,
    Guid id,
    RecordPaymentRequest request,
    CancellationToken cancellationToken) =>
{
    try
    {
        var command = new RecordPayLaterPaymentCommand(id, request.PaymentAmount);
        var result = await handler.HandleAsync(command, cancellationToken);
        return Results.Ok(result);
    }
    catch (PayLaterPaymentNotFoundException)
    {
        return Results.NotFound();
    }
    catch (PayLaterPaymentNotUpdatedException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).RequireAuthorization();

app.Run();

// Make the implicit Program class public so test projects can access it
public partial class Program;

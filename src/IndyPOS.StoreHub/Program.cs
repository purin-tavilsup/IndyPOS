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
using IndyPOS.Application.UseCases.StoreHub.Products.GetStock;
using IndyPOS.Application.UseCases.StoreHub.Products.Update;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using IndyPOS.Application.UseCases.StoreHub.ProductCategories;
using IndyPOS.Application.UseCases.StoreHub.Reports;
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
using IndyPOS.StoreHub.Endpoints.Auth;
using IndyPOS.StoreHub.Endpoints.Cash;
using IndyPOS.StoreHub.Endpoints.Catalogue;
using IndyPOS.StoreHub.Endpoints.Sync;
using IndyPOS.StoreHub.Endpoints.PaymentMethods;
using IndyPOS.StoreHub.Endpoints.Products;
using IndyPOS.StoreHub.Endpoints.Sales;
using IndyPOS.StoreHub.Endpoints.SystemInfo;
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
builder.Services.AddTransient<IQueryHandler<GetProductStockQuery, IReadOnlyList<ProductStockDto>>, GetProductStockQueryHandler>();
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
builder.Services.AddTransient<IQueryHandler<GetAllPaymentMethodsQuery, IReadOnlyList<PaymentMethodDto>>, GetAllPaymentMethodsQueryHandler>();
builder.Services.AddTransient<IQueryHandler<GetProductCategoriesQuery, IReadOnlyList<ProductCategoryDto>>, GetProductCategoriesQueryHandler>();
builder.Services.AddTransient<ICommandHandler<AddCampaignPaymentMethodCommand, PaymentMethodMutationResponse>, AddCampaignPaymentMethodCommandHandler>();
builder.Services.AddTransient<ICommandHandler<TogglePaymentMethodCommand, PaymentMethodMutationResponse>, TogglePaymentMethodCommandHandler>();
builder.Services.AddTransient<ICommandHandler<EditPaymentMethodDisplayCommand, PaymentMethodMutationResponse>, EditPaymentMethodDisplayCommandHandler>();

// Register Report query handlers (in Infrastructure layer)
builder.Services.AddTransient<IQueryHandler<GetSalesSummaryQuery, SalesSummaryDto>, GetSalesSummaryQueryHandler>();
builder.Services.AddTransient<IQueryHandler<GetPayLaterReportQuery, PayLaterReportDto>, GetPayLaterReportQueryHandler>();
builder.Services.AddTransient<IQueryHandler<GetProductSalesQuery, PagedResult<ProductSalesDto>>, GetProductSalesQueryHandler>();

// Legacy report handlers (for WinForms compatibility)
builder.Services.AddTransient<IQueryHandler<GetLegacySalesSummaryQuery, SalesSummary>, GetLegacySalesSummaryQueryHandler>();
builder.Services.AddTransient<IQueryHandler<GetLegacyPaymentsSummaryQuery, PaymentsSummary>, GetLegacyPaymentsSummaryQueryHandler>();

// PayLater handlers (for cashier pay-later management)
builder.Services.AddTransient<IQueryHandler<GetPayLaterQuery, GetPayLaterResponse>, GetPayLaterQueryHandler>();
builder.Services.AddTransient<IQueryHandler<GetPayLaterByIdQuery, PayLaterDto>, GetPayLaterByIdQueryHandler>();
builder.Services.AddTransient<ICommandHandler<RecordPayLaterPaymentCommand, PayLaterDto>, RecordPayLaterPaymentCommandHandler>();

// Cash drawer (ลิ้นชักเก็บเงิน): clock + handlers
builder.Services.AddCashDrawer();

// Sales history (/sales): list, detail, reprint
builder.Services.AddSalesHistory();

// Add JWT authentication
var tokenOptions = builder.Configuration.GetSection(LocalTokenOptions.SectionName).Get<LocalTokenOptions>()
    ?? new LocalTokenOptions();

LocalTokenOptionsValidator.EnsureProductionSafe(
    tokenOptions,
    builder.Environment.IsDevelopment());

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
              .AddRequirements(new CapabilityRequirement(Capability.InventoryAdjust)))
    .AddPolicy("CanManagePaymentMethods", policy =>
        policy.RequireAuthenticatedUser()
              .AddRequirements(new CapabilityRequirement(Capability.PaymentMethodsManage)))
    .AddPolicy(CashEndpoints.Policy, policy =>
        policy.RequireAuthenticatedUser()
              .AddRequirements(new CapabilityRequirement(Capability.CashManage)))
    .AddPolicy(SalesEndpoints.Policy, policy =>
        policy.RequireAuthenticatedUser()
              .AddRequirements(new CapabilityRequirement(Capability.SalesReprint)));

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
    await app.SeedProductCategoriesAsync();
}
else if (Array.Exists(args, a => string.Equals(a, "migrate", StringComparison.OrdinalIgnoreCase)))
{
    await app.MigrateStoreHubDatabaseAsync();
    var seeded = await app.SeedInitialAdminAsync();
    await app.SeedPaymentMethodsAsync();
    await app.SeedProductCategoriesAsync();
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

// Routes, one file per area under Endpoints/. /health/ready is served by
// ServiceDefaults.MapDefaultEndpoints (tag filter on "ready"), backed by the DbContextCheck
// registered above.
app.MapSystemInfoEndpoints();
app.MapAuthEndpoints();
app.MapProductsEndpoints();
app.MapPaymentMethodsEndpoints();
app.MapCatalogueEndpoints();
app.MapSalesEndpoints();
app.MapSyncEndpoints();

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

// Cash drawer routes (/cash/...)
app.MapCashEndpoints();

app.Run();

// Make the implicit Program class public so test projects can access it
public partial class Program;

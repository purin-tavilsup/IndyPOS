using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests;

[Collection("Integration")]
public class InvoiceReprintPersistenceTests : IntegrationTestBase
{
    public InvoiceReprintPersistenceTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Insert_ForAnUnknownInvoice_ThrowsForeignKeyViolation()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        db.InvoiceReprints.Add(new InvoiceReprint
        {
            Id = Guid.NewGuid(), InvoiceId = Guid.NewGuid(), StoreId = TestStoreIdentityService.TestStoreId,
            CreatedUtc = DateTime.UtcNow, LastModifiedUtc = DateTime.UtcNow, CreatedByUserId = Guid.NewGuid()
        });

        var act = () => db.SaveChangesAsync();

        await act.Should()
                 .ThrowAsync<DbUpdateException>();
    }
}

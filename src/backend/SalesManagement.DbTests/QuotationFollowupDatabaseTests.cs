using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using SalesManagement.Core.Models;
using SalesManagement.Core.Quotations;
using SalesManagement.Postgres;
using SalesManagement.Postgres.Migrations;
using SalesManagement.Postgres.Repositories;

namespace SalesManagement.DbTests;

public sealed class QuotationFollowupDatabaseTests
{
    [Fact]
    public void CommercialTermsHaveNullableBoundedColumns()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        using var db = Context(connection);
        var entity = db.Model.FindEntityType(typeof(QuoteRevision))!;
        foreach (var (field, length) in new[] { ("PaymentTerms", 500), ("DeliveryTerms", 500), ("Notes", 4000) })
        {
            var property = entity.FindProperty(field)!;
            Assert.True(property.IsNullable);
            Assert.Equal(length, property.GetMaxLength());
        }
    }

    [Fact]
    public async Task SeedsElevenTermsWithoutOverwritingManagerSettings()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:"); await connection.OpenAsync();
        await using var db = Context(connection); await db.Database.EnsureCreatedAsync();
        var custom = new Incoterm { Code = "CIF", DisplayName = "사용자 지정 이름", Status = EntityStatus.Inactive, SortOrder = 999 };
        db.Add(custom); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var migration = new SeedIncoterms2020();
        for (var run = 0; run < 2; run++)
            foreach (var operation in migration.UpOperations.Cast<SqlOperation>()) await db.Database.ExecuteSqlRawAsync(operation.Sql);
        var terms = await db.Incoterms.AsNoTracking().OrderBy(x => x.Code).ToArrayAsync();
        Assert.Equal(new[] { "CFR", "CIF", "CIP", "CPT", "DAP", "DDP", "DPU", "EXW", "FAS", "FCA", "FOB" }, terms.Select(x => x.Code));
        Assert.Equal("사용자 지정 이름", terms.Single(x => x.Code == "CIF").DisplayName);
        Assert.Equal(EntityStatus.Inactive, terms.Single(x => x.Code == "CIF").Status);
        Assert.Equal(999, terms.Single(x => x.Code == "CIF").SortOrder);
        Assert.All(terms.Where(x => x.Code != "CIF"), x => Assert.Equal(EntityStatus.Active, x.Status));
    }

    [Fact]
    public async Task DraftLineReplacementPersistsAcrossContextsAndPreservesHistory()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:"); await connection.OpenAsync();
        Guid quoteId; Guid oldRevisionId;
        await using (var db = Context(connection))
        {
            await db.Database.EnsureCreatedAsync();
            var user = new User { LoginId = "manager", PasswordHash = "hash", Name = "팀장", Email = "manager@example.test", Role = Role.Manager };
            var customer = new Customer { Code = "C1", CompanyName = "거래처", Country = "KR", OwnerUserId = user.Id };
            var product = new Product { Code = "P1", Name = "스위치", ItemType = ItemType.Product };
            db.AddRange(user, customer, product); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var repo = new QuoteRepository(db); var service = new QuoteService(repo, new CustomerRepository(db), new InquiryRepository(db));
            var lines = new[] { new QuoteLineInput(product.Id, "기존 품목", 1, 100) };
            var quote = await service.CreateAsync(new(null, customer.Id, user.Id, QuoteTradeType.Domestic, Currency.KRW, QuoteTaxType.Exclusive, new(2026, 12, 31), null, null, 0, 0, 0, lines, "T/T 30%", "4 weeks", "Original notes"), user);
            quoteId = quote.Id; var revision = quote.Revisions.Single(); oldRevisionId = revision.Id;
            db.ChangeTracker.Clear();
            revision = await service.SubmitAsync(quoteId, revision.Id, revision.Version, user);
            revision = await service.ApproveAsync(quoteId, revision.Id, revision.Version, user);
            quote = (await repo.GetAsync(quoteId))!;
            var draft = await service.CreateRevisionAsync(quoteId, quote.Version, user);
            Assert.Equal("T/T 30%", draft.PaymentTerms);
            var edited = await service.SaveRevisionAsync(quoteId, draft.Id, new(QuoteTradeType.Domestic, Currency.KRW, QuoteTaxType.Exclusive, draft.ValidUntil, null, null, 0, 0, 0,
                [new(product.Id, "추가 A", 2, 200), new(product.Id, "추가 B", 3, 300)], draft.Version), user);
            var stored = (await repo.GetAsync(quoteId))!.Revisions.Single(x => x.Id == edited.Id);
            Assert.Equal(2, stored.Lines.Count);
            Assert.Equal(edited.Version, stored.Version);
            await service.SaveRevisionAsync(quoteId, edited.Id, new(QuoteTradeType.Domestic, Currency.KRW, QuoteTaxType.Exclusive, edited.ValidUntil, null, null, 0, 0, 0,
                [new(product.Id, "추가 B", 3, 300)], stored.Version, "T/T 100%", "6 weeks", "New notes\nSecond line"), user);
        }
        await using (var fresh = Context(connection))
        {
            var quote = (await new QuoteRepository(fresh).GetAsync(quoteId))!;
            var old = quote.Revisions.Single(x => x.Id == oldRevisionId);
            Assert.Equal("기존 품목", old.Lines.Single().Description);
            Assert.Single(old.Approvals);
            Assert.Equal("T/T 30%", old.PaymentTerms);
            Assert.Equal("4 weeks", old.DeliveryTerms);
            Assert.Equal("Original notes", old.Notes);
            var current = quote.Revisions.Single(x => x.Id == quote.CurrentRevisionId);
            Assert.Equal("추가 B", current.Lines.Single().Description);
            Assert.Equal(990, current.TotalAmount);
            Assert.Equal("T/T 100%", current.PaymentTerms);
            Assert.Equal("6 weeks", current.DeliveryTerms);
            Assert.Equal("New notes\nSecond line", current.Notes);
            var tampered = quote.Revisions.Single(x => x.Id == oldRevisionId);
            tampered.PaymentTerms = "Unauthorized change";
            await Assert.ThrowsAsync<InvalidDataException>(() => new QuoteRepository(fresh).UpdateAsync(quote, quote.Version));
            fresh.ChangeTracker.Clear();
            tampered.PaymentTerms = "T/T 30%";
            tampered.Lines.Clear();
            await Assert.ThrowsAsync<InvalidDataException>(() => new QuoteRepository(fresh).UpdateAsync(quote, quote.Version));
        }
    }

    private static SalesDbContext Context(SqliteConnection connection) => new(new DbContextOptionsBuilder<SalesDbContext>().UseSqlite(connection).Options);
}

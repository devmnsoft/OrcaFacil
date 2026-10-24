using OrcaFacil.Application.Privacy;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class AccountDataBackupTests
{
    private static readonly Guid Acct = Guid.NewGuid();
    private static readonly Guid Cid = Guid.NewGuid();

    private static Client MkClient() => new() { AccountId = Acct, Name = "Fulano", Email = "a@b.c", Phone = "11999", City = "SP", IsActive = true };
    private static ServiceCatalogItem MkSvc() => new() { AccountId = Acct, Code = "SVC-1", Name = "Manutencao", StandardPrice = 500m, IsActive = true };
    private static Document MkDoc()
    {
        var d = new Document { AccountId = Acct, ClientId = Cid, ClientName = "ACME", Type = DocumentType.Budget, Status = "Approved", IssueDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc) };
        d.IssueNumber("ORC-1");
        d.Items = [new DocumentItem { Description = "Item", Quantity = 2m, UnitPrice = 100m, Discount = 0m }];
        return d;
    }
    private static WorkOrder MkWo() => new() { AccountId = Acct, ClientId = Cid, Number = "OS-1", Title = "Manutencao", Status = WorkOrderStatus.Planned, ServiceCompetence = DateOnly.FromDateTime(new DateTime(2026, 2, 1)), ScheduledStart = new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc), TotalSnapshot = 500m, PaymentReceived = false };
    private static Receipt MkRc() => new() { AccountId = Acct, PaymentId = Guid.NewGuid(), ClientId = Cid, Number = "REC-1", Amount = 1000m, PaymentMethod = "Pix", IssuedAt = new DateTime(2026, 2, 5, 12, 0, 0, DateTimeKind.Utc), ServiceDescription = "Manutencao" };
    private static FinancialEntry MkFin() => new() { AccountId = Acct, ClientId = Cid, Description = "Recebivel OS-1", DueDate = DateOnly.FromDateTime(new DateTime(2026, 2, 10)), Amount = 1000m, PaidAmount = 1000m, Status = FinancialEntryStatus.Paid };
    private static RecurringContract MkContract() => new() { AccountId = Acct, ClientId = Cid, Number = "CON-1", Title = "Mensal", Status = ContractStatus.Active, StartDate = DateOnly.FromDateTime(new DateTime(2026, 1, 1)), RecurringAmount = 200m, Periodicity = RecurrencePeriod.Monthly };
    private static FiscalDocumentRequest MkNote() => new() { AccountId = Acct, FiscalDocumentType = "NFS-e", Status = FiscalRequestStatus.Issued, ExternalNumber = "NFE-7", IssuedAt = new DateTime(2026, 2, 6, 0, 0, 0, DateTimeKind.Utc), ProviderName = "Controle manual auditado", ExternalKeyMasked = "abcd****wxyz" };

    private static AccountDataBackup ShapeOne(bool includesFinancial) =>
        AccountDataBackupShaper.Shape(
            clients: [MkClient()], services: [MkSvc()], documents: [MkDoc()], workOrders: [MkWo()], receipts: [MkRc()],
            includesFinancial, financialEntries: includesFinancial ? [MkFin()] : null, contracts: [MkContract()], fiscalNotes: [MkNote()]);

    [Fact] public void Financial_excluded_when_not_included()
    {
        var b = ShapeOne(includesFinancial: false);
        Assert.Null(b.FinancialEntries);
        Assert.Equal("account.core", AccountDataBackupShaper.Scope(false));
        Assert.Equal(7, AccountDataBackupShaper.TotalRecords(b)); // seven core collections, one each
    }

    [Fact] public void Financial_included_when_permitted_and_requested()
    {
        var b = ShapeOne(includesFinancial: true);
        Assert.NotNull(b.FinancialEntries);
        Assert.Single(b.FinancialEntries!);
        Assert.Equal("Recebivel OS-1", b.FinancialEntries![0].Description);
        Assert.Equal(FinancialEntryStatus.Paid.ToString(), b.FinancialEntries[0].Status);
        Assert.Equal(8, AccountDataBackupShaper.TotalRecords(b));
        Assert.Equal("account.full", AccountDataBackupShaper.Scope(true));
    }

    [Fact] public void Financial_stays_null_when_flag_true_but_no_data_available()
    {
        var b = AccountDataBackupShaper.Shape([MkClient()], [], [], [], [], includesFinancial: true, financialEntries: null, [], []);
        Assert.Null(b.FinancialEntries);
    }

    [Fact] public void Enum_values_render_as_names_and_items_are_mapped()
    {
        var b = ShapeOne(includesFinancial: false);
        Assert.Equal(DocumentType.Budget.ToString(), b.Documents[0].Type);
        Assert.Equal("ORC-1", b.Documents[0].Number);
        Assert.Single(b.Documents[0].Items);
        Assert.Equal(PersonType.Individual.ToString(), b.Clients[0].PersonType);
        Assert.Equal(FiscalRequestStatus.Issued.ToString(), b.FiscalNotes[0].Status);
        Assert.Equal("NFE-7", b.FiscalNotes[0].Number);
    }

    [Fact] public void Empty_account_produces_empty_lists_and_zero_records()
    {
        var b = AccountDataBackupShaper.Shape([], [], [], [], [], false, null, [], []);
        Assert.Empty(b.Clients);
        Assert.Empty(b.Documents);
        Assert.Null(b.FinancialEntries);
        Assert.Equal(0, AccountDataBackupShaper.TotalRecords(b));
        Assert.Equal(AccountDataBackupShaper.SchemaVersion, b.SchemaVersion);
    }
}

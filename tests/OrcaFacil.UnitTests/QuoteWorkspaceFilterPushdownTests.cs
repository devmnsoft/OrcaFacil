using Xunit;

namespace OrcaFacil.UnitTests;

/// <summary>
/// Guards the P0 performance contract: financial min/max filters and value sorting must be pushed to
/// PostgreSQL whenever the account has no versioned contracts, without losing the safe in-memory
/// fallback for accounts that do have revisions or work orders.
/// </summary>
public sealed class QuoteWorkspaceFilterPushdownTests
{
    private static string ReadQuoteWorkspaceService()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        return File.ReadAllText(Path.Combine(root, "src", "OrcaFacil.Persistence", "Services", "QuoteWorkspaceService.cs"));
    }

    [Fact]
    public void Financial_filters_are_translated_when_there_are_no_versioned_contracts()
    {
        var source = ReadQuoteWorkspaceService();

        Assert.Contains("pushFinancialToSql", source);
        Assert.Contains("document.Total >= minFilter", source);
        Assert.Contains("document.Total <= maxFilter", source);
    }

    [Fact]
    public void Accounts_with_revisions_or_work_orders_keep_the_in_memory_fallback()
    {
        var source = ReadQuoteWorkspaceService();

        Assert.Contains("needsFinancialMaterialization = wantsFinancialFilter && !pushFinancialToSql", source);
        Assert.Contains("db.DocumentRevisions.AnyAsync(x => x.AccountId == accountId", source);
        Assert.Contains("db.WorkOrders.AnyAsync(x => x.AccountId == accountId", source);
        Assert.Contains("Skip((page - 1) * pageSize)", source);
    }

    [Fact]
    public void Value_sort_is_applied_in_sql_when_pushed_down()
    {
        var source = ReadQuoteWorkspaceService();

        Assert.Contains("OrderByDescending(document => document.Total)", source);
        Assert.Contains("OrderBy(document => document.Total)", source);
    }
}

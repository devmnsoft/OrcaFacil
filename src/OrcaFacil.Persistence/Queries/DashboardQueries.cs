using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.DTOs;

namespace OrcaFacil.Persistence.Queries;

public class DashboardQueries : IDashboardQueries
{
    private readonly IConfiguration _configuration;

    public DashboardQueries(IConfiguration configuration) => _configuration = configuration;

    public async Task<DashboardDto> GetDashboardAsync(Guid userId, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        var metrics = await connection.QuerySingleAsync<(int TotalDocuments, int TotalBudgets, int TotalReceipts, decimal BudgetTotal, decimal ReceiptTotal, int DocumentsThisMonth)>(new CommandDefinition("""
            select count(*)::int as TotalDocuments,
                   count(*) filter (where type = 'Budget')::int as TotalBudgets,
                   count(*) filter (where type = 'Receipt')::int as TotalReceipts,
                   coalesce(sum(total) filter (where type = 'Budget'), 0) as BudgetTotal,
                   coalesce(sum(total) filter (where type = 'Receipt'), 0) as ReceiptTotal,
                   count(*) filter (where date_trunc('month', created_at) = date_trunc('month', now()))::int as DocumentsThisMonth
              from orcafacil.documents
             where user_id = @userId and is_deleted = false
            """, new { userId }, cancellationToken: ct));
        var plan = await connection.ExecuteScalarAsync<string>(new CommandDefinition("select plan::text from orcafacil.users where id = @userId", new { userId }, cancellationToken: ct)) ?? "Free";
        var pdfs = await connection.ExecuteScalarAsync<int>(new CommandDefinition("select coalesce(pdf_generated, 0) from orcafacil.user_usage where user_id = @userId and period = to_char(now(), 'YYYY-MM')", new { userId }, cancellationToken: ct));
        var latest = (await connection.QueryAsync<DocumentSummaryDto>(new CommandDefinition("""
            select id as Id, type::text as Type, number as Number, status as Status, client_name as ClientName, total as Total, created_at as CreatedAt
              from orcafacil.documents
             where user_id = @userId and is_deleted = false
             order by created_at desc
             limit 5
            """, new { userId }, cancellationToken: ct))).AsList();
        return new DashboardDto(metrics.TotalDocuments, metrics.TotalBudgets, metrics.TotalReceipts, metrics.BudgetTotal, metrics.ReceiptTotal, metrics.DocumentsThisMonth, pdfs, plan, latest);
    }
}

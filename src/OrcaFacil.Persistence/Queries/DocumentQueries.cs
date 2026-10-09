using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Application.DTOs;

namespace OrcaFacil.Persistence.Queries;

public class DocumentQueries : IDocumentQueries
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DocumentQueries> _logger;

    public DocumentQueries(IConfiguration configuration, ILogger<DocumentQueries> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DocumentSummaryDto>> ListDocumentsAsync(Guid userId, Guid? accountId = null, CancellationToken ct = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            var sql = accountId.HasValue
                ? """
                    select id as Id,
                           type::text as Type,
                           number as Number,
                           status as Status,
                           client_name as ClientName,
                           total as Total,
                           created_at as CreatedAt
                      from orcafacil.documents
                     where account_id = @accountId and is_deleted = false
                     order by created_at desc
                    """
                : """
                    select id as Id,
                           type::text as Type,
                           number as Number,
                           status as Status,
                           client_name as ClientName,
                           total as Total,
                           created_at as CreatedAt
                      from orcafacil.documents
                     where user_id = @userId and account_id is null and is_deleted = false
                     order by created_at desc
                    """;
            return (await connection.QueryAsync<DocumentSummaryDto>(new CommandDefinition(sql, new { userId, accountId }, cancellationToken: ct))).AsList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar documentos da conta {AccountId} / usuário {UserId}", accountId, userId);
            throw;
        }
    }
}

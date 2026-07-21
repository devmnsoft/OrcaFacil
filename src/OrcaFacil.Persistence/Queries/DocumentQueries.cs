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

    public async Task<IReadOnlyList<DocumentSummaryDto>> ListDocumentsAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            const string sql = """
                select id as Id,
                       type::text as Type,
                       number as Number,
                       status as Status,
                       client_name as ClientName,
                       total as Total,
                       created_at as CreatedAt
                  from core.documents
                 where user_id = @userId
                 order by created_at desc
                """;
            return (await connection.QueryAsync<DocumentSummaryDto>(new CommandDefinition(sql, new { userId }, cancellationToken: ct))).AsList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar documentos do usuário {UserId}", userId);
            throw;
        }
    }
}

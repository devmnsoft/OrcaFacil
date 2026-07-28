namespace OrcaFacil.Persistence.Diagnostics;

public enum DatabaseFailureCategory { Authentication, Unavailable, DatabaseMissing, SchemaMissing, Configuration, Unexpected }

public sealed record DatabaseFailure(DatabaseFailureCategory Category, string? SqlState, string PublicMessage, string AdminMessage);

public static class PostgresFailureClassifier
{
    public static DatabaseFailure Classify(Exception exception)
    {
        var sqlState = FindSqlState(exception);
        return sqlState switch
        {
            "28P01" => new(DatabaseFailureCategory.Authentication, sqlState,
                "Não conseguimos acessar os dados agora. Tente novamente em alguns instantes.",
                "A senha configurada na aplicação não corresponde à senha do usuário PostgreSQL. Atualize o secret ou redefina a senha do role. Reinicie a aplicação depois da alteração."),
            "3D000" => new(DatabaseFailureCategory.DatabaseMissing, sqlState, "O serviço de dados está temporariamente indisponível.", "O banco configurado não existe."),
            "42P01" or "3F000" => new(DatabaseFailureCategory.SchemaMissing, sqlState, "O serviço de dados está temporariamente indisponível.", "O schema ou uma tabela obrigatória não existe."),
            null => new(DatabaseFailureCategory.Unavailable, null, "O serviço de dados está temporariamente indisponível.", "Não foi possível conectar ao serviço de dados."),
            _ => new(DatabaseFailureCategory.Unexpected, sqlState, "O serviço de dados está temporariamente indisponível.", "Falha inesperada no serviço de dados.")
        };
    }

    private static string? FindSqlState(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current.GetType().GetProperty("SqlState")?.GetValue(current) is string value) return value;
        return null;
    }
}

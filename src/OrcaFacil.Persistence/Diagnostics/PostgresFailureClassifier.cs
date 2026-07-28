namespace OrcaFacil.Persistence.Diagnostics;

public enum DatabaseFailureCategory
{
    MissingClientPassword, InvalidServerPassword, DatabaseUnavailable, DatabaseMissing,
    SchemaMissing, PermissionDenied, Timeout, Unexpected,
    Authentication = InvalidServerPassword, Unavailable = DatabaseUnavailable, Configuration = MissingClientPassword
}

public sealed record DatabaseFailure(DatabaseFailureCategory Category, string? SqlState, string PublicMessage, string AdminMessage);

public static class PostgresFailureClassifier
{
    public static DatabaseFailure Classify(Exception exception, IDatabaseConfigurationState? configuration = null)
    {
        if (configuration is { HasPassword: false })
            return new(DatabaseFailureCategory.MissingClientPassword, null, "Não conseguimos acessar os dados agora.",
                "A configuração do banco não possui uma senha válida.");

        var sqlState = FindSqlState(exception);
        return sqlState switch
        {
            "28P01" => new(DatabaseFailureCategory.InvalidServerPassword, sqlState,
                "Não conseguimos acessar os dados agora. Tente novamente em alguns instantes.",
                "A senha configurada na aplicação não corresponde à senha do usuário PostgreSQL. Atualize o secret ou redefina a senha do role. Reinicie a aplicação depois da alteração."),
            "3D000" => new(DatabaseFailureCategory.DatabaseMissing, sqlState, "O serviço de dados está temporariamente indisponível.", "O banco configurado não existe."),
            "42P01" or "3F000" => new(DatabaseFailureCategory.SchemaMissing, sqlState, "O serviço de dados está temporariamente indisponível.", "O schema ou uma tabela obrigatória não existe."),
            "42501" => new(DatabaseFailureCategory.PermissionDenied, sqlState, "O serviço de dados está temporariamente indisponível.", "O usuário não possui permissão para esta operação."),
            "57014" => new(DatabaseFailureCategory.Timeout, sqlState, "O serviço de dados está temporariamente indisponível.", "A operação excedeu o tempo limite."),
            null when exception is TimeoutException => new(DatabaseFailureCategory.Timeout, null, "O serviço de dados está temporariamente indisponível.", "A conexão excedeu o tempo limite."),
            null => new(DatabaseFailureCategory.DatabaseUnavailable, null, "O serviço de dados está temporariamente indisponível.", "Não foi possível conectar ao serviço de dados."),
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

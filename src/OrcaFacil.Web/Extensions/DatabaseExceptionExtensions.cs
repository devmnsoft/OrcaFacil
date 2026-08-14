namespace OrcaFacil.Web.Extensions;

public static class DatabaseExceptionExtensions
{
    public const string InvalidPasswordSqlState = "28P01";

    public static bool IsPostgresInvalidPassword(this Exception exception) =>
        exception.FindPostgresSqlState(InvalidPasswordSqlState) is not null;

    public static bool IsPostgresUndefinedColumn(this Exception exception) =>
        exception.FindPostgresSqlState("42703") is not null;

    public static Exception? FindPostgresSqlState(this Exception exception, string sqlState)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            var value = current.GetType().GetProperty("SqlState")?.GetValue(current)?.ToString();
            if (string.Equals(value, sqlState, StringComparison.OrdinalIgnoreCase)) return current;
        }
        return null;
    }

    public static RegistrationDatabaseFailure GetRegistrationFailure(this Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            var sqlState = current.GetType().GetProperty("SqlState")?.GetValue(current)?.ToString();
            if (string.IsNullOrWhiteSpace(sqlState)) continue;
            var constraint = current.GetType().GetProperty("ConstraintName")?.GetValue(current)?.ToString();
            return new RegistrationDatabaseFailure(sqlState, constraint, sqlState switch
            {
                "23505" => "Duplicate",
                "23503" => "ForeignKeyViolation",
                "23502" => "RequiredField",
                "42P01" => "MissingTable",
                "42703" => "MissingColumn",
                "42501" => "Permission",
                "28P01" => "Authentication",
                "3D000" => "MissingDatabase",
                "57014" => "Timeout",
                _ => "Database"
            });
        }
        return new RegistrationDatabaseFailure(null, null,
            exception is TimeoutException or OperationCanceledException ? "Timeout" : "Unavailable");
    }
}

public sealed record RegistrationDatabaseFailure(string? SqlState, string? Constraint, string Category)
{
    public string ToPublicMessage(string correlationId)
    {
        if (SqlState == "23505" && Constraint?.Contains("email", StringComparison.OrdinalIgnoreCase) == true)
            return "Já existe uma conta com este e-mail. Entre ou recupere seu acesso.";
        if (SqlState == "23505" && Constraint?.Contains("document", StringComparison.OrdinalIgnoreCase) == true)
            return "Já existe uma conta vinculada a este CPF ou CNPJ. Entre ou recupere seu acesso.";
        return $"Não conseguimos concluir seu cadastro. Nenhum dado foi salvo. Tente novamente. Caso o problema continue, informe o código {correlationId} ao suporte.";
    }
}

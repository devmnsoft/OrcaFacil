namespace OrcaFacil.Web.Extensions;

public static class DatabaseExceptionExtensions
{
    public const string InvalidPasswordSqlState = "28P01";

    public static bool IsPostgresInvalidPassword(this Exception exception) =>
        exception.FindPostgresSqlState(InvalidPasswordSqlState) is not null;

    public static Exception? FindPostgresSqlState(this Exception exception, string sqlState)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            var value = current.GetType().GetProperty("SqlState")?.GetValue(current)?.ToString();
            if (string.Equals(value, sqlState, StringComparison.OrdinalIgnoreCase)) return current;
        }
        return null;
    }
}

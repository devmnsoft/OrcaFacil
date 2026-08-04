namespace OrcaFacil.Application.Common;

public sealed record FieldError(string Field, string Message);

public record OperationResult(bool Succeeded, string? Code = null, string? Message = null,
    IReadOnlyList<FieldError>? Errors = null)
{
    public static OperationResult Success(string? message = null) => new(true, Message: message);
    public static OperationResult Failure(string code, string message, params FieldError[] errors) =>
        new(false, code, message, errors);
}

public sealed record OperationResult<T>(bool Succeeded, T? Value = default, string? Code = null,
    string? Message = null, IReadOnlyList<FieldError>? Errors = null)
{
    public static OperationResult<T> Success(T value, string? message = null) => new(true, value, Message: message);
    public static OperationResult<T> Failure(string code, string message, params FieldError[] errors) =>
        new(false, default, code, message, errors);
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
}

public sealed record NextActionDescriptor(string Code, string Title, string Description, string Page,
    IReadOnlyDictionary<string, string>? RouteValues = null, string Icon = "arrow-right", string Tone = "primary");

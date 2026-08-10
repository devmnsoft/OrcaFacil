namespace OrcaFacil.Shared;
public sealed record Result(bool Succeeded, string? Error=null){ public static Result Ok()=>new(true); public static Result Fail(string e)=>new(false,e); }
public sealed record Result<T>(bool Succeeded, T? Value, string? Error)
{
    public object? Message;

    public Result(bool succeeded) : this(succeeded, default, null) { }
    public Result(bool succeeded, string? error) : this(succeeded, default, error) { }

    public static Result<T> Ok(T v) => new(true, v, null);
    public static Result<T> Fail(string e) => new(false, default, e);
}
public static class AppConstants { public const string CompanyName="MNSOFT"; public const string CompanyCnpj="18.160.057/0001-13"; public const string CommercialEmail="comercial@mnsoft.com.br"; }

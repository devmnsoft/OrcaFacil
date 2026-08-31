using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Api;

public static class PublicApiV1
{
    private static readonly string[] Events = ["client.created", "client.updated", "quote.created", "quote.sent", "quote.viewed", "quote.approved", "quote.rejected", "quote.change_requested", "work_order.created", "work_order.started", "work_order.completed", "payment.registered", "receipt.issued", "contract.created", "contract.renewed", "support_ticket.created", "support_ticket.updated", "partner_quote.submitted", "portal.document_downloaded"];

    public static IEndpointRouteBuilder MapPublicApiV1(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1").RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { AuthenticationSchemes = ApiKeyAuthenticationHandler.Scheme }).RequireRateLimiting("api");
        api.AddEndpointFilter<ApiScopeFilter>();

        api.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(new { accountId = user.FindFirstValue("account_id"), keyPrefix = user.FindFirstValue("api_key_prefix"), scopes = user.FindAll("scope").Select(x => x.Value) })).WithMetadata(new RequiredScope("profile.read"));
        api.MapGet("/clients", async (int? page, int? pageSize, string? query, ClaimsPrincipal user, OrcaFacilDbContext db, CancellationToken ct) =>
        {
            var accountId = Account(user); var number = Math.Max(page ?? 1, 1); var size = Math.Clamp(pageSize ?? 20, 1, 100);
            var source = db.Clients.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted);
            if (!string.IsNullOrWhiteSpace(query)) { var pattern = $"%{query.Trim()}%"; source = source.Where(x => EF.Functions.ILike(x.Name, pattern) || (x.Email != null && EF.Functions.ILike(x.Email, pattern))); }
            var total = await source.CountAsync(ct); var items = await source.OrderBy(x => x.Name).Skip((number - 1) * size).Take(size).Select(x => new { x.Id, x.Name, x.TradeName, x.Email, x.Phone, x.City, x.IsActive, x.CreatedAt, x.UpdatedAt }).ToListAsync(ct);
            return Results.Ok(Page(items, number, size, total));
        }).WithMetadata(new RequiredScope("clients.read"));
        api.MapGet("/clients/{id:guid}", async (Guid id, ClaimsPrincipal user, OrcaFacilDbContext db, CancellationToken ct) =>
            await db.Clients.AsNoTracking().Where(x => x.Id == id && x.AccountId == Account(user) && !x.IsDeleted).Select(x => new { x.Id, x.Name, x.TradeName, x.Email, x.Phone, x.City, x.Address, x.IsActive, x.CreatedAt, x.UpdatedAt }).SingleOrDefaultAsync(ct) is { } item ? Results.Ok(item) : ApiError(404, "not_found", "Cliente não encontrado."))
            .WithMetadata(new RequiredScope("clients.read"));
        api.MapPost("/clients", CreateClient).WithMetadata(new RequiredScope("clients.write"));
        api.MapPatch("/clients/{id:guid}", UpdateClient).WithMetadata(new RequiredScope("clients.write"));

        api.MapGet("/services", async (int? page, int? pageSize, ClaimsPrincipal user, OrcaFacilDbContext db, CancellationToken ct) =>
        { var n=Math.Max(page??1,1);var s=Math.Clamp(pageSize??20,1,100);var q=db.ServiceCatalogItems.AsNoTracking().Where(x=>x.AccountId==Account(user)&&x.IsActive&&!x.IsDeleted);var total=await q.CountAsync(ct);var items=await q.OrderBy(x=>x.Name).Skip((n-1)*s).Take(s).Select(x=>new{x.Id,x.Code,x.Name,x.Description,unit=x.UnitCode,price=x.StandardPrice,x.SuggestedDurationMinutes}).ToListAsync(ct);return Results.Ok(Page(items,n,s,total)); }).WithMetadata(new RequiredScope("services.read"));
        api.MapGet("/services/{id:guid}", async (Guid id, ClaimsPrincipal user, OrcaFacilDbContext db, CancellationToken ct) => await db.ServiceCatalogItems.AsNoTracking().Where(x=>x.Id==id&&x.AccountId==Account(user)&&!x.IsDeleted).Select(x=>new{x.Id,x.Code,x.Name,x.Description,unit=x.UnitCode,price=x.StandardPrice,x.SuggestedDurationMinutes}).SingleOrDefaultAsync(ct) is {} item?Results.Ok(item):ApiError(404,"not_found","Serviço não encontrado.")).WithMetadata(new RequiredScope("services.read"));
        api.MapGet("/webhooks/events", () => Results.Ok(new { items = Events })).WithMetadata(new RequiredScope("webhooks.read"));
        return endpoints;
    }

    private static async Task<IResult> CreateClient(CreateClientRequest request, HttpContext http, ClaimsPrincipal user, OrcaFacilDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200) return ApiError(400,"validation_error","Revise os campos informados.",new[]{new { field="name",message="Nome é obrigatório e deve ter até 200 caracteres." }});
        if (!http.Request.Headers.TryGetValue("Idempotency-Key", out var header) || header.ToString().Length is < 8 or > 200) return ApiError(400,"validation_error","Idempotency-Key é obrigatório (8 a 200 caracteres).");
        var accountId=Account(user);var keyId=RequiredClaimGuid(user, "api_key_id");var keyHash=Hash(header.ToString());var requestHash=Hash(System.Text.Json.JsonSerializer.Serialize(request));
        var previous=await db.ApiIdempotencyKeys.AsNoTracking().SingleOrDefaultAsync(x=>x.AccountId==accountId&&x.ApiKeyId==keyId&&x.KeyHash==keyHash&&x.ExpiresAt>DateTime.UtcNow,ct);
        if(previous is not null)return previous.RequestHash==requestHash?Results.Content(previous.ResponseJson,"application/json",statusCode:previous.ResponseStatusCode):ApiError(409,"idempotency_conflict","A chave de idempotência já foi usada com outro conteúdo.");
        var client=new Client{AccountId=accountId,UserId=keyId,Name=request.Name.Trim(),Email=Clean(request.Email,254),Phone=Clean(request.Phone,30),City=Clean(request.City,120),Address=Clean(request.Address,300)};db.Clients.Add(client);var json=System.Text.Json.JsonSerializer.Serialize(new{client.Id,client.Name,client.Email,client.Phone,client.City,client.CreatedAt});db.ApiIdempotencyKeys.Add(new ApiIdempotencyKey{AccountId=accountId,ApiKeyId=keyId,KeyHash=keyHash,RequestHash=requestHash,ResponseStatusCode=201,ResponseJson=json,ExpiresAt=DateTime.UtcNow.AddHours(24)});await db.SaveChangesAsync(ct);return Results.Content(json,"application/json",statusCode:201);
    }
    private static async Task<IResult> UpdateClient(Guid id, UpdateClientRequest request, ClaimsPrincipal user, OrcaFacilDbContext db, CancellationToken ct){var item=await db.Clients.SingleOrDefaultAsync(x=>x.Id==id&&x.AccountId==Account(user)&&!x.IsDeleted,ct);if(item is null)return ApiError(404,"not_found","Cliente não encontrado.");if(string.IsNullOrWhiteSpace(request.Name))return ApiError(400,"validation_error","Nome é obrigatório.");item.Name=request.Name.Trim();item.Email=Clean(request.Email,254);item.Phone=Clean(request.Phone,30);item.City=Clean(request.City,120);item.Touch();await db.SaveChangesAsync(ct);return Results.Ok(new{item.Id,item.Name,item.Email,item.Phone,item.City,item.UpdatedAt});}
    private static Guid Account(ClaimsPrincipal user) => RequiredClaimGuid(user, "account_id");
    private static Guid RequiredClaimGuid(ClaimsPrincipal user, string claim) =>
        Guid.TryParse(user.FindFirstValue(claim), out var value) ? value : throw new AuthenticationFailureException($"Claim obrigatória inválida: {claim}.");
    private static string Hash(string value)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static string? Clean(string? value,int max)=>string.IsNullOrWhiteSpace(value)?null:value.Trim()[..Math.Min(value.Trim().Length,max)];
    private static object Page<T>(IReadOnlyList<T> items,int page,int size,int total)=>new{items,page,pageSize=size,totalItems=total,totalPages=(int)Math.Ceiling(total/(double)size)};
    private static IResult ApiError(int status,string code,string message,object[]? details=null)=>Results.Json(new{error=new{code,message,correlationId=System.Diagnostics.Activity.Current?.Id??Guid.NewGuid().ToString("N"),details=details??Array.Empty<object>()}},statusCode:status);
}

public sealed record CreateClientRequest(string Name,string? Email,string? Phone,string? City,string? Address);
public sealed record UpdateClientRequest(string Name,string? Email,string? Phone,string? City);
public sealed record RequiredScope(string Name);

public sealed class ApiScopeFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var required=context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<RequiredScope>()?.Name;
        if(required is null||required=="profile.read"||context.HttpContext.User.FindAll("scope").Any(x=>x.Value==required))return await next(context);
        return Results.Json(new{error=new{code="scope_required",message=$"O escopo {required} é obrigatório.",correlationId=context.HttpContext.TraceIdentifier,details=Array.Empty<object>()}},statusCode:403);
    }
}

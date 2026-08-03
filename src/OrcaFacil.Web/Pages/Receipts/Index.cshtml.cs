using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrcaFacil.Application.Receipts;
using OrcaFacil.Domain.Enums;
using OrcaFacil.Web.ViewModels.Receipts;

namespace OrcaFacil.Web.Pages.Receipts;
[Authorize]
public sealed class IndexModel(IReceiptQueryService receipts) : PageModel
{
    [BindProperty(SupportsGet=true)] public DateTime? From { get; set; } [BindProperty(SupportsGet=true)] public DateTime? To { get; set; }
    [BindProperty(SupportsGet=true)] public Guid? ClientId { get; set; } [BindProperty(SupportsGet=true)] public string? PaymentMethod { get; set; }
    [BindProperty(SupportsGet=true)] public ReceiptOriginType? OriginType { get; set; } [BindProperty(SupportsGet=true)] public string? Status { get; set; }
    [BindProperty(SupportsGet=true)] public decimal? MinimumAmount { get; set; } [BindProperty(SupportsGet=true)] public decimal? MaximumAmount { get; set; }
    [BindProperty(SupportsGet=true)] public string Sort { get; set; } = "recent"; [BindProperty(SupportsGet=true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet=true)] public int PageSize { get; set; } = 25;
    public ReceiptIndexFilterState Filters { get; private set; } = new(); public ReceiptListResult Result { get; private set; } = new([],0,1,25,1,0,0,0,0);
    public IReadOnlyList<ReceiptListItem> Receipts => Result.Items; public int TotalPages => Result.TotalPages;
    public async Task<IActionResult> OnGetAsync(CancellationToken ct) { Filters = new(){From=From,To=To,ClientId=ClientId,PaymentMethod=PaymentMethod,OriginType=OriginType,Status=Status,MinimumAmount=MinimumAmount,MaximumAmount=MaximumAmount,Sort=Sort,PageNumber=Math.Max(1,PageNumber),PageSize=Math.Clamp(PageSize,10,100)}; var result=await receipts.ListAsync(new(From,To,ClientId,PaymentMethod,OriginType,Status,MinimumAmount,MaximumAmount,Sort,Filters.PageNumber,Filters.PageSize),ct); if(result is null)return Forbid(); Result=result;PageNumber=result.Page;PageSize=result.PageSize;Filters=Filters.WithPage(result.Page);return Page(); }
    public IReadOnlyDictionary<string,string> GetRouteValues(int pageNumber) { var values=new Dictionary<string,string>{{"PageNumber",Math.Max(1,pageNumber).ToString(CultureInfo.InvariantCulture)},{"PageSize",Filters.PageSize.ToString(CultureInfo.InvariantCulture)},{"Sort",Filters.Sort}}; Add("From",Filters.From?.ToString("yyyy-MM-dd"));Add("To",Filters.To?.ToString("yyyy-MM-dd"));Add("ClientId",Filters.ClientId?.ToString());Add("PaymentMethod",Filters.PaymentMethod);Add("OriginType",Filters.OriginType?.ToString());Add("Status",Filters.Status);Add("MinimumAmount",Filters.MinimumAmount?.ToString(CultureInfo.InvariantCulture));Add("MaximumAmount",Filters.MaximumAmount?.ToString(CultureInfo.InvariantCulture));return values; void Add(string key,string? value){if(!string.IsNullOrWhiteSpace(value))values[key]=value;} }
}
file static class ReceiptFilterExtensions { public static ReceiptIndexFilterState WithPage(this ReceiptIndexFilterState x,int page)=>new(){From=x.From,To=x.To,ClientId=x.ClientId,PaymentMethod=x.PaymentMethod,OriginType=x.OriginType,Status=x.Status,MinimumAmount=x.MinimumAmount,MaximumAmount=x.MaximumAmount,Sort=x.Sort,PageNumber=page,PageSize=x.PageSize}; }

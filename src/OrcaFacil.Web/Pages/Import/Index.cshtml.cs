using System.Globalization;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.Abstractions;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Pages.Import;

[Authorize]
[RequestSizeLimit(5 * 1024 * 1024)]
public sealed class IndexModel(OrcaFacilDbContext db, ICurrentAccountService account) : PageModel
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    [BindProperty] public IFormFile? File { get; set; }
    [BindProperty] public DataImportType Type { get; set; }
    public IReadOnlyList<DataImport> Recent { get; private set; } = [];
    public DataImport? Preview { get; private set; }
    public IReadOnlyList<ImportRow> Rows { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken ct)
    {
        if (account.AccountId is not { } accountId) return Forbid();
        Recent = await db.DataImports.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt).Take(8).ToListAsync(ct);
        if (id.HasValue)
        {
            Preview = Recent.FirstOrDefault(x => x.Id == id) ?? await db.DataImports.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.AccountId == accountId && !x.IsDeleted, ct);
            if (Preview is not null && Preview.Status == DataImportStatus.ReadyToImport)
                Rows = JsonSerializer.Deserialize<List<ImportRow>>(Preview.StagedRowsJson ?? "[]", JsonOptions) ?? [];
        }
        return Page();
    }

    public async Task<IActionResult> OnPostUploadAsync(CancellationToken ct)
    {
        if (account.AccountId is not { } accountId) return Forbid();
        var userId = account.UserId;
        if (File is null || File.Length == 0) ModelState.AddModelError(nameof(File), "Selecione um arquivo CSV.");
        else if (File.Length > 5 * 1024 * 1024) ModelState.AddModelError(nameof(File), "O arquivo deve ter no máximo 5 MB.");
        else if (!string.Equals(Path.GetExtension(File.FileName), ".csv", StringComparison.OrdinalIgnoreCase)) ModelState.AddModelError(nameof(File), "Envie somente CSV UTF-8.");
        if (!ModelState.IsValid) { await OnGetAsync(null, ct); return Page(); }

        List<ImportRow> rows;
        await using (var stream = File!.OpenReadStream())
        using (var reader = new StreamReader(stream, new UTF8Encoding(false, true), true))
        {
            try { rows = Parse(await reader.ReadToEndAsync(ct), Type); }
            catch (Exception ex) when (ex is DecoderFallbackException or InvalidDataException) { ModelState.AddModelError(nameof(File), ex.Message); await OnGetAsync(null, ct); return Page(); }
        }
        await DetectDuplicates(rows, Type, accountId, ct);
        var import = new DataImport { AccountId = accountId, UploadedByUserId = userId, Type = Type, FileName = Path.GetFileName(File.FileName), Status = DataImportStatus.ReadyToImport, TotalRows = rows.Count, FailedRows = rows.Count(x => x.Errors.Count > 0), Summary = "Arquivo validado; aguardando confirmação.", StagedRowsJson = JsonSerializer.Serialize(rows, JsonOptions), ErrorsJson = JsonSerializer.Serialize(rows.Where(x => x.Errors.Count > 0), JsonOptions) };
        db.Add(import); db.ActivityEvents.Add(new ActivityEvent { AccountId = accountId, ActorUserId = userId, Action = "DATA_IMPORT_VALIDATED", EntityType = nameof(DataImport), EntityId = import.Id, Summary = $"{Type}: {rows.Count} linhas; {import.FailedRows} com impedimento." });
        await db.SaveChangesAsync(ct); return RedirectToPage(new { id = import.Id });
    }

    public async Task<IActionResult> OnPostConfirmAsync(Guid id, CancellationToken ct)
    {
        if (account.AccountId is not { } accountId) return Forbid();
        var userId = account.UserId;
        var import = await db.DataImports.SingleOrDefaultAsync(x => x.Id == id && x.AccountId == accountId && !x.IsDeleted, ct);
        if (import is null) return NotFound();
        if (import.Status != DataImportStatus.ReadyToImport) return BadRequest();
        var rows = JsonSerializer.Deserialize<List<ImportRow>>(import.StagedRowsJson ?? "[]", JsonOptions) ?? [];
        var valid = rows.Where(x => x.Errors.Count == 0).ToList();
        foreach (var row in valid)
        {
            if (import.Type == DataImportType.Clients) db.Clients.Add(new Client { AccountId = accountId, UserId = userId, Name = row.Value("name")!, TradeName = row.Value("company"), DocumentNumber = Digits(row.Value("document")), Email = row.Value("email"), Phone = Digits(row.Value("phone") ?? row.Value("whatsapp")), Address = row.Value("address"), City = row.Value("city"), Notes = JoinNotes(row), IsActive = !IsInactive(row.Value("status")) });
            else db.ServiceCatalogItems.Add(new ServiceCatalogItem { AccountId = accountId, Name = row.Value("name")!, Description = row.Value("description"), UnitCode = row.Value("unit") ?? "service", StandardPrice = Money(row.Value("price")), EstimatedCost = Money(row.Value("cost")), DesiredMarginPercentage = Money(row.Value("margin")), DefaultDeliveryTerm = row.Value("deadline"), DefaultNotes = row.Value("notes"), Tags = row.Value("tags"), IsActive = !IsInactive(row.Value("active")) });
        }
        import.ImportedRows = valid.Count; import.SkippedRows = rows.Count - valid.Count; import.FailedRows = import.SkippedRows; import.Status = DataImportStatus.Imported; import.CompletedAt = DateTime.UtcNow; import.Summary = $"{valid.Count} registros criados; {import.SkippedRows} linhas não importadas."; import.StagedRowsJson = null;
        db.ActivityEvents.Add(new ActivityEvent { AccountId = accountId, ActorUserId = userId, Action = "DATA_IMPORT_COMPLETED", EntityType = nameof(DataImport), EntityId = import.Id, Summary = import.Summary });
        await db.SaveChangesAsync(ct); TempData["Success"] = import.Summary; return RedirectToPage(new { id });
    }

    public IActionResult OnGetTemplate(DataImportType type)
    {
        var header = type == DataImportType.Clients ? "nome,empresa,documento,email,telefone,whatsapp,endereco,cidade,estado,cep,observacoes,origem,status\n" : "nome,descricao,categoria,unidade,preco_padrao,custo_estimado,margem_desejada,prazo_padrao,observacoes,ativo,tags\n";
        return File(new UTF8Encoding(true).GetBytes(header), "text/csv; charset=utf-8", $"modelo-{type.ToString().ToLowerInvariant()}.csv");
    }

    public async Task<IActionResult> OnGetErrorsAsync(Guid id, CancellationToken ct)
    {
        if (account.AccountId is not { } accountId) return Forbid();
        var item = await db.DataImports.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.AccountId == accountId && !x.IsDeleted, ct); if (item is null) return NotFound();
        var errors = JsonSerializer.Deserialize<List<ImportRow>>(item.ErrorsJson ?? "[]", JsonOptions) ?? [];
        var csv = new StringBuilder("linha,campo,valor,mensagem,sugestao\n"); foreach (var row in errors) foreach (var error in row.Errors) csv.AppendLine($"{row.Line},dados,,{Quote(error)},{Quote("Corrija a linha e importe novamente")}");
        return File(new UTF8Encoding(true).GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"erros-importacao-{id:N}.csv");
    }

    private async Task DetectDuplicates(List<ImportRow> rows, DataImportType type, Guid accountId, CancellationToken ct)
    {
        if (type == DataImportType.Clients) { var existing = await db.Clients.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted).Select(x => new { x.Email, x.Phone, x.DocumentNumber }).ToListAsync(ct); foreach (var r in rows) if (existing.Any(x => Eq(x.Email,r.Value("email")) || Eq(Digits(x.Phone),Digits(r.Value("phone"))) || Eq(Digits(x.DocumentNumber),Digits(r.Value("document"))))) r.Errors.Add("Possível duplicidade nesta conta."); }
        else { var names = await db.ServiceCatalogItems.AsNoTracking().Where(x => x.AccountId == accountId && !x.IsDeleted).Select(x => x.Name.ToUpper()).ToListAsync(ct); foreach (var r in rows) if (names.Contains((r.Value("name") ?? "").ToUpperInvariant())) r.Errors.Add("Serviço com este nome já existe nesta conta."); }
    }

    private static List<ImportRow> Parse(string csv, DataImportType type)
    {
        var lines = Csv(csv); if (lines.Count < 2) throw new InvalidDataException("O CSV precisa ter cabeçalho e ao menos uma linha.");
        var aliases = type == DataImportType.Clients ? ClientAliases : ServiceAliases; var headers = lines[0].Select(Normalize).Select(x => aliases.GetValueOrDefault(x, x)).ToArray();
        if (!headers.Contains("name")) throw new InvalidDataException("Mapeie uma coluna de nome (nome, cliente ou serviço).");
        var result = new List<ImportRow>(); for (var i=1;i<lines.Count;i++) { if(lines[i].All(string.IsNullOrWhiteSpace)) continue; var values = new Dictionary<string,string?>(); for(var c=0;c<headers.Length;c++) values[headers[c]] = c < lines[i].Count ? lines[i][c].Trim() : null; var row=new ImportRow(i+1,values,[]); if(string.IsNullOrWhiteSpace(row.Value("name"))) row.Errors.Add("Nome é obrigatório."); if(type==DataImportType.Clients && !ValidEmail(row.Value("email"))) row.Errors.Add("E-mail inválido."); if(type==DataImportType.Services && !TryMoney(row.Value("price"))) row.Errors.Add("Preço padrão inválido."); result.Add(row); } return result;
    }
    private static List<List<string>> Csv(string input) { var rows=new List<List<string>>(); var row=new List<string>(); var cell=new StringBuilder(); var quoted=false; for(var i=0;i<input.Length;i++){var ch=input[i];if(ch=='"'){if(quoted&&i+1<input.Length&&input[i+1]=='"'){cell.Append('"');i++;}else quoted=!quoted;}else if((ch==','||ch==';')&&!quoted){row.Add(cell.ToString());cell.Clear();}else if((ch=='\n'||ch=='\r')&&!quoted){if(ch=='\r'&&i+1<input.Length&&input[i+1]=='\n')i++;row.Add(cell.ToString());cell.Clear();rows.Add(row);row=[];}else cell.Append(ch);}if(cell.Length>0||row.Count>0){row.Add(cell.ToString());rows.Add(row);}return rows; }
    private static string Normalize(string value) => new(value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c)).ToArray());
    private static bool ValidEmail(string? value) { if(string.IsNullOrWhiteSpace(value)) return true; try { return new MailAddress(value).Address == value; } catch { return false; } }
    private static bool TryMoney(string? value) => string.IsNullOrWhiteSpace(value) || decimal.TryParse(value, NumberStyles.Currency, CultureInfo.GetCultureInfo("pt-BR"), out _) || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _);
    private static decimal Money(string? value) => decimal.TryParse(value, NumberStyles.Currency, CultureInfo.GetCultureInfo("pt-BR"), out var x) || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out x) ? x : 0;
    private static string? Digits(string? value) { var x = string.Concat((value ?? "").Where(char.IsDigit)); return x.Length == 0 ? null : x; }
    private static bool Eq(string? a,string? b)=>!string.IsNullOrWhiteSpace(a)&&!string.IsNullOrWhiteSpace(b)&&string.Equals(a.Trim(),b.Trim(),StringComparison.OrdinalIgnoreCase);
    private static bool IsInactive(string? x)=>string.Equals(x,"inativo",StringComparison.OrdinalIgnoreCase)||string.Equals(x,"não",StringComparison.OrdinalIgnoreCase)||x=="0";
    private static string? JoinNotes(ImportRow r) { var parts=new[]{r.Value("notes"),r.Value("origin") is {Length:>0} o?$"Origem: {o}":null,r.Value("state") is {Length:>0} s?$"UF: {s}":null,r.Value("zip") is {Length:>0} z?$"CEP: {z}":null}; var value=string.Join(" | ",parts.Where(x=>!string.IsNullOrWhiteSpace(x)));return value.Length==0?null:value; }
    private static string Quote(string x)=>$"\"{x.Replace("\"","\"\"")}\"";
    private static readonly Dictionary<string,string> ClientAliases=new(){{"nome","name"},{"cliente","name"},{"empresa","company"},{"documento","document"},{"cpf","document"},{"cnpj","document"},{"email","email"},{"telefone","phone"},{"celular","phone"},{"whatsapp","whatsapp"},{"endereco","address"},{"cidade","city"},{"estado","state"},{"uf","state"},{"cep","zip"},{"observacoes","notes"},{"origem","origin"},{"status","status"}};
    private static readonly Dictionary<string,string> ServiceAliases=new(){{"nome","name"},{"servico","name"},{"produtoservico","name"},{"descricao","description"},{"categoria","category"},{"unidade","unit"},{"precopadrao","price"},{"preco","price"},{"valor","price"},{"custoestimado","cost"},{"margemdesejada","margin"},{"prazopadrao","deadline"},{"observacoes","notes"},{"ativo","active"},{"tags","tags"}};
}

public sealed record ImportRow(int Line, Dictionary<string,string?> Values, List<string> Errors) { public string? Value(string key)=>Values.GetValueOrDefault(key); }

using System.Globalization;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

namespace OrcaFacil.Application.DataGovernance;

public enum QualitySeverity { Info, Low, Medium, High, Critical }
public enum QualityFindingStatus { Open, InReview, Fixed, Ignored, FalsePositive, Blocked }
public enum NormalizationKind { Trim, Email, Document, Phone, PostalCode, State }

public sealed record GovernedRecord(Guid AccountId, Guid Id, string Entity, IReadOnlyDictionary<string, string?> Values);
public sealed record QualityRule(string Code, string Name, string Entity, string Field, QualitySeverity Severity, bool Active, bool BlocksFlow, string Recommendation);
public sealed record QualityFinding(Guid AccountId, Guid RecordId, string Entity, string RuleCode, QualitySeverity Severity, string Message, string Recommendation);
public sealed record QualityScore(int Value, string Classification, int Evaluated, int WeightedIssues, IReadOnlyDictionary<QualitySeverity, int> Issues)
{
    public static QualityScore Empty { get; } = new(100, "Ótimo", 0, 0, new Dictionary<QualitySeverity, int>());
}

public sealed class DataQualityRuleService
{
    public IReadOnlyList<QualityRule> InitialRules { get; } =
    [
        new("CLIENT_DOCUMENT_REQUIRED", "Cliente sem documento", "Client", "document", QualitySeverity.High, true, false, "Informar CPF ou CNPJ válido."),
        new("CLIENT_EMAIL_REQUIRED", "Cliente sem e-mail", "Client", "email", QualitySeverity.Medium, true, false, "Informar o e-mail principal."),
        new("CLIENT_EMAIL_VALID", "Cliente com e-mail inválido", "Client", "email", QualitySeverity.High, true, false, "Revisar o formato do e-mail."),
        new("CLIENT_PHONE_REQUIRED", "Cliente sem telefone", "Client", "phone", QualitySeverity.Medium, true, false, "Informar o telefone principal."),
        new("CLIENT_ADDRESS_REQUIRED", "Cliente sem endereço fiscal", "Client", "address", QualitySeverity.High, true, true, "Completar o endereço fiscal."),
        new("SERVICE_CATEGORY_REQUIRED", "Serviço sem categoria", "Service", "category", QualitySeverity.Medium, true, false, "Selecionar uma categoria."),
        new("SERVICE_PRICE_REQUIRED", "Serviço sem preço", "Service", "price", QualitySeverity.High, true, true, "Cadastrar um preço válido."),
        new("ASSET_CLIENT_REQUIRED", "Ativo sem cliente", "Asset", "client", QualitySeverity.High, true, false, "Vincular um cliente existente."),
        new("WORK_ORDER_ADDRESS_REQUIRED", "OS sem endereço", "WorkOrder", "address", QualitySeverity.High, true, true, "Completar o endereço de atendimento."),
        new("PAYMENT_ORIGIN_REQUIRED", "Pagamento sem origem", "Payment", "origin", QualitySeverity.Critical, true, true, "Vincular a origem ou justificar a origem manual.")
    ];

    public QualityRule CreateVersion(QualityRule current, QualityRule update, bool isSuperAdmin)
    {
        if (string.IsNullOrWhiteSpace(update.Code) || string.IsNullOrWhiteSpace(update.Name))
            throw new ArgumentException("Código e nome da regra são obrigatórios.");
        if (update.Code.StartsWith("GLOBAL_", StringComparison.OrdinalIgnoreCase) && !isSuperAdmin)
            throw new UnauthorizedAccessException("Somente SuperAdmin pode alterar regras globais.");
        return update with { Code = current.Code };
    }
}

public sealed class DataQualityEngine
{
    public IReadOnlyList<QualityFinding> Evaluate(Guid accountId, IEnumerable<GovernedRecord> records, IEnumerable<QualityRule> rules)
    {
        if (accountId == Guid.Empty) throw new ArgumentException("A conta é obrigatória.", nameof(accountId));
        var scoped = records.Where(x => x.AccountId == accountId).ToArray();
        var findings = new List<QualityFinding>();
        foreach (var record in scoped)
        foreach (var rule in rules.Where(x => x.Active && string.Equals(x.Entity, record.Entity, StringComparison.OrdinalIgnoreCase)))
        {
            record.Values.TryGetValue(rule.Field, out var value);
            var invalid = string.IsNullOrWhiteSpace(value);
            if (rule.Code.EndsWith("EMAIL_VALID", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(value)) invalid = !IsEmail(value);
            if (rule.Code.EndsWith("PRICE_REQUIRED", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(value)) invalid = !decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price <= 0;
            if (invalid) findings.Add(new(accountId, record.Id, record.Entity, rule.Code, rule.Severity, rule.Name, rule.Recommendation));
        }
        return findings;
    }

    private static bool IsEmail(string value) { try { return new MailAddress(value).Address == value; } catch (FormatException) { return false; } }
}

public sealed class DataQualityCheckService(DataQualityEngine engine)
{
    public IReadOnlyList<QualityFinding> Run(Guid accountId, IEnumerable<GovernedRecord> records, IEnumerable<QualityRule> rules) => engine.Evaluate(accountId, records, rules);
}

public sealed class DataQualityFindingService
{
    public QualityFindingStatus Transition(QualityFindingStatus current, QualityFindingStatus target, string? reason)
    {
        if ((target is QualityFindingStatus.Fixed or QualityFindingStatus.Ignored or QualityFindingStatus.FalsePositive) && string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Informe o motivo da resolução.", nameof(reason));
        if (current == QualityFindingStatus.Fixed && target == QualityFindingStatus.Ignored)
            throw new InvalidOperationException("Um achado corrigido precisa ser reaberto antes de ser ignorado.");
        return target;
    }
}

public sealed class DataQualityScoreService
{
    private static readonly IReadOnlyDictionary<QualitySeverity, int> Weights = new Dictionary<QualitySeverity, int>
    { [QualitySeverity.Info] = 1, [QualitySeverity.Low] = 2, [QualitySeverity.Medium] = 5, [QualitySeverity.High] = 10, [QualitySeverity.Critical] = 20 };

    public QualityScore Calculate(int evaluatedRecords, IEnumerable<QualityFinding> findings)
    {
        if (evaluatedRecords < 0) throw new ArgumentOutOfRangeException(nameof(evaluatedRecords));
        var counts = findings.GroupBy(x => x.Severity).ToDictionary(x => x.Key, x => x.Count());
        if (evaluatedRecords == 0) return QualityScore.Empty;
        var penalty = counts.Sum(x => Weights[x.Key] * x.Value);
        var score = Math.Clamp(100 - (int)Math.Round(penalty * 100m / (evaluatedRecords * 20m)), 0, 100);
        return new(score, score >= 90 ? "Ótimo" : score >= 75 ? "Bom" : score >= 50 ? "Atenção" : "Crítico", evaluatedRecords, penalty, counts);
    }
}

public sealed record DuplicateCandidate(Guid AccountId, Guid LeftId, Guid RightId, int Similarity, IReadOnlyList<string> MatchedFields);

public sealed class DuplicateDetectionService
{
    public IReadOnlyList<DuplicateCandidate> Detect(Guid accountId, IEnumerable<GovernedRecord> records)
    {
        var list = records.Where(x => x.AccountId == accountId).ToArray();
        var result = new List<DuplicateCandidate>();
        for (var left = 0; left < list.Length; left++) for (var right = left + 1; right < list.Length; right++)
        {
            var matches = new List<string>();
            Match(list[left], list[right], "document", NormalizeDigits, matches);
            Match(list[left], list[right], "email", x => x.Trim().ToLowerInvariant(), matches);
            Match(list[left], list[right], "phone", NormalizeDigits, matches);
            var nameSimilarity = Similarity(Value(list[left], "name"), Value(list[right], "name"));
            if (nameSimilarity >= 85) matches.Add("name");
            var score = Math.Min(100, matches.Sum(x => x switch { "document" => 100, "email" => 90, "phone" => 75, _ => nameSimilarity }) / Math.Max(1, matches.Count));
            if (matches.Count > 0 && (matches.Any(x => x is "document" or "email") || score >= 85)) result.Add(new(accountId, list[left].Id, list[right].Id, score, matches));
        }
        return result;
    }

    private static void Match(GovernedRecord left, GovernedRecord right, string field, Func<string, string> normalize, ICollection<string> matches)
    { var a = Value(left, field); var b = Value(right, field); if (!string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b) && normalize(a) == normalize(b)) matches.Add(field); }
    private static string? Value(GovernedRecord row, string key) => row.Values.TryGetValue(key, out var value) ? value : null;
    private static string NormalizeDigits(string value) => string.Concat(value.Where(char.IsDigit));
    private static int Similarity(string? left, string? right)
    {
        var a = NormalizeText(left); var b = NormalizeText(right); if (a.Length == 0 || b.Length == 0) return 0;
        var distance = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) distance[i, 0] = i; for (var j = 0; j <= b.Length; j++) distance[0, j] = j;
        for (var i = 1; i <= a.Length; i++) for (var j = 1; j <= b.Length; j++) distance[i, j] = Math.Min(Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1), distance[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
        return (int)Math.Round((1m - distance[a.Length, b.Length] / (decimal)Math.Max(a.Length, b.Length)) * 100m);
    }
    private static string NormalizeText(string? value) => string.Concat((value ?? "").Normalize(NormalizationForm.FormD).Where(x => CharUnicodeInfo.GetUnicodeCategory(x) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(x))).ToUpperInvariant();
}

public sealed record MergePreview(Guid AccountId, Guid PrimaryId, Guid SecondaryId, IReadOnlyDictionary<string, (string? Primary, string? Secondary)> Conflicts, IReadOnlyDictionary<string, int> RelatedRecords);

public sealed class MasterDataMergeService
{
    public MergePreview Preview(GovernedRecord primary, GovernedRecord secondary, IReadOnlyDictionary<string, int>? related = null)
    {
        EnsureSameTenant(primary, secondary);
        var conflicts = primary.Values.Keys.Union(secondary.Values.Keys).Select(k => (Key: k, Primary: Value(primary, k), Secondary: Value(secondary, k)))
            .Where(x => !string.IsNullOrWhiteSpace(x.Primary) && !string.IsNullOrWhiteSpace(x.Secondary) && !string.Equals(x.Primary, x.Secondary, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(x => x.Key, x => (x.Primary, x.Secondary));
        return new(primary.AccountId, primary.Id, secondary.Id, conflicts, related ?? new Dictionary<string, int>());
    }

    public MergeDecision Confirm(MergePreview preview, string reason, bool hasPermission, bool confirmed)
    {
        if (!hasPermission) throw new UnauthorizedAccessException("A permissão DataQuality.Merge é obrigatória.");
        if (!confirmed) throw new InvalidOperationException("A prévia precisa ser confirmada explicitamente.");
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("O motivo da mesclagem é obrigatório.", nameof(reason));
        return new(preview.AccountId, preview.PrimaryId, preview.SecondaryId, reason.Trim(), DateTime.UtcNow, false);
    }

    private static string? Value(GovernedRecord row, string key) => row.Values.TryGetValue(key, out var value) ? value : null;
    private static void EnsureSameTenant(GovernedRecord left, GovernedRecord right) { if (left.AccountId == Guid.Empty || left.AccountId != right.AccountId) throw new InvalidOperationException("Mesclagem entre contas é proibida."); }
}

public sealed record MergeDecision(Guid AccountId, Guid PrimaryId, Guid SecondaryId, string Reason, DateTime ConfirmedAt, bool PhysicallyDeleted);
public sealed class ClientMergeService(MasterDataMergeService merge) { public MergePreview Preview(GovernedRecord primary, GovernedRecord secondary, IReadOnlyDictionary<string, int> links) => merge.Preview(primary, secondary, links); public MergeDecision Confirm(MergePreview preview, string reason, bool permitted, bool confirmed) => merge.Confirm(preview, reason, permitted, confirmed); }

public sealed record NormalizationPreview(string? Original, string? Normalized, bool Changed, bool Sensitive);
public sealed class DataNormalizationService
{
    public NormalizationPreview Preview(string? value, NormalizationKind kind)
    {
        var normalized = kind switch
        {
            NormalizationKind.Trim => value?.Trim(), NormalizationKind.Email => value?.Trim().ToLowerInvariant(),
            NormalizationKind.Document or NormalizationKind.Phone or NormalizationKind.PostalCode => value is null ? null : string.Concat(value.Where(char.IsDigit)),
            NormalizationKind.State => value?.Trim().ToUpperInvariant(), _ => value
        };
        return new(value, normalized, !string.Equals(value, normalized, StringComparison.Ordinal), kind is NormalizationKind.Document or NormalizationKind.Phone);
    }
}

public sealed record ImportRowPreview(int Number, IReadOnlyDictionary<string, string?> Values, IReadOnlyList<string> Errors);
public sealed record ImportPreview(Guid AccountId, Guid Token, DateTime CreatedAt, IReadOnlyList<ImportRowPreview> Rows) { public bool CanCommit => Rows.Count > 0; }
public sealed record ImportCommitResult(int Imported, int Skipped, IReadOnlyList<ImportRowPreview> InvalidRows);

public sealed class DataImportPreviewService
{
    public ImportPreview Create(Guid accountId, IEnumerable<IReadOnlyDictionary<string, string?>> rows, IReadOnlyCollection<string> requiredColumns)
    {
        if (accountId == Guid.Empty) throw new ArgumentException("A conta é obrigatória.", nameof(accountId));
        var previews = rows.Select((row, index) => new ImportRowPreview(index + 2, row, requiredColumns.Where(x => !row.TryGetValue(x, out var value) || string.IsNullOrWhiteSpace(value)).Select(x => $"Campo obrigatório ausente: {x}.").ToArray())).ToArray();
        return new(accountId, Guid.NewGuid(), DateTime.UtcNow, previews);
    }
}

public sealed class DataImportService(DataImportPreviewService previewService) { public ImportPreview Preview(Guid accountId, IEnumerable<IReadOnlyDictionary<string, string?>> rows, IReadOnlyCollection<string> required) => previewService.Create(accountId, rows, required); }
public sealed class DataImportCommitService
{
    public ImportCommitResult Commit(Guid accountId, ImportPreview? preview, bool confirmed)
    {
        if (preview is null) throw new InvalidOperationException("A importação exige prévia.");
        if (preview.AccountId != accountId) throw new UnauthorizedAccessException("A prévia pertence a outra conta.");
        if (!confirmed) throw new InvalidOperationException("Confirme a importação após revisar a prévia.");
        var invalid = preview.Rows.Where(x => x.Errors.Count > 0).ToArray(); return new(preview.Rows.Count - invalid.Length, invalid.Length, invalid);
    }
}
public sealed class DataImportRollbackService { public bool CanRollback(DateTime committedAt, DateTime? lastChangedAt) => !lastChangedAt.HasValue || lastChangedAt <= committedAt; public void EnsureSafe(DateTime committedAt, DateTime? lastChangedAt) { if (!CanRollback(committedAt, lastChangedAt)) throw new InvalidOperationException("Há dados alterados após a importação; o rollback automático foi bloqueado."); } }
public sealed class DataIntegrityService { public IReadOnlyList<QualityFinding> Check(Guid accountId, IEnumerable<GovernedRecord> records, IEnumerable<QualityRule> constraints) => new DataQualityEngine().Evaluate(accountId, records, constraints); }
public sealed class SensitiveDataChangeReviewService { public string Mask(string? value) { if (string.IsNullOrEmpty(value)) return "—"; var visible = Math.Min(4, value.Length); return new string('•', value.Length - visible) + value[^visible..]; } public void EnsureBulkPermission(int count, bool permitted) { if (count > 1 && !permitted) throw new UnauthorizedAccessException("Alterações sensíveis em massa exigem permissão."); } }
public sealed class ModuleDataQualityService(DataQualityScoreService scoreService) { public QualityScore Calculate(int records, IEnumerable<QualityFinding> findings) => scoreService.Calculate(records, findings); }
public sealed class DataQualityFixService { public void EnsureReason(QualityFindingStatus action, string? reason) { if ((action is QualityFindingStatus.Ignored or QualityFindingStatus.FalsePositive) && string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("O motivo é obrigatório.", nameof(reason)); } }
public sealed class DataQualityAutomationIntegrationService { public bool RequiresHumanReview(string eventCode) => eventCode is "DUPLICATE_DETECTED" or "SENSITIVE_DATA_CHANGE" or "NORMALIZATION_SUGGESTED"; }
public sealed class GovernedDataQualityAiService { public string Explain(QualityFinding finding) => $"{finding.Message} Ação sugerida para revisão humana: {finding.Recommendation}"; }

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrcaFacil.Application.Documents;

public sealed record DocumentSnapshot(IssuerSnapshot Issuer, CustomerSnapshot Customer, QuoteSnapshot Quote, IReadOnlyList<QuoteItemSnapshot> Items);
public sealed record IssuerSnapshot(string Name, string? Document, string? Email, string? Phone, string? Address, string? City, string? State, string? Logo, string? Pix, string? CommercialData);
public sealed record CustomerSnapshot(string Name, string? Type, string? Document, string? Phone, string? Email, string? Address, string? City, string? State);
public sealed record QuoteSnapshot(string Number, DateTime IssueDate, DateTime? ValidUntil, string? DeliveryTime, string? Payment, string? Conditions, string? Notes, string Template, string? PrimaryColor, string? Footer, bool ShowPlatformBrand, decimal Subtotal, decimal Discount, decimal Total);
public sealed record QuoteItemSnapshot(string Description, string? Unit, decimal Quantity, decimal UnitPrice, decimal Discount, decimal Subtotal, decimal Total);
public sealed record SerializedDocumentSnapshot(string Json, string Hash);

public interface IDocumentSnapshotSerializer
{
    SerializedDocumentSnapshot Serialize(DocumentSnapshot snapshot);
    string ComputeHash(string canonicalJson);
}

/// <summary>Creates the stable, culture-independent representation used by immutable commercial revisions.</summary>
public sealed class DocumentSnapshotSerializer : IDocumentSnapshotSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = false
    };

    public SerializedDocumentSnapshot Serialize(DocumentSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var canonical = new DocumentSnapshot(Normalize(snapshot.Issuer), Normalize(snapshot.Customer), Normalize(snapshot.Quote),
            snapshot.Items.Select(Normalize).OrderBy(x => x.Description, StringComparer.Ordinal)
                .ThenBy(x => x.Unit, StringComparer.Ordinal).ThenBy(x => x.Quantity).ThenBy(x => x.UnitPrice).ToArray());
        var json = JsonSerializer.Serialize(canonical, Options);
        return new(json, ComputeHash(json));
    }

    public string ComputeHash(string canonicalJson) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalJson ?? string.Empty)));

    private static string N(string? value) => (value ?? string.Empty).Trim().Normalize(NormalizationForm.FormC);
    private static string? O(string? value) => string.IsNullOrWhiteSpace(value) ? null : N(value);
    private static decimal D(decimal value) => decimal.Parse(value.ToString("0.############################", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    private static DateTime T(DateTime value) => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    private static IssuerSnapshot Normalize(IssuerSnapshot x) => new(N(x.Name), O(x.Document), O(x.Email)?.ToLowerInvariant(), O(x.Phone), O(x.Address), O(x.City), O(x.State)?.ToUpperInvariant(), O(x.Logo), O(x.Pix), O(x.CommercialData));
    private static CustomerSnapshot Normalize(CustomerSnapshot x) => new(N(x.Name), O(x.Type), O(x.Document), O(x.Phone), O(x.Email)?.ToLowerInvariant(), O(x.Address), O(x.City), O(x.State)?.ToUpperInvariant());
    private static QuoteSnapshot Normalize(QuoteSnapshot x) => new(N(x.Number), T(x.IssueDate), x.ValidUntil is null ? null : T(x.ValidUntil.Value), O(x.DeliveryTime), O(x.Payment), O(x.Conditions), O(x.Notes), N(x.Template).ToLowerInvariant(), O(x.PrimaryColor)?.ToUpperInvariant(), O(x.Footer), x.ShowPlatformBrand, D(x.Subtotal), D(x.Discount), D(x.Total));
    private static QuoteItemSnapshot Normalize(QuoteItemSnapshot x) => new(N(x.Description), O(x.Unit)?.ToLowerInvariant(), D(x.Quantity), D(x.UnitPrice), D(x.Discount), D(x.Subtotal), D(x.Total));
}

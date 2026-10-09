using System.Globalization;
using System.IO;
using OrcaFacil.Application.Localization;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Document = OrcaFacil.Domain.Entities.Document;

namespace OrcaFacil.Infrastructure.Pdf;

public abstract class DocumentPdfTemplate
{
    private const string Primary = "#1E3A5F";
    private const string Accent = "#2D7DD2";
    private const string Success = "#1F9D6B";

    protected DocumentPdfTemplate(Document document, IssuerProfile? issuer, PlanType plan, string? languageCode = null, string? currencyCode = "BRL")
    {
        Document = document;
        Issuer = issuer;
        Plan = plan;
        LanguageCode = SupportedLocales.Normalize(languageCode);
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "BRL" : currencyCode.Trim().ToUpperInvariant();
    }

    protected Document Document { get; }
    protected IssuerProfile? Issuer { get; }
    protected PlanType Plan { get; }
    public string LanguageCode { get; }
    public string CurrencyCode { get; }
    protected abstract string Title { get; }

    protected string FormatCurrency(decimal amount)
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo(LanguageCode).Clone();
        culture.NumberFormat.CurrencySymbol = CurrencyCode switch
        {
            "USD" => "US$",
            "EUR" => "€",
            _ => "R$"
        };
        return amount.ToString("C", culture);
    }

    protected string FormatDate(DateTime date)
    {
        return LanguageCode == "en-US" ? date.ToString("MM/dd/yyyy") : date.ToString("dd/MM/yyyy");
    }

    protected string GetLabel(string key) => LanguageCode switch
    {
        "en-US" => key switch
        {
            "Tagline" => "Professional quotes and receipts — a MNSOFT product",
            "Issued" => "Issued:",
            "ValidUntil" => "Valid until:",
            "Issuer" => "Issuer",
            "NotSpecified" => "Not specified",
            "Client" => "Client",
            "ClientNotSpecified" => "Client not specified",
            "Description" => "Description",
            "Qty" => "Qty.",
            "UnitPrice" => "Unit Price",
            "Total" => "Total",
            "Subtotal" => "Subtotal:",
            "Discount" => "Discount:",
            "NotesScope" => "Notes / Scope:",
            "CommercialConditions" => "Commercial Terms",
            "Warranty" => "Warranty",
            "GeneratedWith" => "Generated with OrçaFácil — a MNSOFT product",
            "QuoteTitle" => "Quote",
            "ReceiptTitle" => "Receipt",
            "PublicApprovalNotice" => "Customer approval block available via secure public link.",
            "ReceiptNotice" => "Received the amount of {0} ({1}) for the services and items listed in this document.",
            "PaymentMethod" => "Payment method: {0}",
            _ => key
        },
        "es-ES" or "es-419" => key switch
        {
            "Tagline" => "Presupuestos y recibos profesionales — un producto MNSOFT",
            "Issued" => "Emisión:",
            "ValidUntil" => "Validez:",
            "Issuer" => "Emisor",
            "NotSpecified" => "No especificado",
            "Client" => "Cliente",
            "ClientNotSpecified" => "Cliente no especificado",
            "Description" => "Descripción",
            "Qty" => "Cant.",
            "UnitPrice" => "Precio unit.",
            "Total" => "Total",
            "Subtotal" => "Subtotal:",
            "Discount" => "Descuento:",
            "NotesScope" => "Observaciones / Alcance:",
            "CommercialConditions" => "Condiciones Comerciales",
            "Warranty" => "Garantía",
            "GeneratedWith" => "Generado con OrçaFácil — un producto MNSOFT",
            "QuoteTitle" => "Presupuesto",
            "ReceiptTitle" => "Recibo",
            "PublicApprovalNotice" => "Bloque de aprobación del cliente disponible a través del enlace público seguro.",
            "ReceiptNotice" => "Hemos recibido la cantidad de {0} ({1}) correspondiente a los servicios e ítems detallados en este documento.",
            "PaymentMethod" => "Forma de pago / cobro: {0}",
            _ => key
        },
        _ => key switch
        {
            "Tagline" => "Orçamentos e recibos profissionais — um produto MNSOFT",
            "Issued" => "Emissão:",
            "ValidUntil" => "Validade:",
            "Issuer" => "Emitente",
            "NotSpecified" => "Não informado",
            "Client" => "Cliente",
            "ClientNotSpecified" => "Cliente não informado",
            "Description" => "Descrição",
            "Qty" => "Qtd.",
            "UnitPrice" => "Unitário",
            "Total" => "Total",
            "Subtotal" => "Subtotal:",
            "Discount" => "Desconto:",
            "NotesScope" => "Observações / Escopo:",
            "CommercialConditions" => "Condições Comerciais",
            "Warranty" => "Garantia",
            "GeneratedWith" => "Gerado com OrçaFácil — um produto MNSOFT",
            "QuoteTitle" => "Orçamento",
            "ReceiptTitle" => "Recibo",
            "PublicApprovalNotice" => "Bloco de aprovação do cliente disponível pelo link público.",
            "ReceiptNotice" => "Recebemos a quantia de {0} ({1}) referente aos serviços e itens discriminados neste documento.",
            "PaymentMethod" => "Forma de quitação / recebimento: {0}",
            _ => key
        }
    };

    public byte[] Generate() => QuestPDF.Fluent.Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Margin(34);
            page.DefaultTextStyle(x => x.FontSize(10).FontColor("#1C2430"));
            page.Header().Background(Primary).Padding(16).Row(row =>
            {
                var logoBytes = GetLogoBytes();
                if (logoBytes is not null && logoBytes.Length > 0)
                {
                    row.ConstantItem(100).MaxHeight(48).AlignMiddle().Image(logoBytes).FitArea();
                    row.ConstantItem(12);
                }

                row.RelativeItem().Column(col =>
                {
                    var headerTitle = !string.IsNullOrWhiteSpace(Issuer?.BusinessName)
                        ? Issuer.BusinessName
                        : "OrçaFácil";
                    col.Item().Text(headerTitle).FontColor(Colors.White).Bold().FontSize(18);
                    col.Item().Text(GetLabel("Tagline")).FontColor("#E9F3FF").FontSize(9);
                    col.Item().Text($"{Title} {Document.Number}").FontColor("#E9F3FF").FontSize(11).SemiBold();
                });

                row.ConstantItem(130).AlignRight().Column(col =>
                {
                    col.Item().AlignRight().Text($"{GetLabel("Issued")} {FormatDate(Document.IssueDate)}").FontColor(Colors.White).SemiBold();
                    if (Document.ValidUntil.HasValue)
                    {
                        col.Item().AlignRight().Text($"{GetLabel("ValidUntil")} {FormatDate(Document.ValidUntil.Value)}").FontColor("#E9F3FF").FontSize(9);
                    }
                });
            });

            page.Content().PaddingVertical(18).Column(column =>
            {
                column.Spacing(12);
                column.Item().Row(row =>
                {
                    row.RelativeItem().Border(1).BorderColor("#E2E8F0").Padding(12).Column(col =>
                    {
                        col.Item().Text(GetLabel("Issuer")).FontColor(Accent).Bold();
                        col.Item().Text(Issuer?.BusinessName ?? GetLabel("NotSpecified")).Bold();
                        if (!string.IsNullOrWhiteSpace(Issuer?.DocumentNumber)) col.Item().Text(Issuer.DocumentNumber);
                        if (!string.IsNullOrWhiteSpace(Issuer?.Email)) col.Item().Text(Issuer.Email);
                        if (!string.IsNullOrWhiteSpace(Issuer?.Phone)) col.Item().Text(Issuer.Phone);
                        if (!string.IsNullOrWhiteSpace(Issuer?.Address)) col.Item().Text(Issuer.Address);
                    });
                    row.ConstantItem(16);
                    row.RelativeItem().Border(1).BorderColor("#E2E8F0").Padding(12).Column(col =>
                    {
                        col.Item().Text(GetLabel("Client")).FontColor(Accent).Bold();
                        col.Item().Text(string.IsNullOrWhiteSpace(Document.ClientName) ? GetLabel("ClientNotSpecified") : Document.ClientName).Bold();
                        if (!string.IsNullOrWhiteSpace(Document.ClientDocument)) col.Item().Text(Document.ClientDocument);
                        if (!string.IsNullOrWhiteSpace(Document.ClientEmail)) col.Item().Text(Document.ClientEmail);
                        if (!string.IsNullOrWhiteSpace(Document.ClientPhone)) col.Item().Text(Document.ClientPhone);
                        if (!string.IsNullOrWhiteSpace(Document.ClientCity)) col.Item().Text(Document.ClientCity);
                    });
                });

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(5);
                        c.ConstantColumn(52);
                        c.ConstantColumn(108);
                        c.ConstantColumn(118);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Background("#EAF1F8").Padding(6).Text(GetLabel("Description")).FontColor(Primary).Bold();
                        h.Cell().Background("#EAF1F8").Padding(6).AlignRight().Text(GetLabel("Qty")).FontColor(Primary).Bold();
                        h.Cell().Background("#EAF1F8").Padding(6).AlignRight().Text(GetLabel("UnitPrice")).FontColor(Primary).Bold();
                        h.Cell().Background("#EAF1F8").Padding(6).AlignRight().Text(GetLabel("Total")).FontColor(Primary).Bold();
                    });
                    foreach (var item in Document.Items)
                    {
                        table.Cell().ShowEntire().BorderBottom(1).BorderColor("#E2E8F0").Padding(6).Text(item.Description);
                        table.Cell().ShowEntire().BorderBottom(1).BorderColor("#E2E8F0").Padding(6).AlignRight().Text(item.Quantity.ToString("N2", CultureInfo.GetCultureInfo(LanguageCode)));
                        table.Cell().ShowEntire().BorderBottom(1).BorderColor("#E2E8F0").Padding(6).AlignRight().Text(FormatCurrency(item.UnitPrice));
                        table.Cell().ShowEntire().BorderBottom(1).BorderColor("#E2E8F0").Padding(6).AlignRight().Text(FormatCurrency(item.CalculateTotal()));
                    }
                });

                if (Document.Discount > 0)
                {
                    column.Item().AlignRight().Text($"{GetLabel("Subtotal")} {FormatCurrency(Document.Subtotal)}");
                    column.Item().AlignRight().Text($"{GetLabel("Discount")} {FormatCurrency(Document.Discount)}");
                }

                column.Item().AlignRight().Background("#E9F7F1").Padding(12).Text($"{GetLabel("Total")} {FormatCurrency(Document.Total)}").FontColor(Success).Bold().FontSize(18);

                if (!string.IsNullOrWhiteSpace(Document.Notes))
                {
                    column.Item().Border(1).BorderColor("#E2E8F0").Padding(10).Column(c =>
                    {
                        c.Item().Text(GetLabel("NotesScope")).SemiBold().FontColor(Primary);
                        c.Item().Text(Document.Notes);
                    });
                }

                if (!string.IsNullOrWhiteSpace(Document.ConditionsText) || !string.IsNullOrWhiteSpace(Document.WarrantyText))
                {
                    column.Item().Row(r =>
                    {
                        if (!string.IsNullOrWhiteSpace(Document.ConditionsText))
                        {
                            r.RelativeItem().Border(1).BorderColor("#E2E8F0").Padding(8).Column(c =>
                            {
                                c.Item().Text(GetLabel("CommercialConditions")).SemiBold().FontColor(Primary);
                                c.Item().Text(Document.ConditionsText);
                            });
                        }
                        if (!string.IsNullOrWhiteSpace(Document.ConditionsText) && !string.IsNullOrWhiteSpace(Document.WarrantyText))
                        {
                            r.ConstantItem(12);
                        }
                        if (!string.IsNullOrWhiteSpace(Document.WarrantyText))
                        {
                            r.RelativeItem().Border(1).BorderColor("#E2E8F0").Padding(8).Column(c =>
                            {
                                c.Item().Text(GetLabel("Warranty")).SemiBold().FontColor(Primary);
                                c.Item().Text(Document.WarrantyText);
                            });
                        }
                    });
                }

                AddSpecificContent(column);

                if (Plan == PlanType.Free)
                {
                    column.Item().AlignCenter().Text(GetLabel("GeneratedWith")).FontColor(Colors.Grey.Medium).FontSize(9);
                }
            });

            page.Footer().AlignCenter().Text("MNSOFT • CNPJ 18.160.057/0001-13 • comercial@mnsoft.com.br").FontColor(Primary);
        });
    }).GeneratePdf();

    protected virtual void AddSpecificContent(ColumnDescriptor column) { }

    private bool _logoLoaded;
    private byte[]? _cachedLogoBytes;

    protected byte[]? GetLogoBytes()
    {
        if (_logoLoaded) return _cachedLogoBytes;
        _cachedLogoBytes = TryLoadLogoBytes(Issuer?.LogoPath, Document.AccountId);
        _logoLoaded = true;
        return _cachedLogoBytes;
    }

    private static byte[]? TryLoadLogoBytes(string? logoPath, Guid? accountId)
    {
        if (string.IsNullOrWhiteSpace(logoPath)) return null;

        // Proteção contra SSRF e esquemas externos
        if (logoPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            logoPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            logoPath.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) ||
            logoPath.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Rejeita tentativas explícitas de path traversal e caracteres inválidos
        if (logoPath.Contains("..") || logoPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            return null;
        }

        try
        {
            var relativePath = logoPath.TrimStart('/', '\\')
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            // Se for caminho em uploads/branding e a conta for conhecida, validar pertença à conta
            if (relativePath.StartsWith($"uploads{Path.DirectorySeparatorChar}branding{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                if (accountId.HasValue && accountId.Value != Guid.Empty)
                {
                    var expectedPrefix = $"uploads{Path.DirectorySeparatorChar}branding{Path.DirectorySeparatorChar}{accountId.Value:N}";
                    if (!relativePath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        // Tentativa de acessar logo de outra conta: rejeitar
                        return null;
                    }
                }
            }

            var ext = Path.GetExtension(relativePath).ToLowerInvariant();
            if (ext is not (".png" or ".jpg" or ".jpeg" or ".webp")) return null;

            // Raízes autorizadas confinadas ao storage web da aplicação
            var allowedRoots = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "wwwroot"),
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "OrcaFacil.Web", "wwwroot")
            };

            string? resolvedPath = null;
            foreach (var root in allowedRoots)
            {
                try
                {
                    if (!Directory.Exists(root)) continue;
                    var fullRoot = Path.GetFullPath(root);
                    var combined = Path.GetFullPath(Path.Combine(fullRoot, relativePath));

                    // Confinamento estrito à raiz autorizada (garante ausência de jailbreak/escape)
                    if (combined.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(combined))
                    {
                        resolvedPath = combined;
                        break;
                    }
                }
                catch
                {
                    // Ignora erros de caminho em raízes não existentes
                }
            }

            if (resolvedPath is null) return null;

            var fileInfo = new FileInfo(resolvedPath);
            if (fileInfo.Length > 2 * 1024 * 1024) return null; // Limite de 2 MB

            var bytes = File.ReadAllBytes(resolvedPath);
            if (bytes.Length < 4) return null;

            // Validação estrita de magic bytes para PNG, JPEG ou WebP
            var isPng = bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4e && bytes[3] == 0x47;
            var isJpeg = bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff;
            var isWebp = bytes.Length >= 12 && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
                         bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50;

            return (isPng || isJpeg || isWebp) ? bytes : null;
        }
        catch
        {
            // Fallback gracioso: imagem corrompida mantém o PDF funcional
            return null;
        }
    }
}

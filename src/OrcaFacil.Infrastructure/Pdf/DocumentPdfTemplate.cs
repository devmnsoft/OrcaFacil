using System.IO;
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

    protected DocumentPdfTemplate(Document document, IssuerProfile? issuer, PlanType plan)
    {
        Document = document;
        Issuer = issuer;
        Plan = plan;
    }

    protected Document Document { get; }
    protected IssuerProfile? Issuer { get; }
    protected PlanType Plan { get; }
    protected abstract string Title { get; }

    public byte[] Generate() => QuestPDF.Fluent.Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Margin(34);
            page.DefaultTextStyle(x => x.FontSize(10).FontColor("#1C2430"));
            page.Header().Background(Primary).Padding(16).Row(row =>
            {
                var logoBytes = TryLoadLogoBytes(Issuer?.LogoPath);
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
                    col.Item().Text("Orçamentos e recibos profissionais — um produto MNSOFT").FontColor("#E9F3FF").FontSize(9);
                    col.Item().Text($"{Title} {Document.Number}").FontColor("#E9F3FF").FontSize(11).SemiBold();
                });

                row.ConstantItem(130).AlignRight().Column(col =>
                {
                    col.Item().AlignRight().Text($"Emissão: {Document.IssueDate:dd/MM/yyyy}").FontColor(Colors.White).SemiBold();
                    if (Document.ValidUntil.HasValue)
                    {
                        col.Item().AlignRight().Text($"Validade: {Document.ValidUntil.Value:dd/MM/yyyy}").FontColor("#E9F3FF").FontSize(9);
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
                        col.Item().Text("Emitente").FontColor(Accent).Bold();
                        col.Item().Text(Issuer?.BusinessName ?? "Não informado").Bold();
                        if (!string.IsNullOrWhiteSpace(Issuer?.DocumentNumber)) col.Item().Text(Issuer.DocumentNumber);
                        if (!string.IsNullOrWhiteSpace(Issuer?.Email)) col.Item().Text(Issuer.Email);
                        if (!string.IsNullOrWhiteSpace(Issuer?.Phone)) col.Item().Text(Issuer.Phone);
                        if (!string.IsNullOrWhiteSpace(Issuer?.Address)) col.Item().Text(Issuer.Address);
                    });
                    row.ConstantItem(16);
                    row.RelativeItem().Border(1).BorderColor("#E2E8F0").Padding(12).Column(col =>
                    {
                        col.Item().Text("Cliente").FontColor(Accent).Bold();
                        col.Item().Text(string.IsNullOrWhiteSpace(Document.ClientName) ? "Cliente não informado" : Document.ClientName).Bold();
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
                        h.Cell().Background("#EAF1F8").Padding(6).Text("Descrição").FontColor(Primary).Bold();
                        h.Cell().Background("#EAF1F8").Padding(6).AlignRight().Text("Qtd.").FontColor(Primary).Bold();
                        h.Cell().Background("#EAF1F8").Padding(6).AlignRight().Text("Unitário").FontColor(Primary).Bold();
                        h.Cell().Background("#EAF1F8").Padding(6).AlignRight().Text("Total").FontColor(Primary).Bold();
                    });
                    foreach (var item in Document.Items)
                    {
                        table.Cell().ShowEntire().BorderBottom(1).BorderColor("#E2E8F0").Padding(6).Text(item.Description);
                        table.Cell().ShowEntire().BorderBottom(1).BorderColor("#E2E8F0").Padding(6).AlignRight().Text(item.Quantity.ToString("N2"));
                        table.Cell().ShowEntire().BorderBottom(1).BorderColor("#E2E8F0").Padding(6).AlignRight().Text(item.UnitPrice.ToString("C"));
                        table.Cell().ShowEntire().BorderBottom(1).BorderColor("#E2E8F0").Padding(6).AlignRight().Text(item.CalculateTotal().ToString("C"));
                    }
                });

                if (Document.Discount > 0)
                {
                    column.Item().AlignRight().Text($"Subtotal: {Document.Subtotal:C}");
                    column.Item().AlignRight().Text($"Desconto: {Document.Discount:C}");
                }

                column.Item().AlignRight().Background("#E9F7F1").Padding(12).Text($"Total: {Document.Total:C}").FontColor(Success).Bold().FontSize(18);

                if (!string.IsNullOrWhiteSpace(Document.Notes))
                {
                    column.Item().Border(1).BorderColor("#E2E8F0").Padding(10).Column(c =>
                    {
                        c.Item().Text("Observações / Escopo:").SemiBold().FontColor(Primary);
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
                                c.Item().Text("Condições Comerciais").SemiBold().FontColor(Primary);
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
                                c.Item().Text("Garantia").SemiBold().FontColor(Primary);
                                c.Item().Text(Document.WarrantyText);
                            });
                        }
                    });
                }

                AddSpecificContent(column);

                if (Plan == PlanType.Free)
                {
                    column.Item().AlignCenter().Text("Gerado com OrçaFácil — um produto MNSOFT").FontColor(Colors.Grey.Medium).FontSize(9);
                }
            });

            page.Footer().AlignCenter().Text("MNSOFT • CNPJ 18.160.057/0001-13 • comercial@mnsoft.com.br").FontColor(Primary);
        });
    }).GeneratePdf();

    protected virtual void AddSpecificContent(ColumnDescriptor column) { }

    private static byte[]? TryLoadLogoBytes(string? logoPath)
    {
        if (string.IsNullOrWhiteSpace(logoPath)) return null;

        // Proteção contra SSRF e requisições externas não autorizadas
        if (logoPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            logoPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var cleanPath = logoPath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            var candidates = new[]
            {
                cleanPath,
                Path.Combine(AppContext.BaseDirectory, cleanPath),
                Path.Combine(AppContext.BaseDirectory, "wwwroot", cleanPath),
                Path.Combine(Directory.GetCurrentDirectory(), cleanPath),
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", cleanPath),
                Path.Combine(Directory.GetCurrentDirectory(), "src", "OrcaFacil.Web", "wwwroot", cleanPath)
            };

            string? resolvedPath = null;
            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    resolvedPath = candidate;
                    break;
                }
            }

            if (resolvedPath is null) return null;

            var ext = Path.GetExtension(resolvedPath).ToLowerInvariant();
            if (ext is not (".png" or ".jpg" or ".jpeg" or ".webp")) return null;

            var fileInfo = new FileInfo(resolvedPath);
            if (fileInfo.Length > 2 * 1024 * 1024) return null; // Limite de 2 MB

            var bytes = File.ReadAllBytes(resolvedPath);
            if (bytes.Length < 4) return null;

            // Validação de magic bytes para PNG, JPEG ou WebP
            var isPng = bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4e && bytes[3] == 0x47;
            var isJpeg = bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff;
            var isWebp = bytes.Length >= 12 && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
                         bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50;

            return (isPng || isJpeg || isWebp) ? bytes : null;
        }
        catch
        {
            return null;
        }
    }
}

using System.Text.RegularExpressions;

namespace OrcaFacil.Application.DocumentTemplates;

public static partial class DocumentTemplateValidator
{
    public static readonly IReadOnlySet<string> AllowedVariables = new HashSet<string>(StringComparer.Ordinal)
    { "empresa_nome", "empresa_documento", "empresa_email", "empresa_telefone", "empresa_whatsapp", "empresa_endereco", "cliente_nome", "cliente_documento", "cliente_email", "cliente_telefone", "proposta_numero", "proposta_data", "proposta_validade", "proposta_total", "proposta_itens", "condicoes_comerciais", "observacoes", "os_numero", "recibo_numero", "recibo_valor", "contrato_numero" };

    public static IReadOnlyList<string> Validate(string content, string? css = null)
    {
        var errors = new List<string>();
        var source = string.Concat(content, "\n", css);
        if (Regex.IsMatch(source, @"<\s*script\b|javascript\s*:|on\w+\s*=", RegexOptions.IgnoreCase))
            errors.Add("Scripts, handlers de evento e URLs JavaScript não são permitidos.");
        foreach (Match match in VariableRegex().Matches(source))
            if (!AllowedVariables.Contains(match.Groups[1].Value)) errors.Add($"Variável inválida: {{{{{match.Groups[1].Value}}}}}.");
        return errors.Distinct(StringComparer.Ordinal).ToArray();
    }

    [GeneratedRegex(@"\{\{\s*([a-z_]+)\s*\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex VariableRegex();
}

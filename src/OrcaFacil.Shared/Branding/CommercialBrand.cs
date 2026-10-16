namespace OrcaFacil.Shared.Branding;

/// <summary>
/// Camada central de apresentação da marca comercial do produto.
/// A marca percebida pelo usuário é <b>Fechô</b>; a engenharia continua usando os
/// identificadores técnicos <c>OrcaFacil.*</c> (namespaces, banco, schema, rotas).
///
/// Esta classe reúne apenas textos e referências visuais de apresentação, permitindo
/// reutilização em layouts, e-mails, notificações, PDFs, PWA e metadados públicos
/// sem duplicar valores em dezenas de arquivos.
/// </summary>
public static class CommercialBrand
{
    /// <summary>Nome comercial exibido ao usuário. O acento circunflexo é obrigatório.</summary>
    public const string Name = "Fechô";

    /// <summary>Slogan principal da marca.</summary>
    public const string Tagline = "Orçou. Enviou. Fechô.";

    /// <summary>Assinatura institucional completa.</summary>
    public const string Endorsement = "Um produto MNSOFT";

    /// <summary>Assinatura compacta da controladora, sem competir com a marca Fechô.</summary>
    public const string ParentCompany = "MNSOFT";

    /// <summary>Posicionamento comercial em uma frase.</summary>
    public const string ValueProposition =
        "A forma simples de organizar seus serviços, enviar propostas profissionais e transformar oportunidades em negócios.";

    /// <summary>Headline principal da landing page.</summary>
    public const string HeroHeadline = "Seu trabalho vale muito. Seu orçamento também.";

    /// <summary>Subheadline principal da landing page.</summary>
    public const string HeroSubheadline =
        "Crie propostas profissionais, organize seus clientes e acompanhe seus negócios em um só lugar.";

    /// <summary>Chamada principal de conversão.</summary>
    public const string CtaPrimary = "Começar grátis";

    /// <summary>Chamada secundária de conversão.</summary>
    public const string CtaSecondary = "Conhecer o Fechô";

    // ---- Identidade jurídica da controladora (não é a marca do produto) ----
    public const string LegalCompanyName = "MNSOFT";
    public const string CompanyCnpj = "18.160.057/0001-13";
    public const string CommercialEmail = "comercial@mnsoft.com.br";
    public const string PrivacyEmail = "privacidade@mnsoft.com.br";

    // ---- Paleta oficial (espelhada em wwwroot/css/tokens.css) ----
    public const string ColorDeepGreen = "#0B3835";
    public const string ColorPetrol = "#0F766E";
    public const string ColorMint = "#B9F1D1";
    public const string ColorIce = "#F5F8F7";
    public const string ColorGraphite = "#172B2A";
    public const string ColorSoftGrey = "#536B67";
    public const string ColorAmber = "#D6A34F";

    // ---- Assets comerciais ----
    public const string LogoHorizontalLight = "/img/brand/fecho-logo.svg";
    public const string LogoHorizontalWhite = "/img/brand/fecho-logo-white.svg";
    public const string LogoHorizontalDark = "/img/brand/fecho-logo-dark.svg";
    public const string LogoMono = "/img/brand/fecho-logo-mono.svg";
    public const string Symbol = "/img/brand/fecho-symbol.svg";
    public const string Compact = "/img/brand/fecho-compact.svg";
    public const string PdfLogo = "/img/brand/fecho-pdf.svg";
    public const string Favicon = "/favicon.svg";

    /// <summary>
    /// Assinatura da marca d'água do plano gratuito, localizada por idioma.
    /// Planos pagos não recebem marca d'água (regra preservada em <c>PlanLimitService</c>).
    /// </summary>
    public static string Watermark(string? languageCode) => languageCode switch
    {
        "en-US" => "Generated with Fechô — a MNSOFT product",
        "es-ES" or "es-419" => "Generado con Fechô — un producto MNSOFT",
        _ => "Gerado com Fechô — Um produto MNSOFT"
    };

    /// <summary>Rodapé institucional usado em e-mails e documentos.</summary>
    public static string InstitutionalFooter => $"{Name} · {Endorsement} · {CommercialEmail}";
}

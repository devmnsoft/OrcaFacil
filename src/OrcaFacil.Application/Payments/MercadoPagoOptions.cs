namespace OrcaFacil.Application.Payments;

public class MercadoPagoOptions
{
    public bool Enabled { get; set; }
    public string Environment { get; set; } = "Sandbox";
    public string AccessToken { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string StatementDescriptor { get; set; } = "ORCAFACIL";
    public int PixExpirationMinutes { get; set; } = 60;
    public int BoletoExpirationDays { get; set; } = 3;
}

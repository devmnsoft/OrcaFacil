using OrcaFacil.Domain.Common;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Domain.Entities;

public class Payment : Entity
{
    public Guid UserId { get; set; }
    public Guid? SubscriptionId { get; set; }
    public string Provider { get; set; } = "Manual";
    public PaymentStatus Status { get; set; }
    public PlanType Plan { get; set; }
    public string? BillingCycle { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BRL";
    public string? ExternalPaymentId { get; set; }
    public string? ExternalPreferenceId { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ExternalReference { get; set; }
    public string? PixQrCode { get; set; }
    public string? PixQrCodeBase64 { get; set; }
    public string? PixTicketUrl { get; set; }
    public string? BoletoUrl { get; set; }
    public string? BoletoBarcode { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? RawResponseJson { get; set; }
    public string? PayerEmail { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

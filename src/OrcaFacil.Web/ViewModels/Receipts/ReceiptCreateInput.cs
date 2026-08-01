using System.ComponentModel.DataAnnotations;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Web.ViewModels.Receipts;

public sealed class ReceiptCreateInput
{
    [Required(ErrorMessage = "Selecione um cliente.")]
    public Guid ClientId { get; set; }
    [EnumDataType(typeof(ReceiptOriginType), ErrorMessage = "Selecione uma origem válida.")]
    public ReceiptOriginType OriginType { get; set; } = ReceiptOriginType.Standalone;
    public Guid? WorkOrderId { get; set; }
    public Guid? DocumentId { get; set; }
    [Range(0.01, 999999999, ErrorMessage = "Informe um valor maior que zero.")]
    public decimal Amount { get; set; }
    [Required, StringLength(40)] public string PaymentMethod { get; set; } = "Pix";
    [Required] public DateTime PaidAt { get; set; } = DateTime.Today;
    [StringLength(180)] public string? City { get; set; }
    [Required(ErrorMessage = "Descreva o serviço recebido."), StringLength(1000)]
    public string ServiceDescription { get; set; } = string.Empty;
    [StringLength(1000)] public string? Notes { get; set; }
    [StringLength(128)] public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N");
}

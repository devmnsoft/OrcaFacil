using OrcaFacil.Domain.Entities;

namespace OrcaFacil.Application.Billing;

/// <summary>
/// Colaborador opcional do ciclo de cobrança: traduz uma TRANSIÇÃO REAL de status
/// de assinatura (registrada no motor BillingStatusService após persistência)
/// em aviso para o titular da conta. Sem consumidor configurado o motor segue
/// funcionando apenas com a trilha de eventos — nada de notificação decorativa.
/// </summary>
public interface IBillingNoticeService
{
    Task NotifyAsync(Subscription subscription, string eventType, string details, CancellationToken ct = default);
}

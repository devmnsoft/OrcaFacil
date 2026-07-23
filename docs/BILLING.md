# Billing e benefícios

Statuses: `SubscriptionStatus` = Free, Trial, Active, PastDue, Suspended, Cancelled, ManualRelease. `PaymentStatus` = Pending, Approved, Rejected, Cancelled, Expired, Refunded, Chargeback.

`PlanEntitlementService` centraliza bloqueios: inadimplentes PastDue/Suspended perdem benefícios Pro, permanecem no acesso Free, veem documentos existentes e voltam a gerar PDF com marca OrçaFácil. A rotina `BillingStatusService` prepara sincronização de vencidos, suspensão e restauração.

# Pendências operacionais V1.9

- **Gateway online:** não está habilitado. Nenhum checkout, PIX, boleto ou aprovação automática é exibido. A contratação segue por solicitação e a equipe registra pagamentos manuais apenas após confirmação externa real.
- **Operação:** configurar e homologar um provedor real, segredo via ambiente/cofre, webhook assinado e conciliação antes de habilitar checkout.
- **Banco:** aplicar a migration `20260820210000_AddSaasBillingV19` (ou o patch idempotente `database/patch_sprint18_billing_v19.sql`) com backup e validação prévia.

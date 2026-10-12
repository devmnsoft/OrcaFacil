using OrcaFacil.Application.Common;

namespace OrcaFacil.Application.Commercial;

public sealed record CommercialNextActionContext(
    Guid DocumentId,
    string DocumentStatus,
    bool HasChangeRequest = false,
    CommercialBalance? Balance = null,
    Guid? WorkOrderId = null,
    string? WorkOrderStatus = null,
    decimal WorkOrderPaid = 0m,
    decimal WorkOrderBalance = 0m,
    Guid? LatestPaymentId = null,
    Guid? LatestReceiptId = null);

public static class CommercialNextActionResolver
{
    public static NextActionDescriptor ResolveQuoteAction(CommercialNextActionContext context)
    {
        if (context.HasChangeRequest)
            return DetailsAction("revision", "Criar nova versão",
                "Preserve a resposta do cliente e prepare os ajustes solicitados.", context.DocumentId, "negotiation", "version");

        if (context.WorkOrderId is { } workOrderId)
            return ResolveWorkOrderBackedQuote(context, workOrderId);

        return context.DocumentStatus.ToUpperInvariant() switch
        {
            "DRAFT" => Action("continue", "Continuar orçamento", "Complete os dados antes de compartilhar.",
                "/Documents/CreateBudget", context.DocumentId, "document"),
            "ISSUED" or "READY" => DetailsAction("share", "Criar acesso",
                "Envie uma versão segura ao cliente.", context.DocumentId, "sharing", "share"),
            "SENT" or "VIEWED" => DetailsAction("follow-up", "Programar retorno",
                "Mantenha a negociacao avancando; visualizacao nao e aprovacao.", context.DocumentId, "negotiation", "share"),
            "APPROVED" => DetailsAction("create-work-order", "Criar ordem",
                "Transforme a aprovação em execução sem duplicar caixa ou recebimentos.", context.DocumentId, "summary", "work-order"),
            "REJECTED" => DetailsAction("duplicate", "Duplicar proposta",
                "Preserve o histórico e use o conteúdo em uma nova negociação.", context.DocumentId, "summary", "copy-link"),
            _ => DetailsAction("review", "Revisar proposta",
                "Consulte o histórico e defina o próximo passo.", context.DocumentId)
        };
    }

    public static CommercialNextAction ResolveDocumentAction(CommercialNextActionContext context)
    {
        var action = ResolveQuoteAction(context);
        var routeId = action.RouteValues is not null && action.RouteValues.TryGetValue("id", out var id) && Guid.TryParse(id, out var parsed)
            ? parsed
            : (Guid?)null;

        var handler = action.Code switch
        {
            "revision" => "Revision",
            "create-work-order" => "WorkOrder",
            "duplicate" => "Duplicate",
            _ => null
        };

        return new CommercialNextAction(action.Code, ToDocumentTitle(action), action.Description, ToDocumentLabel(action),
            handler, handler is null ? action.Page : null, handler is null ? routeId : null, action.Icon,
            handler is null ? action.RouteValues : null);
    }

    private static NextActionDescriptor ResolveWorkOrderBackedQuote(CommercialNextActionContext context, Guid workOrderId)
    {
        if (context.Balance is { HasBlockingDivergence: true })
            return DetailsAction("financial-review", "Revisar vínculos",
                "Há divergência financeira impeditiva neste contrato.", context.DocumentId, "finance", "payment", "warning");

        if (context.Balance is { HasOverpayment: true })
            return DetailsAction("overpaid", "Revisar excedente",
                "Há recebimento acima do contratado.", context.DocumentId, "finance", "payment", "warning");

        return context.WorkOrderStatus switch
        {
            "Planned" => Action("schedule", "Agendar execução",
                "Defina data e responsável para tirar o serviço do planejamento.", "/WorkOrders/Details", workOrderId, "calendar"),
            "Scheduled" => Action("start", "Preparar início",
                "A ordem está agendada e pronta para execução.", "/WorkOrders/Details", workOrderId, "start"),
            "InProgress" => Action("complete", "Concluir execução",
                "Registre a conclusão técnica na ordem de serviço.", "/WorkOrders/Details", workOrderId, "success"),
            "Completed" when context.WorkOrderBalance > 0m => Action("payment", context.WorkOrderPaid > 0m ? "Registrar saldo restante" : "Registrar recebimento",
                $"Saldo atual de {context.WorkOrderBalance:C}. Serviço concluído não significa quitação.", "/Payments/Register", workOrderId, "payment"),
            _ when context.WorkOrderPaid > 0m && context.LatestPaymentId is not null => Action("receipt-pending", "Emitir recibo",
                "Há pagamento ativo sem recibo vigente.", "/WorkOrders/Details", workOrderId, "receipt"),
            _ when context.LatestReceiptId is { } receiptId => Action("receipt", "Consultar recibo",
                "Pagamento e recibo vigente estão registrados.", "/Receipts/Details", receiptId, "receipt"),
            _ when context.Balance is { IsSettled: true } => DetailsAction("finance", "Consultar financeiro",
                "Contrato sem saldo operacional; acompanhe o histórico financeiro.", context.DocumentId, "finance", "payment"),
            _ => Action("open-work-order", "Abrir ordem",
                "Acompanhe execução, recebimentos e próximos passos pela ordem de serviço.", "/WorkOrders/Details", workOrderId, "work-order")
        };
    }

    private static string ToDocumentTitle(NextActionDescriptor action) => action.Code switch
    {
        "continue" => "Edite e prepare para envio",
        "share" => "Compartilhe a proposta",
        "follow-up" => "Acompanhe a resposta do cliente",
        "revision" => "Transforme o retorno em uma nova versão",
        "create-work-order" => "Converta a aprovação em operação",
        "open-work-order" => "Abra a ordem de serviço",
        "payment" => action.Title,
        "receipt" => "Recibo disponível",
        "receipt-pending" => "Emita o recibo",
        "finance" => "Consulte o financeiro",
        _ => action.Title
    };

    private static string ToDocumentLabel(NextActionDescriptor action) => action.Code switch
    {
        "continue" => "Editar orçamento",
        "share" => "Compartilhar",
        "follow-up" => "Ver interação",
        "revision" => "Criar nova versão",
        "create-work-order" => "Criar ordem de serviço",
        "open-work-order" => "Abrir ordem de serviço",
        "financial-review" => "Revisar financeiro",
        "overpaid" => "Revisar excedente",
        "receipt" => "Visualizar recibo",
        "receipt-pending" => "Emitir recibo",
        "finance" => "Ver financeiro",
        _ => action.Title
    };

    private static NextActionDescriptor DetailsAction(string code, string title, string description, Guid documentId,
        string tab = "summary", string icon = "arrow-right", string tone = "primary") =>
        new(code, title, description, "/Documents/Details", new Dictionary<string, string>
        {
            ["id"] = documentId.ToString(),
            ["tab"] = tab
        }, icon, tone);

    private static NextActionDescriptor Action(string code, string title, string description, string page, Guid targetId,
        string icon = "arrow-right", string tone = "primary") =>
        new(code, title, description, page, new Dictionary<string, string>
        {
            ["id"] = targetId.ToString()
        }, icon, tone);
}

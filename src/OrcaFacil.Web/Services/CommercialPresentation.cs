namespace OrcaFacil.Web.Services;

public static class CommercialPresentation
{
    public static string QuoteStatus(string? status) => status switch
    {
        "Draft" => "Rascunho",
        "Ready" => "Pronto para envio",
        "Issued" => "Emitido",
        "Sent" => "Enviado ao cliente",
        "Viewed" => "Visualizado pelo cliente",
        "Approved" => "Aprovado",
        "Rejected" => "Recusado",
        "InNegotiation" => "Alteração solicitada",
        "ChangeRequested" => "Alteração solicitada",
        "Converted" => "Convertido em ordem de serviço",
        "ConvertedToWorkOrder" => "Convertido em ordem de serviço",
        "Expired" => "Validade encerrada",
        "Cancelled" => "Cancelado",
        _ => "Situação não reconhecida"
    };

    public static string WorkOrderStatus(string? status) => status switch
    {
        "Planned" => "Planejada",
        "Scheduled" => "Agendada",
        "InProgress" => "Em execução",
        "Paused" => "Pausada",
        "Completed" => "Concluída",
        "Cancelled" => "Cancelada",
        null or "" => "Sem ordem de serviço",
        _ => "Situação operacional não reconhecida"
    };

    public static string Financial(decimal? total, decimal paid) 
    {
        if (total is null) return "Sem movimento financeiro";
        var balance = total.Value - paid;
        if (balance < 0) balance = 0;
        if (paid == 0) return "Nenhum recebimento";
        if (balance == 0) return "Quitado";
        return "Recebido parcialmente";
    }

    public static string Decision(string? decision) => decision switch
    {
        "Approved" => "Aprovada",
        "Rejected" => "Recusada",
        "ChangeRequested" => "Alteração solicitada",
        null or "" => "Aguardando decisão",
        _ => decision
    };
}

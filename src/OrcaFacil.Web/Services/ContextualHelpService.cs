namespace OrcaFacil.Web.Services;

public interface IContextualHelpService
{
    HelpContentDefinition? Find(string code);
    IReadOnlyList<HelpContentDefinition> ForPage(string page);
}
public sealed class ContextualHelpService : IContextualHelpService
{
    private static readonly HelpContentDefinition[] Content =
    [
        new("budget.about", "O que é um orçamento?", "Apresente uma proposta clara.", "Apresenta ao cliente os serviços, valores, condições e prazo da proposta.", "Uma instalação elétrica com materiais, mão de obra e validade.", "budget", "/Documents/CreateBudget", null, HelpDisplayMode.Popover, 10, 1),
        new("budget.draft", "Rascunho", "Continue quando quiser.", "Fica salvo para você continuar depois. Ele ainda não foi enviado.", "Revise os itens antes de compartilhar.", "document", "/Documents/Edit", null, HelpDisplayMode.Tooltip, 20, 1),
        new("budget.validity", "Validade", "Defina até quando vale a proposta.", "Depois desta data, pode ser necessário criar uma nova versão.", "Validade de 15 dias.", "activity", "/Documents/CreateBudget", null, HelpDisplayMode.Popover, 30, 1),
        new("approval.about", "Aprovação online", "Receba a resposta pelo link.", "O cliente pode responder pelo link. O aceite é comercial e não representa assinatura digital certificada.", "O cliente aprova ou pede alteração.", "success", "/Discover", "Profissional", HelpDisplayMode.Drawer, 40, 1),
        new("receipt.about", "O que é um recibo?", "Registre um recebimento.", "Registra um pagamento informado por você. Não substitui nota fiscal.", "Pagamento de uma etapa concluída.", "receipt", "/Documents/CreateReceipt", null, HelpDisplayMode.Popover, 50, 1)
    ];
    public HelpContentDefinition? Find(string code) => Content.FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    public IReadOnlyList<HelpContentDefinition> ForPage(string page) => Content.Where(x => x.RelatedPage.Equals(page, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.DisplayOrder).ToArray();
}
public sealed record HelpContentDefinition(string Code,string Title,string Summary,string Explanation,string Example,string Icon,string RelatedPage,string? RequiredPlanCode,HelpDisplayMode DisplayMode,int DisplayOrder,int Version);
public enum HelpDisplayMode { Tooltip, Popover, Drawer, Modal }

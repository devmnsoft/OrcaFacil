using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OrcaFacil.Web.Pages.Support;

public sealed class IndexModel : PageModel
{
    private static readonly HelpTopic[] Topics =
    [
        new("Primeiros passos", "Crie sua conta e prepare o primeiro documento.", "/Help/PrimeiroOrcamento", "budget"),
        new("Clientes", "Cadastre e organize os dados dos seus clientes.", "/Clients/Index", "client"),
        new("Orçamentos", "Crie, revise, gere o PDF e envie.", "/Help/PrimeiroOrcamento", "budget"),
        new("Recibos", "Registre um pagamento recebido.", "/Help/PrimeiroRecibo", "receipt"),
        new("PDFs", "Baixe e compartilhe seus documentos.", "/Help/GerarPdf", "document"),
        new("Modelos", "Reaproveite estruturas de serviços.", "/Help/Modelos", "template"),
        new("Meu plano", "Consulte recursos e limites disponíveis.", "/Subscription/Index", "plan"),
        new("Pagamentos", "Entenda cobranças e o estado do plano.", "/Subscription/Index", "payment"),
        new("Acesso", "Resolva dificuldades para entrar.", "/Auth/Login", "person"),
        new("Segurança e privacidade", "Conheça os cuidados com seus dados.", "/Privacidade", "shield")
    ];

    [BindProperty(SupportsGet = true)] public string? Query { get; set; }
    public IReadOnlyList<HelpTopic> Results { get; private set; } = Topics;

    public void OnGet()
    {
        if (string.IsNullOrWhiteSpace(Query)) return;
        Results = Topics.Where(x => x.Title.Contains(Query, StringComparison.OrdinalIgnoreCase)
            || x.Description.Contains(Query, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    public sealed record HelpTopic(string Title, string Description, string Url, string Icon);
}

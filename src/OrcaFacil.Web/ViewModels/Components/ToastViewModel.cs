namespace OrcaFacil.Web.ViewModels.Components;

public sealed record ToastViewModel(string Type, string Message, string IconCss, string Title)
{
    public static ToastViewModel Create(string type, string message)
    {
        var normalized = string.IsNullOrWhiteSpace(type) ? "info" : type.Trim().ToLowerInvariant();
        return normalized switch
        {
            "success" => new("success", message, "bi-check-circle", "Tudo certo"),
            "warning" => new("warning", message, "bi-exclamation-triangle", "Atenção"),
            "danger" or "error" => new("danger", message, "bi-x-circle", "Não foi possível concluir"),
            _ => new("info", message, "bi-info-circle", "Informação")
        };
    }
}

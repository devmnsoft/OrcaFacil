namespace OrcaFacil.Web.ViewModels;
public sealed record InfoTooltipViewModel(string Id,string Label,string Text);
public sealed record InfoPopoverViewModel(string Id,string Title,string Summary,string Explanation);
public sealed record DialogViewModel(string Id,string Title,string Description,string ConfirmLabel,string? ConfirmPage=null);
public sealed record ContextGuideViewModel(string Title,string Purpose,string Saved,string Next,string Care);

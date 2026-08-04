namespace OrcaFacil.Web.ViewModels;
public sealed record InfoTooltipViewModel(string Id,string Label,string Text);
public sealed record InfoPopoverViewModel(string Id,string Title,string Summary,string Explanation);
public sealed record DialogViewModel(string Id,string Title,string Description,string ConfirmLabel,string? ConfirmPage=null);
public sealed record ContextGuideViewModel(string Title,string Purpose,string Saved,string Next,string Care);
public sealed record PageIntroActionViewModel(string Text, string Page, string? IconName = null);
public sealed record PageIntroViewModel(
    string Eyebrow,
    string Title,
    string Description,
    string IconName,
    string? PrimaryActionText = null,
    string? PrimaryActionPage = null,
    IReadOnlyList<PageIntroActionViewModel>? SecondaryActions = null,
    string? HelpCode = null,
    string? WhatThisPageDoes = null,
    string? NextStepText = null);
public sealed record MetricStripItemViewModel(string Label, string Value, string? Detail = null, string? IconName = null);
public sealed record MetricStripViewModel(IReadOnlyList<MetricStripItemViewModel> Items, string AccessibleLabel = "Indicadores da área");
public sealed record FilterChipViewModel(string Label, string RemoveUrl);
public sealed record FilterPanelViewModel(string Title = "Filtros", IReadOnlyList<FilterChipViewModel>? ActiveFilters = null);
public sealed record NextActionViewModel(string Title, string Reason, string ActionText, string ActionPage, string IconName = "arrow-right");
public sealed record ActionMenuItemViewModel(string Text, string Page, string? IconName = null, bool Destructive = false);
public sealed record ActionMenuViewModel(string Label, IReadOnlyList<ActionMenuItemViewModel> Items);

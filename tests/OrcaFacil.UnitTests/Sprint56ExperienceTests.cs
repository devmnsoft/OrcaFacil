using System.Text.RegularExpressions;
using Xunit;

namespace OrcaFacil.UnitTests;

internal static class Sprint56Source
{
    public static string Read(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        return File.ReadAllText(Path.Combine(new[] { root }.Concat(parts).ToArray()));
    }
}

public sealed class DesignSystemV58Tests
{
    [Fact] public void DefinesPremiumSemanticAndResponsiveContracts()
    {
        var tokens = Sprint56Source.Read("src", "OrcaFacil.Web", "wwwroot", "css", "tokens.css");
        var design = Sprint56Source.Read("src", "OrcaFacil.Web", "wwwroot", "css", "design-system.css");
        Assert.Contains("Design System V5.8", tokens);
        foreach (var contract in new[] { "metric-card", "validation-summary", "field-validation", "form-actions", "drawer", "tabs" }) Assert.Contains(contract, design);
    }
}

public sealed class FormValidationUiTests
{
    [Fact] public void LoginAndCommercialRoutineProvideVisibleValidationAndSafePosts()
    {
        var login = Sprint56Source.Read("src", "OrcaFacil.Web", "Pages", "Auth", "Login.cshtml");
        var routine = Sprint56Source.Read("src", "OrcaFacil.Web", "Pages", "CommercialRoutine", "Index.cshtml");
        Assert.Contains("asp-validation-summary", login);
        Assert.Contains("asp-validation-for", login);
        Assert.Contains("AntiForgeryToken", routine);
        Assert.Contains("data-submit-lock", routine);
        Assert.DoesNotMatch(new Regex("<button\\b(?![^>]*\\btype=)", RegexOptions.IgnoreCase), login + routine);
    }
}

public sealed class ValidationMessageCatalogTests
{
    [Fact] public void MessagesAreHumanActionableAndTechnicalDetailsAreLogged()
    {
        var catalog = Sprint56Source.Read("src", "OrcaFacil.Web", "Services", "UserFeedbackMessageCatalog.cs");
        Assert.Contains("Revise os campos destacados", catalog);
        Assert.Contains("Selecione um serviço", catalog);
        Assert.Contains("logger.LogError", catalog);
        Assert.DoesNotContain("Object reference not set", catalog, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class PopupFeedbackTests
{
    [Fact] public void ConfirmationAndToastAreAccessibleWithoutNativeDialogs()
    {
        var host = Sprint56Source.Read("src", "OrcaFacil.Web", "Pages", "Shared", "Partials", "_OverlayHost.cshtml");
        var toast = Sprint56Source.Read("src", "OrcaFacil.Web", "Pages", "Shared", "Partials", "_ToastHost.cshtml");
        var script = Sprint56Source.Read("src", "OrcaFacil.Web", "wwwroot", "js", "ui", "feedback.js");
        Assert.Contains("aria-modal=\"true\"", host);
        Assert.Contains("data-confirm-accept", host);
        Assert.Contains("aria-live=\"polite\"", toast);
        Assert.DoesNotContain("window.confirm(", script);
    }
}

public sealed class DocumentsNewPremiumStructureTests
{
    [Fact] public void GuidedStartUsesSemanticCommandsAndUsefulEmptyStates()
    {
        var page = Sprint56Source.Read("src", "OrcaFacil.Web", "Pages", "Documents", "New.cshtml");
        Assert.Contains("data-scroll-target", page);
        Assert.Contains("of-start-empty", page);
        Assert.DoesNotContain("href=\"#", page);
    }
}

public sealed class CommercialRoutinePremiumStructureTests
{
    [Fact] public void CriticalActionRequiresConfirmationAndPreservesHistory()
    {
        var page = Sprint56Source.Read("src", "OrcaFacil.Web", "Pages", "CommercialRoutine", "Index.cshtml");
        Assert.Contains("data-confirm=", page);
        Assert.Contains("histórico será preservado", page);
        Assert.Contains("of-empty-state", page);
    }
}

public sealed class MobileLayoutSmokeTests
{
    [Fact] public void MobileContractsCoverSmallAndWideViewports()
    {
        var css = Sprint56Source.Read("src", "OrcaFacil.Web", "wwwroot", "css", "mobile.css") + Sprint56Source.Read("src", "OrcaFacil.Web", "wwwroot", "css", "responsive.css");
        foreach (var width in new[] { "320px", "360px", "390px", "430px", "768px", "1024px", "1440px", "1920px" }) Assert.Contains(width, css);
    }
}

using Xunit;

namespace OrcaFacil.UnitTests;

internal static class Sprint57Source
{
    public static string Read(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        return File.ReadAllText(Path.Combine(new[] { root }.Concat(parts).ToArray()));
    }
}

public sealed class DesignSystemV59Tests
{
    [Fact]
    public void LayoutsUseTheUnifiedAccessibleFeedbackContract()
    {
        var authenticated = Sprint57Source.Read("src", "OrcaFacil.Web", "Pages", "Shared", "_Layout.cshtml");
        var publicLayout = Sprint57Source.Read("src", "OrcaFacil.Web", "Pages", "Shared", "_PublicLayout.cshtml");
        foreach (var contract in new[] { "_ToastHost", "_ConfirmDialog", "~/js/feedback.js", "~/js/dialogs.js", "~/js/forms.js" })
        {
            Assert.Contains(contract, authenticated);
            Assert.Contains(contract, publicLayout);
        }
    }
}

public sealed class ConfirmDialogTests
{
    [Fact]
    public void ConfirmationHasAccessibleSemanticsAndKeyboardFocusContainment()
    {
        var host = Sprint57Source.Read("src", "OrcaFacil.Web", "Pages", "Shared", "Partials", "_OverlayHost.cshtml");
        var dialogs = Sprint57Source.Read("src", "OrcaFacil.Web", "wwwroot", "js", "dialogs.js");
        Assert.Contains("aria-modal=\"true\"", host);
        Assert.Contains("data-confirm-accept", host);
        Assert.Contains("event.key !== 'Tab'", dialogs);
        Assert.Contains("document.activeElement", dialogs);
    }
}

public sealed class ToastHostTests
{
    [Fact]
    public void ToastsAreLiveDismissibleAndMobileSafe()
    {
        var host = Sprint57Source.Read("src", "OrcaFacil.Web", "Pages", "Shared", "Partials", "_ToastHost.cshtml");
        var css = Sprint57Source.Read("src", "OrcaFacil.Web", "wwwroot", "css", "feedback.css");
        Assert.Contains("aria-live=\"polite\"", host);
        Assert.Contains("type=\"button\"", host);
        Assert.Contains("safe-area-inset-top", css);
        Assert.Contains("prefers-reduced-motion", css);
    }
}

public sealed class LoadingStateTests
{
    [Fact]
    public void SubmitLockValidatesAndPreventsDuplicateSubmission()
    {
        var forms = Sprint57Source.Read("src", "OrcaFacil.Web", "wwwroot", "js", "forms.js");
        Assert.Contains("form.checkValidity()", forms);
        Assert.Contains("event.preventDefault()", forms);
        Assert.Contains("aria-busy", forms);
        Assert.Contains("event.submitter", forms);
    }
}

using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using OrcaFacil.Web.ViewModels.Components;

namespace OrcaFacil.Web.Extensions;

public static class TempDataNotificationExtensions
{
    private const string Key = "OrcaFacil.Toasts";

    public static void Success(this ITempDataDictionary tempData, string message) => tempData.AddToast("success", message);
    public static void Success(this ITempDataDictionary tempData, string title, string message)
    {
        var toasts = tempData.ReadToasts().ToList();
        toasts.Add(new ToastViewModel("success", message, "bi-check-circle", title));
        tempData[Key] = JsonSerializer.Serialize(toasts);
    }
    public static void Info(this ITempDataDictionary tempData, string message) => tempData.AddToast("info", message);
    public static void Warning(this ITempDataDictionary tempData, string message) => tempData.AddToast("warning", message);
    public static void Error(this ITempDataDictionary tempData, string message) => tempData.AddToast("danger", message);

    private static void AddToast(this ITempDataDictionary tempData, string type, string message)
    {
        var toasts = tempData.ReadToasts().ToList();
        toasts.Add(ToastViewModel.Create(type, message));
        tempData[Key] = JsonSerializer.Serialize(toasts);
    }

    public static IReadOnlyList<ToastViewModel> ReadToasts(this ITempDataDictionary tempData)
    {
        if (tempData[Key] is not string json || string.IsNullOrWhiteSpace(json)) return Array.Empty<ToastViewModel>();
        try { return JsonSerializer.Deserialize<ToastViewModel[]>(json) ?? Array.Empty<ToastViewModel>(); }
        catch { return Array.Empty<ToastViewModel>(); }
    }
}

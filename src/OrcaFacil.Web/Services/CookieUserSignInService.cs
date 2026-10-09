using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.DTOs;
using OrcaFacil.Application.Localization;
using OrcaFacil.Persistence;

namespace OrcaFacil.Web.Services;

public sealed record UserSignInResult(AccountSelection? Account, bool HasMultipleAccounts);

public interface IUserSignInService
{
    Task<UserSignInResult> SignInAsync(HttpContext context, UserSummaryDto user,
        Guid? preferredAccountId = null, bool persistent = false, CancellationToken cancellationToken = default);
}

public sealed class CookieUserSignInService(
    IAccountSelectionService accountSelection,
    OrcaFacilDbContext db,
    ILogger<CookieUserSignInService> logger) : IUserSignInService
{
    public async Task<UserSignInResult> SignInAsync(HttpContext context, UserSummaryDto user,
        Guid? preferredAccountId = null, bool persistent = false, CancellationToken cancellationToken = default)
    {
        var (account, availableAccounts) = await accountSelection.SelectAsync(user.Id, preferredAccountId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var claims = new List<Claim>
        {
            new("sub", user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("user_id", user.Id.ToString()),
            new("session_version", user.SessionVersion.ToString(CultureInfo.InvariantCulture)),
            new("authentication_time", now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture))
        };

        // A user with more than one tenant must make an explicit choice. Do not
        // leak a default AccountId into the authentication ticket.
        var selectedAccount = availableAccounts > 1 && preferredAccountId is null ? null : account;
        if (selectedAccount is not null)
        {
            claims.AddRange([
                new Claim("account_id", selectedAccount.AccountId.ToString()),
                new Claim("account_member_id", selectedAccount.AccountMemberId.ToString()),
                new Claim("account_role", selectedAccount.Role),
                new Claim("account_status", selectedAccount.Status),
                new Claim("effective_plan_code", selectedAccount.EffectivePlanCode)
            ]);
        }

        var properties = new AuthenticationProperties
        {
            AllowRefresh = true,
            IsPersistent = persistent,
            IssuedUtc = now,
            ExpiresUtc = persistent ? now.AddDays(14) : null
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties);

        // Restaura a preferência de idioma persistida do usuário ou padrão da conta
        try
        {
            var userPref = await db.Database.SqlQueryRaw<string>(
                "SELECT language_code FROM orcafacil.user_locale_preferences WHERE user_id = {0}", user.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(userPref) && selectedAccount is not null)
            {
                userPref = await db.Database.SqlQueryRaw<string>(
                    "SELECT language_code FROM orcafacil.account_locale_settings WHERE account_id = {0}", selectedAccount.AccountId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(userPref))
            {
                var normalizedPref = SupportedLocales.Normalize(userPref);
                context.Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(normalizedPref)),
                    new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddYears(1),
                        HttpOnly = true,
                        IsEssential = true,
                        SameSite = SameSiteMode.Lax,
                        Secure = context.Request.IsHttps
                    });
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Preferência de idioma não recuperada para UserId {UserId}", user.Id);
        }

        logger.LogInformation("USER_SIGNED_IN UserId {UserId} AccountId {AccountId} MultipleAccounts {MultipleAccounts}",
            user.Id, selectedAccount?.AccountId, availableAccounts > 1);
        return new UserSignInResult(selectedAccount, availableAccounts > 1);
    }
}

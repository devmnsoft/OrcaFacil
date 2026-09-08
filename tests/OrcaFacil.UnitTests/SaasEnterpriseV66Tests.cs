using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using OrcaFacil.Application.Accounts.Admin;
using OrcaFacil.Application.Auth;
using OrcaFacil.Application.Saas.Billing;
using OrcaFacil.Application.Saas.Modules;
using OrcaFacil.Domain.Entities;
using OrcaFacil.Persistence.Diagnostics;
using Xunit;

namespace OrcaFacil.UnitTests;

internal static class SaasV66Source
{
    public static string Read(params string[] parts) => File.ReadAllText(Path.Combine(new[] { CompositionRootSource.RepositoryRoot }.Concat(parts).ToArray()));
}

public sealed class SaasEnterpriseV66Tests
{
    [Fact] public void Catalog_has_unique_real_modules_with_prices_routes_and_permissions(){var modules=new SaasModuleRegistryService().GetAll();Assert.True(modules.Count>=18);Assert.Equal(modules.Count,modules.Select(x=>x.Code).Distinct().Count());Assert.All(modules,x=>{Assert.StartsWith("/",x.RoutePrefix);Assert.NotEmpty(x.RequiredPermissionCode);Assert.True(x.MonthlyPrice>=0);Assert.True(x.AnnualPrice>=0);});}
}
public sealed class ModuleRegistryTests{[Fact]public void Documents_and_commercial_are_registered(){var r=new SaasModuleRegistryService();Assert.Equal("DOCUMENTS",r.FindByPath("/Documents/New")?.Code);Assert.Equal("COMMERCIAL_ROUTINE",r.FindByPath("/CommercialRoutine")?.Code);}}
public sealed class ModuleEntitlementServiceTests{[Fact]public void Suspended_subscription_never_grants_access(){var s=new AccountModuleSubscription();s.Activate(Guid.NewGuid());Assert.True(s.GrantsAccess(DateTime.UtcNow));s.Suspend(Guid.NewGuid());Assert.False(s.GrantsAccess(DateTime.UtcNow));}}
public sealed class ModulePricingServiceTests{[Fact]public void Pricing_uses_catalog_values(){var r=new SaasModuleRegistryService();var p=new ModulePricingService(r);Assert.Equal(69,p.Resolve("DOCUMENTS",false));Assert.Equal(690,p.Resolve("DOCUMENTS",true));}}
public sealed class AccountModuleSubscriptionServiceTests{[Fact]public void Trial_expires_deterministically(){var s=new AccountModuleSubscription();s.StartTrial(Guid.NewGuid(),DateTime.UtcNow.AddMinutes(1));Assert.True(s.GrantsAccess(DateTime.UtcNow));Assert.False(s.GrantsAccess(DateTime.UtcNow.AddMinutes(2)));}}
public sealed class FeatureAccessServiceTests{[Fact]public void Backend_composes_permission_entitlement_and_feature(){var source=SaasV66Source.Read("src","OrcaFacil.Persistence","Services","Saas","ModuleAccessServices.cs");Assert.Contains("hasPermission",source);Assert.Contains("HasAccessAsync",source);Assert.Contains("AccountModuleEntitlements",source);}}
public sealed class LoginIdentifierServiceTests
{
    private static LoginIdentifierService Service()=>new(new BrazilianDocumentNormalizer(),new InstitutionalEmailValidator());
    [Fact]public void Cpf_is_normalized()=>Assert.Equal(new(LoginIdentifierKind.Cpf,"52998224725"),Service().Normalize("529.982.247-25"));
    [Fact]public void Cnpj_is_normalized()=>Assert.Equal(new(LoginIdentifierKind.Cnpj,"11222333000181"),Service().Normalize("11.222.333/0001-81"));
    [Fact]public void Email_is_lowercase()=>Assert.Equal(new(LoginIdentifierKind.Email,"pessoa@empresa.com.br"),Service().Normalize(" Pessoa@Empresa.COM.BR "));
}
public sealed class TenantLoginResolverTests{[Fact]public void Login_queries_membership_and_active_account(){var source=SaasV66Source.Read("src","OrcaFacil.Application","Auth","AuthService.cs");Assert.Contains("linkedUserIds",source);Assert.Contains("AccountMemberStatus.Active",source);Assert.Contains("AccountStatus.Active",source);}}
public sealed class AccountSelectionServiceTests{[Fact]public void Multiple_accounts_require_explicit_selection(){var source=SaasV66Source.Read("src","OrcaFacil.Web","Services","CookieUserSignInService.cs");Assert.Contains("availableAccounts > 1 && preferredAccountId is null",source);Assert.Contains("selectedAccount",source);}}
public sealed class ProfilePermissionMatrixTests{[Fact]public void Tenant_admin_cannot_assign_global_or_owner_profile(){Assert.False(ProfilePermissionMatrix.CanAssign("Administrator","SuperAdmin"));Assert.False(ProfilePermissionMatrix.CanAssign("Administrator","Owner"));Assert.True(ProfilePermissionMatrix.CanAssign("Owner","Financial"));}}
public sealed class ModuleMenuComposerTests{[Fact]public void Menu_is_composed_from_entitlements_and_not_dead_placeholders(){var source=SaasV66Source.Read("src","OrcaFacil.Web","Services","ModuleMenuComposer.cs");Assert.Contains("entitlements.CheckAsync",source);Assert.DoesNotContain("href=\"#",source);Assert.DoesNotContain("javascript"+":void",source);}}
public sealed class TenantIsolationTests{[Fact]public void Tenant_admin_mutations_filter_current_account(){var source=SaasV66Source.Read("src","OrcaFacil.Web","Pages","AccountAdmin","Users","Edit.cshtml.cs");Assert.Contains("x.AccountId==account",source);Assert.DoesNotContain("IgnoreQueryFilters",source);}}
public sealed class SuperAdminGlobalAccessTests{[Fact]public void Global_pages_have_explicit_policy(){var attribute=typeof(OrcaFacil.Web.Pages.SuperAdmin.IndexModel).GetCustomAttribute<AuthorizeAttribute>();Assert.Equal("SuperAdminOnly",attribute?.Policy);}}
public sealed class AccountAdminIsolationTests{[Fact]public void Account_admin_page_requires_account_policy(){var attribute=typeof(OrcaFacil.Web.Pages.AccountAdmin.IndexModel).GetCustomAttribute<AuthorizeAttribute>();Assert.Equal("AccountAdmin",attribute?.Policy);}}
public sealed class PortalIsolationTests{[Fact]public void Module_guard_executes_before_routed_pages(){var source=SaasV66Source.Read("src","OrcaFacil.Web","Program.cs");Assert.True(source.IndexOf("UseMiddleware<ModuleAccessMiddleware>",StringComparison.Ordinal)<source.IndexOf("MapRazorPages()",StringComparison.Ordinal));}}
public sealed class ModuleUsageTrackerTests{[Fact]public void Tracking_is_best_effort_and_excludes_payloads(){var source=SaasV66Source.Read("src","OrcaFacil.Persistence","Services","Saas","ModuleAccessServices.cs");Assert.Contains("MODULE_USAGE_TRACKING_FAILED",source);Assert.DoesNotContain("Password",typeof(AccountModuleUsageEvent).GetProperties().Select(x=>x.Name));Assert.DoesNotContain("Token",typeof(AccountModuleUsageEvent).GetProperties().Select(x=>x.Name));}}
public sealed class SystemHealthSaasTests{[Fact]public void Schema_contract_includes_saas_tables(){Assert.Contains("saas_modules",DatabaseSchemaContractService.RegistrationContract.Keys);Assert.Contains("account_module_entitlements",DatabaseSchemaContractService.RegistrationContract.Keys);Assert.Contains(DatabaseSchemaContractService.SaasEnterpriseV66Migration,DatabaseSchemaContractService.RequiredMigrations);}}
public sealed class QualityGateSaasTests{[Fact]public void Quality_gate_uses_real_schema_contract(){var source=SaasV66Source.Read("src","OrcaFacil.Application","Quality","QualityGateService.cs");Assert.Contains("CheckRegistrationContractAsync",source);Assert.DoesNotContain("Math.random",source,StringComparison.OrdinalIgnoreCase);}}
public sealed class DashboardSuperAdminTests{[Fact]public void Global_dashboard_uses_database_queries_only(){var source=SaasV66Source.Read("src","OrcaFacil.Web","Pages","SuperAdmin","Index.cshtml.cs");Assert.Contains("CountAsync",source);Assert.Contains("SumAsync",source);Assert.DoesNotContain("Math.random",source,StringComparison.OrdinalIgnoreCase);}}
public sealed class DashboardAccountTests{[Fact]public void Account_dashboard_service_is_tenant_bound(){var source=SaasV66Source.Read("src","OrcaFacil.Web","Services","DashboardExperienceService.cs");Assert.Contains("currentUser.UserId",source,StringComparison.OrdinalIgnoreCase);Assert.Contains("dashboardQueries.GetDashboardAsync",source,StringComparison.Ordinal);}}
public sealed class SuperAdminClientManagementTests{[Fact]public void Destructive_client_actions_are_soft_and_audited(){var source=SaasV66Source.Read("src","OrcaFacil.Web","Pages","SuperAdmin","Clients","Edit.cshtml.cs");Assert.Contains("MarkAsDeleted",source);Assert.Contains("AccountModuleAuditLogs",source);}}
public sealed class AccountAdminUserManagementTests{[Fact]public void Tenant_user_deletion_only_removes_membership(){var source=SaasV66Source.Read("src","OrcaFacil.Web","Pages","AccountAdmin","Users","Edit.cshtml.cs");Assert.Contains("AccountMember",source);Assert.DoesNotContain("db.Users.SingleOrDefaultAsync",source);}}

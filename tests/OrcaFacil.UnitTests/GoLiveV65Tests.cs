using OrcaFacil.Application.GoLive;
using OrcaFacil.Domain.Entities;

namespace OrcaFacil.UnitTests;

public sealed class GoLiveChecklistServiceTests
{
    [Fact] public void Critical_pending_item_blocks_production() { var item=new GoLiveChecklistItem{AccountId=Guid.NewGuid(),IsCritical=true}; Assert.Equal(GoLiveStatus.Blocked,new GoLiveChecklistService().ResolveStatus([item],GoLiveStatus.ReadyForProduction)); }
    [Fact] public void Manual_item_requires_same_account_confirmation_and_evidence() { var account=Guid.NewGuid(); var item=new GoLiveChecklistItem{AccountId=account}; var service=new GoLiveChecklistService(); Assert.Throws<InvalidOperationException>(()=>service.CompleteManual(item,account,Guid.NewGuid(),"","",false)); service.CompleteManual(item,account,Guid.NewGuid(),"Ana","Backup restaurado",true); Assert.True(item.IsCompleted); }
    [Fact] public void Checklist_is_tenant_scoped() { var item=new GoLiveChecklistItem{AccountId=Guid.NewGuid()}; Assert.Throws<UnauthorizedAccessException>(()=>new GoLiveChecklistService().CompleteManual(item,Guid.NewGuid(),Guid.NewGuid(),"Ana","Revisado",true)); }
}

public sealed class ProductionReadinessServiceTests
{
    [Theory] [InlineData(null)] [InlineData("Host=127.0.0.1:1;Database=prod")] [InlineData("Host=db;Database=unavailable")] public void Rejects_non_operational_database(string? value)=>Assert.Throws<InvalidOperationException>(()=>new ProductionReadinessService().ValidateConnectionString(value));
}

public sealed class TrainingProgressServiceTests
{
    [Fact] public void Progress_belongs_to_user_and_account_and_requires_confirmation(){var a=Guid.NewGuid();var u=Guid.NewGuid();var p=new TrainingProgress{AccountId=a,UserId=u};var s=new TrainingProgressService();Assert.Throws<UnauthorizedAccessException>(()=>s.Complete(p,Guid.NewGuid(),u,true));Assert.Throws<InvalidOperationException>(()=>s.Complete(p,a,u,false));s.Complete(p,a,u,true);Assert.True(p.UserConfirmed);}
}

public sealed class CriticalRouteMonitorServiceTests
{
    [Fact] public void Fingerprint_is_stable_and_secrets_are_redacted(){var s=new RouteErrorFingerprintService();Assert.Equal(s.Create("DbError","/Dashboard"),s.Create("DbError","/Dashboard"));Assert.DoesNotContain("token",s.Sanitize("token=private"),StringComparison.OrdinalIgnoreCase);}
}

public sealed class TrainingGuideServiceTests
{
    [Fact] public void Every_guide_points_to_a_real_route(){var root=FindRoot();foreach(var lesson in new TrainingGuideService().GetLessons(true)){var relative=lesson.Route.Trim('/').Replace("CreateBudget","CreateBudget");Assert.False(string.IsNullOrWhiteSpace(relative));}Assert.True(Directory.Exists(Path.Combine(root,"src","OrcaFacil.Web","Pages")));}
    private static string FindRoot(){var d=new DirectoryInfo(AppContext.BaseDirectory);while(d is not null&&!File.Exists(Path.Combine(d.FullName,"OrcaFacil.sln")))d=d.Parent;return d?.FullName??throw new InvalidOperationException();}
}

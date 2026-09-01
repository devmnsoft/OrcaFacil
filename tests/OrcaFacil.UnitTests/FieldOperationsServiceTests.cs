using OrcaFacil.Application.Field;
using Xunit;

namespace OrcaFacil.UnitTests;

public sealed class FieldTeamServiceTests
{
    [Fact] public void Inactive_technician_cannot_receive_work() =>
        Assert.False(new FieldTeamService().CanAssign(true, new(Guid.NewGuid(), Guid.NewGuid(), FieldTechnicianStatus.Inactive, 5, 0)).Succeeded);
}

public sealed class FieldDispatchServiceTests
{
    [Fact] public void Work_order_without_client_is_rejected()
    {
        var account = Guid.NewGuid();
        Assert.False(new FieldDispatchService().Validate(FieldFactory.Order(account) with { ClientId = null }, account).Succeeded);
    }
}

public sealed class FieldScheduleServiceTests
{
    [Fact] public void Work_order_without_dates_is_rejected() =>
        Assert.False(new FieldScheduleService().Validate(FieldFactory.Order(Guid.NewGuid()), []).Succeeded);
}

public sealed class FieldRouteServiceTests
{
    [Fact] public void Route_requires_an_assignee() =>
        Assert.False(new FieldRouteService().Validate(Guid.Empty, Guid.Empty, null, null).Succeeded);
}

public sealed class FieldVisitSessionServiceTests
{
    [Fact] public void Duplicate_check_in_is_rejected()
    {
        var account = Guid.NewGuid(); var technician = Guid.NewGuid(); var order = FieldFactory.Order(account, technicianId: technician);
        var open = new FieldVisitSession(Guid.NewGuid(), account, order.Id, technician, DateTime.UtcNow);
        Assert.False(new FieldVisitSessionService().CanCheckIn(order, account, technician, [open]).Succeeded);
    }

    [Fact] public void Checkout_requires_mandatory_checklist()
    {
        var account = Guid.NewGuid(); var technician = Guid.NewGuid();
        var order = FieldFactory.Order(account, technicianId: technician) with { RequiresChecklist = true, ChecklistComplete = false };
        var open = new FieldVisitSession(Guid.NewGuid(), account, order.Id, technician, DateTime.UtcNow);
        Assert.False(new FieldVisitSessionService().CanCheckOut(order, open, FieldVisitOutcome.Completed).Succeeded);
    }
}

public sealed class FieldEvidenceServiceTests
{
    [Fact] public void Executable_evidence_is_rejected() => Assert.False(new FieldVisitEvidenceService().Validate("payload.exe", 100).Succeeded);
}

public sealed class FieldSignatureServiceTests
{
    [Fact] public void Signature_requires_consent() => Assert.False(new FieldVisitSignatureService().Validate("Cliente", false, new byte[] { 1 }).Succeeded);
}

public sealed class FieldMaterialUsageServiceTests
{
    [Fact] public void Negative_stock_is_blocked_by_policy() => Assert.False(new FieldMaterialUsageService().Validate(2, 1, false).Succeeded);
}

public sealed class FieldTimeEntryServiceTests
{
    [Fact] public void A_user_cannot_start_two_timers()
    {
        var account = Guid.NewGuid(); var user = Guid.NewGuid();
        Assert.False(new FieldTimeEntryService().CanStart(account, user, [(account, user, null)]).Succeeded);
    }
}

public sealed class FieldOfflineSyncServiceTests
{
    [Fact] public void Same_idempotency_key_does_not_duplicate()
    {
        var account = Guid.NewGuid(); var item = new OfflineQueueItem(account, "visit-1", "checklist", "sha256:a", OfflineSyncState.Applied);
        Assert.Equal(OfflineSyncState.Applied, new FieldOfflineSyncService().Resolve(item, [item]));
    }

    [Fact] public void Changed_payload_is_a_traceable_conflict()
    {
        var account = Guid.NewGuid(); var old = new OfflineQueueItem(account, "visit-1", "checklist", "sha256:a", OfflineSyncState.Applied);
        Assert.Equal(OfflineSyncState.Conflict, new FieldOfflineSyncService().Resolve(old with { PayloadHash = "sha256:b" }, [old]));
    }
}

public sealed class FieldPortalIsolationTests
{
    [Fact] public void Customer_cannot_read_another_customers_work_order() =>
        Assert.False(new FieldPortalIsolationService().CanRead(Guid.Empty, Guid.Empty, Guid.NewGuid(), Guid.NewGuid()));
}

public sealed class FieldQualityReviewServiceTests
{
    [Fact] public void Rejection_requires_reason() =>
        Assert.False(new FieldQualityReviewService().Review(Guid.Empty, Guid.Empty, false, null).Succeeded);
}

public sealed class FieldReportServiceTests
{
    [Fact] public void Report_isolated_by_account_and_uses_real_rows()
    {
        var account = Guid.NewGuid();
        var result = new FieldReportService().CountByOutcome(account, [(account, "Completed"), (Guid.NewGuid(), "Completed")]);
        Assert.Equal(1, result["Completed"]);
    }
}

internal static class FieldFactory
{
    internal static FieldWorkOrder Order(Guid account, Guid? technicianId = default) =>
        new(Guid.NewGuid(), account, Guid.NewGuid(), technicianId, null, null, null, "Rua A", false, false, false, false, false, false);
}

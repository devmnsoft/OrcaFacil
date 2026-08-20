using OrcaFacil.Domain.Common;

namespace OrcaFacil.Domain.Entities;

public enum JobExecutionStatus { Running, Succeeded, Failed, Canceled, Skipped }
public enum OutboxStatus { Pending, Processing, Completed, Failed, Canceled }

public sealed class BackgroundJob : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Schedule { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; } = true;
    public DateTime? NextExecutionAt { get; private set; }
    private BackgroundJob() { }
    public BackgroundJob(string name, string schedule, DateTime? nextExecutionAt = null)
    { Name = name; Schedule = schedule; NextExecutionAt = nextExecutionAt; }
}

public sealed class JobExecution : Entity
{
    public string JobName { get; private set; } = string.Empty;
    public string ExecutionId { get; private set; } = string.Empty;
    public string InstanceId { get; private set; } = string.Empty;
    public JobExecutionStatus Status { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }
    public long? DurationMilliseconds { get; private set; }
    public string? ErrorSummary { get; private set; }
    private JobExecution() { }
    public JobExecution(string jobName, string executionId, string instanceId)
    { JobName = jobName; ExecutionId = executionId; InstanceId = instanceId; Status = JobExecutionStatus.Running; StartedAt = DateTime.UtcNow; }
    public void Finish(JobExecutionStatus status, string? safeError = null)
    { Status = status; FinishedAt = DateTime.UtcNow; DurationMilliseconds = (long)(FinishedAt.Value - StartedAt).TotalMilliseconds; ErrorSummary = Sanitize(safeError); Touch(); }
    private static string? Sanitize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Replace("\r", " ").Replace("\n", " ")[..Math.Min(value.Length, 500)];
}

public sealed class JobLock : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string? LockedBy { get; private set; }
    public DateTime? LockedUntil { get; private set; }
    public DateTime? AcquiredAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }
    private JobLock() { }
    public JobLock(string name) { Name = name; }
    public bool TryAcquire(string instanceId, DateTime now, TimeSpan lease)
    {
        if (LockedUntil > now && ReleasedAt is null) return false;
        LockedBy = instanceId; AcquiredAt = now; LockedUntil = now.Add(lease); ReleasedAt = null; Touch(); return true;
    }
    public bool Release(string instanceId, DateTime now)
    { if (LockedBy != instanceId || ReleasedAt is not null) return false; ReleasedAt = now; LockedUntil = now; Touch(); return true; }
}

public sealed class ProcessingOutboxItem : Entity
{
    public Guid AccountId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = "{}";
    public OutboxStatus Status { get; private set; } = OutboxStatus.Pending;
    public int Priority { get; private set; }
    public int Attempts { get; private set; }
    public int MaximumAttempts { get; private set; } = 5;
    public DateTime NextAttemptAt { get; private set; }
    public DateTime? ProcessingStartedAt { get; private set; }
    public string? ProcessingInstanceId { get; private set; }
    public string? LastError { get; private set; }
    private ProcessingOutboxItem() { }
    public ProcessingOutboxItem(Guid accountId, string type, string idempotencyKey, string payloadJson, int priority = 0, int maximumAttempts = 5)
    { if (accountId == Guid.Empty) throw new ArgumentException("AccountId é obrigatório."); AccountId = accountId; Type = type; IdempotencyKey = idempotencyKey; PayloadJson = payloadJson; Priority = priority; MaximumAttempts = Math.Clamp(maximumAttempts, 1, 20); NextAttemptAt = DateTime.UtcNow; }
    public bool Start(string instanceId, DateTime now)
    { if (Status is OutboxStatus.Completed or OutboxStatus.Canceled || Attempts >= MaximumAttempts || NextAttemptAt > now) return false; Status = OutboxStatus.Processing; Attempts++; ProcessingStartedAt = now; ProcessingInstanceId = instanceId; Touch(); return true; }
    public void Complete() { if (Status != OutboxStatus.Processing) return; Status = OutboxStatus.Completed; LastError = null; Touch(); }
    public void Fail(string safeError, DateTime nextAttemptAt)
    { if (Status != OutboxStatus.Processing) return; LastError = safeError.Replace("\r", " ").Replace("\n", " ")[..Math.Min(safeError.Length, 500)]; Status = Attempts >= MaximumAttempts ? OutboxStatus.Failed : OutboxStatus.Pending; NextAttemptAt = nextAttemptAt; ProcessingInstanceId = null; Touch(); }
    public bool RequeueFailed(DateTime now) { if (Status != OutboxStatus.Failed) return false; Attempts = 0; Status = OutboxStatus.Pending; NextAttemptAt = now; LastError = null; Touch(); return true; }
}

public sealed class SystemMetric : Entity { public Guid? AccountId { get; private set; } public string Name { get; private set; } = string.Empty; public double Value { get; private set; } public DateTime PeriodStart { get; private set; } public DateTime PeriodEnd { get; private set; } private SystemMetric() { } public SystemMetric(Guid? accountId, string name, double value, DateTime start, DateTime end) { AccountId=accountId; Name=name; Value=value; PeriodStart=start; PeriodEnd=end; } }
public sealed class SlowQueryLog : Entity { public Guid? AccountId { get; private set; } public Guid? UserId { get; private set; } public string Route { get; private set; }=string.Empty; public string Operation { get; private set; }=string.Empty; public long ElapsedMilliseconds { get; private set; } public string CorrelationId { get; private set; }=string.Empty; public string Summary { get; private set; }=string.Empty; private SlowQueryLog(){} public SlowQueryLog(Guid? accountId, Guid? userId, string route, string operation, long elapsed, string correlationId, string summary){AccountId=accountId;UserId=userId;Route=route;Operation=operation;ElapsedMilliseconds=elapsed;CorrelationId=correlationId;Summary=summary;} }
public sealed class TenantUsageMetric : Entity { public Guid AccountId { get; private set; } public string Resource { get; private set; }=string.Empty; public long Used { get; private set; } public long? Limit { get; private set; } public DateTime PeriodStart { get; private set; } private TenantUsageMetric(){} public TenantUsageMetric(Guid accountId,string resource,long used,long? limit,DateTime periodStart){AccountId=accountId;Resource=resource;Used=used;Limit=limit;PeriodStart=periodStart;} }
public sealed class CacheInvalidationEvent : Entity { public Guid AccountId { get; private set; } public string CacheRegion { get; private set; }=string.Empty; public string Reason { get; private set; }=string.Empty; private CacheInvalidationEvent(){} public CacheInvalidationEvent(Guid accountId,string region,string reason){AccountId=accountId;CacheRegion=region;Reason=reason;} }
public sealed class QuotaEvent : Entity { public Guid AccountId { get; private set; } public string Resource { get; private set; }=string.Empty; public long Used { get; private set; } public long Limit { get; private set; } public bool Blocked { get; private set; } private QuotaEvent(){} public QuotaEvent(Guid accountId,string resource,long used,long limit,bool blocked){AccountId=accountId;Resource=resource;Used=used;Limit=limit;Blocked=blocked;} }
public sealed class RateLimitEvent : Entity { public Guid? AccountId { get; private set; } public string Policy { get; private set; }=string.Empty; public string ClientFingerprint { get; private set; }=string.Empty; public string Route { get; private set; }=string.Empty; private RateLimitEvent(){} public RateLimitEvent(Guid? accountId,string policy,string fingerprint,string route){AccountId=accountId;Policy=policy;ClientFingerprint=fingerprint;Route=route;} }
public sealed class WorkerHeartbeat : Entity { public string InstanceId { get; private set; }=string.Empty; public DateTime LastSeenAt { get; private set; } public string Status { get; private set; }="Healthy"; private WorkerHeartbeat(){} public WorkerHeartbeat(string instanceId,DateTime at){InstanceId=instanceId;LastSeenAt=at;} }

using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.DTOs;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Persistence.Queries;

public class SuperAdminDashboardQueries
{
    private const decimal DefaultProMonthlyPrice = 19.90m;
    private readonly OrcaFacilDbContext _db;

    public SuperAdminDashboardQueries(OrcaFacilDbContext db) => _db = db;

    public async Task<SuperAdminDashboardDto> GetAsync(CancellationToken ct = default)
    {
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var users = ActiveRows(_db.Users);
        var subscriptions = ActiveRows(_db.Subscriptions);
        var documents = ActiveRows(_db.Documents);
        var clients = ActiveRows(_db.Clients);
        var payments = ActiveRows(_db.Payments);

        var dto = new SuperAdminDashboardDto
        {
            TotalUsers = await users.CountAsync(ct),
            ActiveUsers = await users.CountAsync(x => x.IsActive && !x.IsBlocked, ct),
            BlockedUsers = await users.CountAsync(x => x.IsBlocked, ct),
            FreeUsers = await users.CountAsync(x => x.Plan == PlanType.Free, ct),
            ProUsers = await users.CountAsync(x => x.Plan == PlanType.Pro, ct),
            PastDueUsers = await subscriptions.CountAsync(x => x.Status == SubscriptionStatus.PastDue, ct),
            SuspendedUsers = await subscriptions.CountAsync(x => x.Status == SubscriptionStatus.Suspended, ct),
            TotalClients = await clients.CountAsync(ct),
            TotalDocuments = await documents.CountAsync(ct),
            TotalBudgets = await documents.CountAsync(x => x.Type == DocumentType.Budget, ct),
            TotalReceipts = await documents.CountAsync(x => x.Type == DocumentType.Receipt, ct),
            TotalPayments = await payments.CountAsync(ct),
            ApprovedPaymentsAmountMonth = await SumPaymentsAsync(payments.Where(x => x.Status == PaymentStatus.Approved && x.CreatedAt >= monthStart), ct),
            PendingPaymentsAmount = await SumPaymentsAsync(payments.Where(x => x.Status == PaymentStatus.Pending), ct),
            OverduePaymentsAmount = await SumPaymentsAsync(payments.Where(x => x.Status == PaymentStatus.Expired), ct),
            RecentPayments = await GetRecentPaymentsAsync(payments, ct),
            RecentErrors = await ActiveRows(_db.SystemErrors).OrderByDescending(x => x.CreatedAt).Take(5).Select(x => x.Message).ToListAsync(ct)
        };

        dto.TrialUsers = await subscriptions.CountAsync(x => x.Status == SubscriptionStatus.Trial, ct);
        dto.MrrEstimate = await CalculateMrrAsync(subscriptions, ct);
        dto.RecentUsers = await GetRecentUsersAsync(users, documents, clients, ct);
        return dto;
    }

    private static IQueryable<T> ActiveRows<T>(DbSet<T> set) where T : Domain.Common.Entity =>
        set.AsNoTracking().Where(x => !x.IsDeleted);

    private static async Task<decimal> SumPaymentsAsync(IQueryable<Domain.Entities.Payment> query, CancellationToken ct) =>
        await query.SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;

    private static async Task<decimal> CalculateMrrAsync(IQueryable<Domain.Entities.Subscription> subscriptions, CancellationToken ct)
    {
        var activeSubscriptions = await subscriptions
            .Where(x => x.Status == SubscriptionStatus.Active || x.Status == SubscriptionStatus.ManualRelease)
            .Select(x => x.Amount)
            .ToListAsync(ct);

        return activeSubscriptions.Count == 0 ? 0 : activeSubscriptions.Sum(x => x > 0 ? x : DefaultProMonthlyPrice);
    }

    private static async Task<List<SuperAdminUserRowDto>> GetRecentUsersAsync(
        IQueryable<Domain.Entities.UserAccount> users,
        IQueryable<Domain.Entities.Document> documents,
        IQueryable<Domain.Entities.Client> clients,
        CancellationToken ct)
    {
        var recentUsers = await users.OrderByDescending(x => x.CreatedAt).Take(8).Select(x => new
        {
            x.Id,
            x.Name,
            x.Email,
            x.Plan,
            x.LastLoginAt,
            x.IsBlocked
        }).ToListAsync(ct);

        if (recentUsers.Count == 0) return [];

        var userIds = recentUsers.Select(x => x.Id).ToList();
        var documentCounts = await documents.Where(x => userIds.Contains(x.UserId)).GroupBy(x => x.UserId).Select(x => new { UserId = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.UserId, x => x.Count, ct);
        var clientCounts = await clients.Where(x => userIds.Contains(x.UserId)).GroupBy(x => x.UserId).Select(x => new { UserId = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.UserId, x => x.Count, ct);

        return recentUsers.Select(x => new SuperAdminUserRowDto(
            x.Name,
            x.Email,
            x.Plan.ToString(),
            documentCounts.GetValueOrDefault(x.Id),
            clientCounts.GetValueOrDefault(x.Id),
            x.LastLoginAt,
            x.IsBlocked ? "Bloqueado" : "Ativo")).ToList();
    }

    private static Task<List<SuperAdminPaymentRowDto>> GetRecentPaymentsAsync(IQueryable<Domain.Entities.Payment> payments, CancellationToken ct) =>
        payments.OrderByDescending(x => x.CreatedAt)
            .Take(8)
            .Select(x => new SuperAdminPaymentRowDto(x.PayerEmail ?? x.UserId.ToString(), x.Plan.ToString(), x.Amount, x.PaymentMethod ?? "Manual", x.Status.ToString(), x.DueDate, x.PaidAt, x.ExternalPaymentId))
            .ToListAsync(ct);
}

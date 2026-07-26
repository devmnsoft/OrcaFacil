using Microsoft.EntityFrameworkCore;
using OrcaFacil.Application.DTOs;
using OrcaFacil.Domain.Enums;

namespace OrcaFacil.Persistence.Queries;

public class SuperAdminDashboardQueries
{
    private const decimal ProMonthlyPrice = 19.90m;
    private readonly OrcaFacilDbContext _db;

    public SuperAdminDashboardQueries(OrcaFacilDbContext db) => _db = db;

    public async Task<SuperAdminDashboardDto> GetAsync(CancellationToken ct = default)
    {
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var users = _db.Users.AsNoTracking().Where(user => !user.IsDeleted);
        var subscriptions = _db.Subscriptions.AsNoTracking().Where(subscription => !subscription.IsDeleted);
        var payments = _db.Payments.AsNoTracking().Where(payment => !payment.IsDeleted);
        var documents = _db.Documents.AsNoTracking().Where(document => !document.IsDeleted);
        var clients = _db.Clients.AsNoTracking().Where(client => !client.IsDeleted);

        var dto = new SuperAdminDashboardDto
        {
            TotalUsers = await users.CountAsync(ct),
            ActiveUsers = await users.CountAsync(user => user.IsActive && !user.IsBlocked, ct),
            BlockedUsers = await users.CountAsync(user => user.IsBlocked, ct),
            FreeUsers = await users.CountAsync(user => user.Plan == PlanType.Free, ct),
            ProUsers = await users.CountAsync(user => user.Plan == PlanType.Pro, ct),
            TrialUsers = await subscriptions.CountAsync(subscription => subscription.Status == SubscriptionStatus.Trial, ct),
            PastDueUsers = await subscriptions.CountAsync(subscription => subscription.Status == SubscriptionStatus.PastDue, ct),
            SuspendedUsers = await subscriptions.CountAsync(subscription => subscription.Status == SubscriptionStatus.Suspended, ct),
            TotalClients = await clients.CountAsync(ct),
            TotalDocuments = await documents.CountAsync(ct),
            TotalBudgets = await documents.CountAsync(document => document.Type == DocumentType.Budget, ct),
            TotalReceipts = await documents.CountAsync(document => document.Type == DocumentType.Receipt, ct),
            TotalPayments = await payments.CountAsync(ct),
            ApprovedPaymentsAmountMonth = await SumPaymentsAsync(payments.Where(payment => payment.Status == PaymentStatus.Approved && payment.CreatedAt >= monthStart), ct),
            PendingPaymentsAmount = await SumPaymentsAsync(payments.Where(payment => payment.Status == PaymentStatus.Pending), ct),
            OverduePaymentsAmount = await SumPaymentsAsync(payments.Where(payment => payment.Status == PaymentStatus.Expired), ct),
        };

        dto.MrrEstimate = await subscriptions
            .Where(subscription => subscription.Status == SubscriptionStatus.Active && subscription.Plan == PlanType.Pro)
            .SumAsync(subscription => (decimal?)subscription.Amount, ct) ?? dto.ProUsers * ProMonthlyPrice;

        dto.RecentUsers = await BuildRecentUsersAsync(users, documents, clients, ct);
        dto.RecentPayments = await BuildRecentPaymentsAsync(payments, ct);
        dto.RecentErrors = await _db.SystemErrors.AsNoTracking()
            .Where(error => !error.IsDeleted)
            .OrderByDescending(error => error.CreatedAt)
            .Take(5)
            .Select(error => error.Message)
            .ToListAsync(ct);

        return dto;
    }

    private static async Task<decimal> SumPaymentsAsync(IQueryable<Domain.Entities.Payment> query, CancellationToken ct)
        => await query.SumAsync(payment => (decimal?)payment.Amount, ct) ?? 0;

    private static async Task<List<SuperAdminUserRowDto>> BuildRecentUsersAsync(
        IQueryable<Domain.Entities.UserAccount> users,
        IQueryable<Domain.Entities.Document> documents,
        IQueryable<Domain.Entities.Client> clients,
        CancellationToken ct)
    {
        var recentUsers = await users.OrderByDescending(user => user.CreatedAt).Take(8).ToListAsync(ct);
        if (recentUsers.Count == 0) return [];

        var userIds = recentUsers.Select(user => user.Id).ToArray();
        var documentCounts = await documents.Where(document => userIds.Contains(document.UserId))
            .GroupBy(document => document.UserId)
            .Select(group => new { UserId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.UserId, item => item.Count, ct);
        var clientCounts = await clients.Where(client => userIds.Contains(client.UserId))
            .GroupBy(client => client.UserId)
            .Select(group => new { UserId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.UserId, item => item.Count, ct);

        return recentUsers.Select(user => new SuperAdminUserRowDto(
            user.Name,
            user.Email,
            user.Plan.ToString(),
            documentCounts.GetValueOrDefault(user.Id),
            clientCounts.GetValueOrDefault(user.Id),
            user.LastLoginAt,
            user.IsBlocked ? "Bloqueado" : user.IsActive ? "Ativo" : "Inativo")).ToList();
    }

    private static Task<List<SuperAdminPaymentRowDto>> BuildRecentPaymentsAsync(IQueryable<Domain.Entities.Payment> payments, CancellationToken ct)
        => payments.OrderByDescending(payment => payment.CreatedAt)
            .Take(8)
            .Select(payment => new SuperAdminPaymentRowDto(
                payment.PayerEmail ?? payment.UserId.ToString(),
                payment.Plan.ToString(),
                payment.Amount,
                payment.PaymentMethod ?? "Manual",
                payment.Status.ToString(),
                payment.DueDate,
                payment.PaidAt,
                payment.ExternalPaymentId))
            .ToListAsync(ct);
}

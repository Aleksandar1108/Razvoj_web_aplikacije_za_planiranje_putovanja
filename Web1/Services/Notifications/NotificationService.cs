using Microsoft.EntityFrameworkCore;
using Web1.Data;
using Web1.Dtos.Notifications;

namespace Web1.Services.Notifications;

public sealed class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserNotificationDto>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.UserNotifications.AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(100)
            .Select(n => new UserNotificationDto
            {
                Id = n.Id,
                Category = n.Category,
                Title = n.Title,
                Message = n.Message,
                TravelPlanId = n.TravelPlanId,
                TravelDestinationId = n.TravelDestinationId,
                IsRead = n.IsRead,
                CreatedAtUtc = n.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.UserNotifications.AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var row = await _db.UserNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);
        if (row is null)
            return false;

        if (!row.IsRead)
        {
            row.IsRead = true;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _db.UserNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);
    }
}

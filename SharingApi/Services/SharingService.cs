using Microsoft.EntityFrameworkCore;
using SharingApi.Data;
using SharingApi.Data.Entities;
using SharingApi.Dtos;
using SharingApi.Infrastructure;

namespace SharingApi.Services;

public sealed class SharingService : ISharingService
{
    private readonly SharingDbContext _db;

    public SharingService(SharingDbContext db)
    {
        _db = db;
    }

    public async Task<CreateTravelPlanShareLinkResponseDto> CreateShareLinkAsync(
        Guid ownerUserId,
        Guid travelPlanId,
        CreateTravelPlanShareLinkRequestDto request,
        CancellationToken cancellationToken)
    {
        var permission = NormalizePermission(request.Permission);

        var plan = await _db.TravelPlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == travelPlanId && p.UserId == ownerUserId, cancellationToken);
        if (plan is null)
            throw new InvalidOperationException("Plan putovanja nije pronađen.");

        var token = ShareTokenCrypto.CreateOpaqueToken();
        var tokenHash = ShareTokenCrypto.HashToken(token);

        var now = DateTime.UtcNow;
        var link = new TravelPlanShareLinkEntity
        {
            Id = Guid.NewGuid(),
            TravelPlanId = travelPlanId,
            TokenHash = tokenHash,
            Permission = permission,
            CreatedByUserId = ownerUserId,
            CreatedAtUtc = now,
            ExpiresAtUtc = null,
            RevokedAtUtc = null
        };

        _db.TravelPlanShareLinks.Add(link);
        await _db.SaveChangesAsync(cancellationToken);

        var payloadJson =
            $"{{\"v\":1,\"pid\":\"{travelPlanId:D}\",\"p\":\"{permission}\",\"t\":\"{token}\"}}";

        return new CreateTravelPlanShareLinkResponseDto
        {
            TravelPlanId = travelPlanId,
            Permission = permission,
            Token = token,
            QrPayloadJson = payloadJson
        };
    }

    public async Task<ClaimShareLinkResponseDto?> ClaimShareLinkAsync(
        Guid recipientUserId,
        ClaimShareLinkRequestDto request,
        CancellationToken cancellationToken)
    {
        var token = request.Token.Trim();
        var tokenHash = ShareTokenCrypto.HashToken(token);

        var link = await _db.TravelPlanShareLinks.AsNoTracking()
            .FirstOrDefaultAsync(
                l => l.TokenHash == tokenHash
                     && l.RevokedAtUtc == null
                     && (l.ExpiresAtUtc == null || l.ExpiresAtUtc > DateTime.UtcNow),
                cancellationToken);
        if (link is null)
            return null;

        var now = DateTime.UtcNow;
        var existing = await _db.TravelPlanShareRecipients
            .FirstOrDefaultAsync(r => r.TravelPlanId == link.TravelPlanId && r.RecipientUserId == recipientUserId, cancellationToken);

        var effective = MaxPermission(existing?.Permission, link.Permission);

        if (existing is null)
        {
            _db.TravelPlanShareRecipients.Add(new TravelPlanShareRecipientEntity
            {
                Id = Guid.NewGuid(),
                TravelPlanId = link.TravelPlanId,
                RecipientUserId = recipientUserId,
                Permission = effective,
                ClaimedAtUtc = now,
                UpdatedAtUtc = now
            });
        }
        else
        {
            existing.Permission = effective;
            existing.UpdatedAtUtc = now;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new ClaimShareLinkResponseDto
        {
            TravelPlanId = link.TravelPlanId,
            Permission = effective
        };
    }

    public async Task<IReadOnlyList<SharedTravelPlanListItemDto>> ListSharedPlansForUserAsync(
        Guid recipientUserId,
        CancellationToken cancellationToken)
    {
        var rows = await (
                from r in _db.TravelPlanShareRecipients.AsNoTracking()
                join p in _db.TravelPlans.AsNoTracking() on r.TravelPlanId equals p.Id
                where r.RecipientUserId == recipientUserId
                orderby r.UpdatedAtUtc descending
                select new SharedTravelPlanListItemDto
                {
                    TravelPlanId = r.TravelPlanId,
                    Permission = r.Permission,
                    Name = p.Name,
                    ShortDescription = p.ShortDescription,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    UpdatedAtUtc = r.UpdatedAtUtc
                })
            .ToListAsync(cancellationToken);

        return rows;
    }

    private static string NormalizePermission(string permission)
    {
        var p = permission.Trim().ToLowerInvariant();
        if (p is not ("view" or "edit"))
            throw new ArgumentException("Permission mora biti 'view' ili 'edit'.");
        return p;
    }

    private static string MaxPermission(string? a, string b)
    {
        var aa = (a ?? "view").Trim().ToLowerInvariant();
        var bb = b.Trim().ToLowerInvariant();
        return aa == "edit" || bb == "edit" ? "edit" : "view";
    }
}

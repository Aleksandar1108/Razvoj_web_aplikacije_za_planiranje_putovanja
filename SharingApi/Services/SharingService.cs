using CrossService.Clients;
using CrossService.Dtos;
using Microsoft.EntityFrameworkCore;
using SharingApi.Data;
using SharingApi.Data.Entities;
using SharingApi.Dtos;
using SharingApi.Infrastructure;

namespace SharingApi.Services;

public sealed class SharingService : ISharingService
{
    private readonly SharingDbContext _db;
    private readonly ITravelPlansInternalClient _travelPlans;

    public SharingService(SharingDbContext db, ITravelPlansInternalClient travelPlans)
    {
        _db = db;
        _travelPlans = travelPlans;
    }

    public async Task<CreateTravelPlanShareLinkResponseDto> CreateShareLinkAsync(
        Guid ownerUserId,
        Guid travelPlanId,
        CreateTravelPlanShareLinkRequestDto request,
        CancellationToken cancellationToken)
    {
        var permission = NormalizePermission(request.Permission);

        var owner = await _travelPlans.GetOwnerAsync(travelPlanId, cancellationToken);
        if (!owner.IsOwner || owner.OwnerUserId != ownerUserId)
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
        var recipients = await _db.TravelPlanShareRecipients.AsNoTracking()
            .Where(r => r.RecipientUserId == recipientUserId)
            .OrderByDescending(r => r.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

        if (recipients.Count == 0)
            return Array.Empty<SharedTravelPlanListItemDto>();

        var planIds = recipients.Select(r => r.TravelPlanId).Distinct().ToList();
        var plans = (await _travelPlans.GetMetaBatchAsync(planIds, cancellationToken))
            .ToDictionary(p => p.Id);

        return recipients.Select(r =>
        {
            plans.TryGetValue(r.TravelPlanId, out var plan);
            return new SharedTravelPlanListItemDto
            {
                TravelPlanId = r.TravelPlanId,
                Permission = r.Permission,
                Name = plan?.Name ?? "Plan",
                ShortDescription = plan?.ShortDescription ?? string.Empty,
                StartDate = plan?.StartDate ?? DateOnly.MinValue,
                EndDate = plan?.EndDate ?? DateOnly.MinValue,
                UpdatedAtUtc = r.UpdatedAtUtc
            };
        }).ToList();
    }

    public async Task<int> DeleteAllByTravelPlanIdAsync(Guid travelPlanId, CancellationToken cancellationToken)
    {
        var links = await _db.TravelPlanShareLinks
            .Where(l => l.TravelPlanId == travelPlanId)
            .ToListAsync(cancellationToken);
        var recipients = await _db.TravelPlanShareRecipients
            .Where(r => r.TravelPlanId == travelPlanId)
            .ToListAsync(cancellationToken);

        if (links.Count > 0)
            _db.TravelPlanShareLinks.RemoveRange(links);
        if (recipients.Count > 0)
            _db.TravelPlanShareRecipients.RemoveRange(recipients);

        if (links.Count == 0 && recipients.Count == 0)
            return 0;

        await _db.SaveChangesAsync(cancellationToken);
        return links.Count + recipients.Count;
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

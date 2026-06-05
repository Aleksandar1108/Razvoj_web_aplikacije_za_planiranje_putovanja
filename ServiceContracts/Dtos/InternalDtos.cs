namespace ServiceContracts.Dtos;

public sealed class TravelPlanMetaDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public decimal PlannedBudget { get; set; }
}

public sealed class TravelPlanExistsDto
{
    public bool Exists { get; set; }
}

public sealed class TravelPlanOwnerDto
{
    public bool IsOwner { get; set; }
    public Guid? OwnerUserId { get; set; }
}

public sealed class ShareAccessDto
{
    public string Kind { get; set; } = "none";
}

public sealed class UserBriefDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class UserExistsDto
{
    public bool Exists { get; set; }
}

public sealed class UsersBriefRequestDto
{
    public List<Guid> UserIds { get; set; } = new();
}

public sealed class CreateAdminNotificationRequestDto
{
    public Guid OwnerUserId { get; set; }
    public Guid TravelPlanId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? ItemLabel { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public bool? ChecklistDone { get; set; }
}

public sealed class ActivityCostSumDto
{
    public decimal TotalEstimatedCost { get; set; }
}

public sealed class SharedPlanMetaBatchRequestDto
{
    public List<Guid> TravelPlanIds { get; set; } = new();
}

public sealed class SharedPlanMetaDto
{
    public Guid TravelPlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
}

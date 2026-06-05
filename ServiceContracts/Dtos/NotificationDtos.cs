namespace ServiceContracts.Dtos;

public sealed class UserNotificationDto
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid? TravelPlanId { get; set; }
    public Guid? TravelDestinationId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class UnreadNotificationCountDto
{
    public int Count { get; set; }
}

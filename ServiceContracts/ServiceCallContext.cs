namespace ServiceContracts;

public sealed class ServiceCallContext
{
    public Guid? UserId { get; init; }
    public string? Role { get; init; }
    public string? ShareToken { get; init; }

    public bool IsAdmin => string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);

    public static ServiceCallContext Anonymous { get; } = new();

    public static ServiceCallContext ForUser(Guid userId, string? role = null) =>
        new() { UserId = userId, Role = role };

    public static ServiceCallContext WithShareToken(string shareToken) =>
        new() { ShareToken = shareToken };
}

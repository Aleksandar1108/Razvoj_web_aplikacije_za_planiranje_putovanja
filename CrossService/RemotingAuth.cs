using ServiceContracts;

namespace CrossService;

public static class RemotingAuth
{
    public static Guid RequireUserId(ServiceCallContext context)
    {
        if (context.UserId is not { } userId)
            throw new ServiceOperationException(401, "Niste prijavljeni.");
        return userId;
    }

    public static void RequireAdmin(ServiceCallContext context)
    {
        RequireUserId(context);
        if (!context.IsAdmin)
            throw new ServiceOperationException(403, "Potrebna je Admin uloga.");
    }
}

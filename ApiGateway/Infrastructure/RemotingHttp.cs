using Microsoft.AspNetCore.Mvc;
using ServiceContracts;

namespace ApiGateway.Infrastructure;

public static class RemotingHttp
{
    public static async Task<ActionResult<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            if (TryMapServiceException(ex, out var result))
                return result!;

            return MapUnexpectedException(ex);
        }
    }

    public static async Task<IActionResult> ExecuteAsync(Func<Task> action, bool noContent = true)
    {
        try
        {
            await action();
            return noContent ? new NoContentResult() : new OkResult();
        }
        catch (Exception ex)
        {
            if (TryMapServiceException(ex, out var result))
                return result!;

            return MapUnexpectedException(ex);
        }
    }

    private static ObjectResult MapUnexpectedException(Exception ex)
    {
        var message = GetDeepExceptionMessage(ex);
        return new ObjectResult(new { message }) { StatusCode = StatusCodes.Status500InternalServerError };
    }

    private static string GetDeepExceptionMessage(Exception ex)
    {
        if (ex is AggregateException aggregate)
        {
            var inner = aggregate.Flatten().InnerExceptions.FirstOrDefault();
            if (inner is not null)
                return GetDeepExceptionMessage(inner);
        }

        if (ex.InnerException is not null)
            return GetDeepExceptionMessage(ex.InnerException);

        return string.IsNullOrWhiteSpace(ex.Message)
            ? "Neočekivana greška na serveru."
            : ex.Message;
    }

    private static bool TryMapServiceException(Exception ex, out ActionResult result)
    {
        var serviceEx = FindServiceOperationException(ex);
        if (serviceEx is null)
        {
            result = null!;
            return false;
        }

        result = new ObjectResult(new { message = serviceEx.Message }) { StatusCode = serviceEx.StatusCode };
        return true;
    }

    private static ServiceOperationException? FindServiceOperationException(Exception ex)
    {
        if (ex is AggregateException aggregate)
        {
            foreach (var inner in aggregate.Flatten().InnerExceptions)
            {
                var found = FindServiceOperationException(inner);
                if (found is not null)
                    return found;
            }
        }

        if (ex is ServiceOperationException direct)
            return direct;

        return ex.InnerException is not null
            ? FindServiceOperationException(ex.InnerException)
            : null;
    }
}

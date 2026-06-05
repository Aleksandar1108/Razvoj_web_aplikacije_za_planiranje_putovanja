namespace ServiceContracts;

public sealed class ServiceOperationException : Exception
{
    public int StatusCode { get; }

    public ServiceOperationException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }
}

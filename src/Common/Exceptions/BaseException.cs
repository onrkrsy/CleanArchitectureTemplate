namespace Common.Exceptions;

public abstract class BaseException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }
    public IDictionary<string, object> AdditionalData { get; }

    protected BaseException(
        string message,
        string errorCode,
        int statusCode = 500,
        Exception? innerException = null) : base(message, innerException)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
        AdditionalData = new Dictionary<string, object>();
    }

    public void AddData(string key, object value)
    {
        AdditionalData[key] = value;
    }
}

public class BusinessException : BaseException
{
    public BusinessException(string message, string errorCode = "BUSINESS_ERROR")
        : base(message, errorCode, 400)
    {
    }
}

public class ValidationException : BaseException
{
    public ValidationException(string message, string errorCode = "VALIDATION_ERROR")
        : base(message, errorCode, 400)
    {
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.", "VALIDATION_ERROR", 400)
    {
        foreach (var error in errors)
        {
            AddData(error.Key, error.Value);
        }
    }
}

public class NotFoundException : BaseException
{
    public NotFoundException(string message, string errorCode = "NOT_FOUND")
        : base(message, errorCode, 404)
    {
    }

    public NotFoundException(string resource, object key)
        : base($"{resource} with key '{key}' was not found.", "NOT_FOUND", 404)
    {
        AddData("Resource", resource);
        AddData("Key", key);
    }
}

public class ConflictException : BaseException
{
    public ConflictException(string message, string errorCode = "CONFLICT")
        : base(message, errorCode, 409)
    {
    }
}

public class UnauthorizedException : BaseException
{
    public UnauthorizedException(string message = "Unauthorized access.", string errorCode = "UNAUTHORIZED")
        : base(message, errorCode, 401)
    {
    }
}

public class ForbiddenException : BaseException
{
    public ForbiddenException(string message = "Access forbidden.", string errorCode = "FORBIDDEN")
        : base(message, errorCode, 403)
    {
    }
}

public class ExternalServiceException : BaseException
{
    public string ServiceName { get; }

    public ExternalServiceException(string serviceName, string message, string errorCode = "EXTERNAL_SERVICE_ERROR")
        : base($"External service '{serviceName}' error: {message}", errorCode, 502)
    {
        ServiceName = serviceName;
        AddData("ServiceName", serviceName);
    }
}
namespace Common.Models;

public class Result
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();

    public static Result Success(string message = "")
    {
        return new Result { IsSuccess = true, Message = message };
    }

    public static Result Failure(string message, List<string>? errors = null)
    {
        return new Result 
        { 
            IsSuccess = false, 
            Message = message, 
            Errors = errors ?? new List<string>() 
        };
    }

    public static Result Failure(string message, string error)
    {
        return new Result 
        { 
            IsSuccess = false, 
            Message = message, 
            Errors = new List<string> { error } 
        };
    }
}

public class Result<T> : Result
{
    public T? Data { get; set; }

    public static Result<T> Success(T data, string message = "")
    {
        return new Result<T> { IsSuccess = true, Data = data, Message = message };
    }

    public static new Result<T> Failure(string message, List<string>? errors = null)
    {
        return new Result<T> 
        { 
            IsSuccess = false, 
            Message = message, 
            Errors = errors ?? new List<string>() 
        };
    }

    public static new Result<T> Failure(string message, string error)
    {
        return new Result<T> 
        { 
            IsSuccess = false, 
            Message = message, 
            Errors = new List<string> { error } 
        };
    }
}
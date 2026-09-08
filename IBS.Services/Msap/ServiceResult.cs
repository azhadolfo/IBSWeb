namespace IBS.Services.Msap
{
    public class ServiceResult
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public ServiceResultStatus Status { get; set; }

        public static ServiceResult Success(string? message = null) => new() { IsSuccess = true, Message = message, Status = ServiceResultStatus.Success };
        public static ServiceResult Failure(string message, ServiceResultStatus status = ServiceResultStatus.Error) => new() { IsSuccess = false, Message = message, Status = status };
        public static ServiceResult Warning(string message) => new() { IsSuccess = true, Message = message, Status = ServiceResultStatus.Warning };
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; set; }

        public static ServiceResult<T> Success(T data, string? message = null) => new() { IsSuccess = true, Message = message, Status = ServiceResultStatus.Success, Data = data };
        public new static ServiceResult<T> Failure(string message, ServiceResultStatus status = ServiceResultStatus.Error) => new() { IsSuccess = false, Message = message, Status = status };
    }

    public enum ServiceResultStatus
    {
        Success,
        Error,
        Warning,
        NotFound,
        ValidationError,
        ConfirmationRequired
    }
}

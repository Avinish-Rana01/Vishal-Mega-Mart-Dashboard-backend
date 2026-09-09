namespace VS_Mart_Backend.Features.Registration.Common
{
    public class ActionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public static ActionResponse Ok(string message = "Operation completed successfully.")
        {
            return new ActionResponse { Success = true, Message = message };
        }

        public static ActionResponse Fail(string message)
        {
            return new ActionResponse { Success = false, Message = message };
        }
    }

    public class ActionResponse<T> : ActionResponse
    {
        public T? Data { get; set; }

        public static ActionResponse<T> Ok(T data, string message = "Success")
        {
            return new ActionResponse<T> { Success = true, Message = message, Data = data };
        }

        public new static ActionResponse<T> Fail(string message)
        {
            return new ActionResponse<T> { Success = false, Message = message, Data = default };
        }
    }
}

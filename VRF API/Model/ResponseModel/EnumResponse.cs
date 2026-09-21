namespace VRF_API.Model.ResponseModel
{
    public class EnumResponse
    {
        public enum ErrorCodeEnum
        {
            Success = 200,
            Failure = 400,
            Unauthorized = 206,
            NotFound = 404,
            InternalServerError = 500,
            UnknownError = 600,
            AlreadyExists = 203
            // Add other error codes as needed
        }

        public enum ApiStatusEnum
        {
            Success,
            Failure,
            NotFound,
            Unauthorized,
            InternalServerError,
            BadRequest,
            Unknown,
            UnknownError,
            AlreadyExists
        }
    }
}

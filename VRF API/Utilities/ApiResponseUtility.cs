using VRF_API.Model.ResponseModel;
using static VRF_API.Model.ResponseModel.EnumResponse;

namespace VRF_API.Utilities
{
    public class ApiResponseUtility
    {
        private static ErrorCodeEnum GetErrorCodeBasedOnStatus(ApiStatusEnum status)
        {
            switch (status)
            {
                case ApiStatusEnum.Success:
                    return ErrorCodeEnum.Success;
                case ApiStatusEnum.NotFound:
                    return ErrorCodeEnum.NotFound;
                case ApiStatusEnum.Unauthorized:
                    return ErrorCodeEnum.Unauthorized;
                case ApiStatusEnum.Failure:
                    return ErrorCodeEnum.Failure;
                case ApiStatusEnum.InternalServerError:
                    return ErrorCodeEnum.InternalServerError;
                default:
                    return ErrorCodeEnum.UnknownError;  // Return a default error code if the status doesn't match any case
            }
        }
        public static ApiResponse GenerateApiResponse(ApiStatusEnum status, string message, object data = null)
        {
            var errorCode = GetErrorCodeBasedOnStatus(status);
            return new ApiResponse
            {
                Status = status,
                Message = message,
                ErrorCode = errorCode,
                Data = data
            };
        }
    }
}
